using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Ui.Toast;

namespace RespectAffectsGameplay;

/// <summary>
/// 工具类: 用于验证 Mod 的 <c>affects_gameplay</c> 标记是否准确, 并提供修正后的判定
/// </summary>
public static class ModAffectsGameplayValidator
{
    /// <summary>
    /// Toast 通知中 <c>mislabeled.body</c> 的 Mod 列表占位符
    /// </summary>
    private const string PlaceholderModList = "{ModList}";

    /// <summary>
    /// Toast 通知中 <c>mislabeled.title</c> 的误标数量占位符
    /// </summary>
    private const string PlaceholderCount = "{Count}";

    /// <summary>
    /// 存储应该被视为影响游戏性的 Mod ID 集合 (不区分大小写)
    /// </summary>
    public static readonly HashSet<string> MislabeledGameplayMods = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 标记是否已订阅主菜单就绪事件, 避免重复订阅
    /// </summary>
    private static bool _toastSubscribed;

    /// <summary>
    /// 验证所有已加载的 Mod 的 <c>affects_gameplay</c> 标记是否准确, 并将结果存储在 <see cref="MislabeledGameplayMods"/> 中
    /// </summary>
    public static void ValidateAll()
    {
        try
        {
            // 清空之前的验证结果
            MislabeledGameplayMods.Clear();

            // 获取所有已经加载的 Mod
            var mods = ModManager.Mods.Where(m => m.state is ModLoadState.Loaded or ModLoadState.Failed).ToArray();
            if (mods.Length == 0)
            {
                ModLog.Debug("没有已加载的 Mod, 跳过 affects_gameplay 标记验证");
                return;
            }

            // 记录日志
            ModLog.Info($"开始验证 {mods.Length} 个 Mod 的 affects_gameplay 标记...");

            // 遍历所有 mods
            foreach (var mod in mods)
            {
                // 获取 manifest
                var manifest = mod.manifest;

                // 如果 mod 没有 manifest, 则无法验证 affects_gameplay 标记, 跳过
                if (manifest is null)
                {
                    ModLog.Warn($"[{mod.path ?? "<unknown>"}] 没有 manifest, 无法验证 affects_gameplay 标记");
                    continue;
                }

                // 获取 mod 的 ID, 如果没有 ID 则使用 name, 如果都没有则使用 "<unknown>"
                var modId = manifest.id ?? manifest.name ?? "<unknown>";

                // 如果 affects_gameplay 标记为 true, 则无需验证, 直接跳过
                if (manifest.affectsGameplay)
                {
                    ModLog.Debug($"[{modId}] affects_gameplay 标记为 true, 无需验证");
                    continue;
                }

                // 检测 mod 是否应视为影响游戏性
                var result = EvaluateMod(mod, modId);

                // 如果检查结果没有异常, 且应视为影响游戏性, 则将其加入 MislabeledGameplayMods 集合
                if (result.Exception is null && result.ShouldTreatAsGameplay)
                {
                    _ = MislabeledGameplayMods.Add(modId);
                    ModLog.Warn($"[{modId}] affects_gameplay 标记不准确, 应视为影响游戏性: {result.Reason}");
                }
            }

            // 输出验证完成日志
            ModLog.Info($"验证完成, 共发现 {MislabeledGameplayMods.Count} 个 affects_gameplay 标记不准确的 Mod");

            // 如果有误标的 Mod, 注册主菜单就绪后显示 Toast 通知
            if (MislabeledGameplayMods.Count > 0)
            {
                ScheduleMislabeledToast();
            }
        }
        catch (Exception ex)
        {
            ModLog.Error($"affects_gameplay 标记验证异常 (不影响功能, 回退到原始 manifest 值): {ex}");
            MislabeledGameplayMods.Clear();
        }
    }

    /// <summary>
    /// 订阅 <see cref="MainMenuReadyEvent"/> 事件, 在主菜单就绪后显示 Toast 通知
    /// </summary>
    private static void ScheduleMislabeledToast()
    {
        if (_toastSubscribed) { return; }
        _toastSubscribed = true;

        try
        {
            // 获取 I18N 实例
            var i18n = ModLoc.Instance;

            // 获取误标的 Mod 列表, 并在主菜单就绪后显示 Toast 通知
            var modList = string.Join("\n", MislabeledGameplayMods.Select(id => $"  • {id}"));

            // 获取误标的 Mod 数量
            var count = MislabeledGameplayMods.Count;

            // 订阅主菜单就绪事件
            _ = RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>((evt, sub) =>
            {
                // 取消订阅, 避免重复显示 Toast
                sub.Dispose();

                // 显示 Toast 通知
                RitsuToastService.Show(new(
                    i18n.Get("toast.mislabeled.body", string.Empty).Replace(PlaceholderModList, modList),
                    i18n.Get("toast.mislabeled.title", string.Empty).Replace(PlaceholderCount, count.ToString()),
                    level: RitsuToastLevel.Warning,
                    durationSeconds: 5));
            });
        }
        catch (Exception ex)
        {
            ModLog.Warn($"无法注册 Toast 通知 (RitsuLib 版本可能不支持): {ex.Message}");
        }
    }

    /// <summary>
    /// 检测单个 Mod 是否包含 AbstractModel 子类, 以判断其是否应视为影响游戏性
    /// </summary>
    /// <param name="mod">要检测的 Mod</param>
    /// <param name="modId">Mod 的 ID (用于日志输出)</param>
    /// <returns>封装了判定结果和原因的 <see cref="EvaluationResult"/></returns>
    private static EvaluationResult EvaluateMod(Mod mod, string modId)
    {
        if (mod.assembly is null)
        {
            ModLog.Debug($"[{modId}] 没有程序集, 无法扫描 AbstractModel 子类, 视为非 gameplay");
            return new(false, string.Empty);
        }

        try
        {
            var subtypes = ReflectionHelper.GetSubtypesFromAssembly(mod.assembly, typeof(AbstractModel));
            return subtypes.Any() ? new(true, $"包含 AbstractModel 子类: {string.Join(", ", subtypes.Select(t => t.FullName))}") : new(false, string.Empty);
        }
        catch (Exception ex)
        {
            ModLog.Debug($"[{modId}] 扫描 AbstractModel 子类时出错: {ex.Message}");
            return new(false, string.Empty, ex);
        }
    }

    /// <summary>
    /// 用于封装单个 Mod 的 <c>affects_gameplay</c> 判定结果和原因
    /// </summary>
    /// <param name="shouldTreatAsGameplay">是否应视为影响游戏性</param>
    /// <param name="reason">判定原因描述</param>
    /// <param name="exception">发生的异常</param>
    private readonly struct EvaluationResult(bool shouldTreatAsGameplay, string reason, Exception? exception = null)
    {
        /// <summary>
        /// 是否应视为影响游戏性
        /// </summary>
        public bool ShouldTreatAsGameplay { get; } = shouldTreatAsGameplay;

        /// <summary>
        /// 判定原因描述
        /// </summary>
        public string Reason { get; } = reason;

        /// <summary>
        /// 发生的异常
        /// </summary>
        public Exception? Exception { get; } = exception;
    }
}
