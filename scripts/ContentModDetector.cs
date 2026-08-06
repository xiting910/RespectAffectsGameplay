using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.Ui.Toast;

namespace RespectAffectsGameplay;

/// <summary>
/// 用于检测已加载的 Mod 是否包含 <see cref="AbstractModel"/> 子类, 并在发现误标的 Mod 时显示 Toast 通知
/// </summary>
public static class ContentModDetector
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
    /// 标记是否已尝试扫描
    /// </summary>
    private static bool _scanned;

    /// <summary>
    /// 标记是否已订阅主菜单就绪事件, 避免重复订阅
    /// </summary>
    private static bool _toastSubscribed;

    /// <summary>
    /// 记录已加载的 Mod 是否包含 <see cref="AbstractModel"/> 子类的字典, 键为 Mod ID, 值为是否包含
    /// </summary>
    private static readonly Dictionary<string, bool> ModContentStatusById = [];

    /// <summary>
    /// 判断某个 Mod 是否是内容性 Mod (即是否包含 <see cref="AbstractModel"/> 子类)
    /// </summary>
    /// <param name="modId">要检测的 Mod 的 ID</param>
    /// <returns><see langword="true"/> 如果检测到该 Mod 是内容性 Mod, <see langword="false"/>
    /// 如果检测到该 Mod 不是内容性 Mod, <see langword="null"/> 表示无法确定</returns>
    public static bool? IsContentMod(string modId)
    {
        EnsureScanPerform();
        return ModContentStatusById.TryGetValue(modId, out var isContent) ? isContent : null;
    }

    /// <summary>
    /// 确认扫描已执行, 如果尚未扫描则执行扫描, 并在发现误标的 Mod 时显示 Toast 通知
    /// </summary>
    private static void EnsureScanPerform()
    {
        // 如果已经扫描过, 则直接返回, 避免重复扫描
        if (_scanned) { return; }

        // 设置扫描标记, 避免重复扫描
        _scanned = true;

        // 记录日志
        ModLog.Info("开始扫描已加载的 Mod 是否为内容性 Mod (是否包含 AbstractModel 子类)");

        try
        {
            // 记录误标的 Mod 列表
            var mislabeledMods = new List<string>();

            // 遍历所有已加载的 Mod
            foreach (var mod in ModManager.Mods.Where(static m => m.IsLoaded()))
            {
                // 获取 Mod ID
                var modId = mod.GetId();

                // 白名单中的 Mod 跳过内容检测, 不参与误标警告
                if (ModSettingsHelper.IsWhitelisted(modId))
                {
                    ModLog.Verbose($"{modId} 在白名单中, 跳过内容检测");
                    continue;
                }

                // 获取该模组是否包含 AbstractModel 子类的检测结果
                var result = mod.ContainsAbstractModel();
                if (result is null) { continue; }

                // 该模组是否包含 AbstractModel 子类
                var isContentMod = result.Value;

                // 记录该模组的内容性状态到字典中
                ModContentStatusById[modId] = isContentMod;

                // 记录日志
                ModLog.Verbose($"检测到 {modId} 的内容性状态: {(isContentMod ? "内容 Mod" : "不是内容 Mod")}");

                // 如果是内容性 Mod, 则记录日志并检查 affects_gameplay 标记
                if (isContentMod && mod.manifest?.affectsGameplay == false)
                {
                    // 记录日志
                    ModLog.Info($"检测到 affects_gameplay 标记可能不准确的内容性 Mod: {modId} (可能应为 true)");

                    // 记录误标的 Mod ID
                    mislabeledMods.Add(modId);
                }
            }

            // 如果发现误标的 Mod, 则显示 Toast 通知
            if (mislabeledMods.Count > 0)
            {
                // 构建误标的 Mod 列表字符串
                var modList = string.Join("\n", mislabeledMods.Select(static id => $"  • {id}"));

                // 显示 Toast 通知
                ScheduleMislabeledToast(modList, mislabeledMods);
            }

            // 记录日志: 扫描完成
            ModLog.Info("已完成扫描已加载的 Mod 是否为内容性 Mod (是否包含 AbstractModel 子类)");
        }
        catch (Exception ex)
        {
            ModLog.Error($"扫描 Mod 内容时发生异常: {ex}");
        }
    }

    /// <summary>
    /// 订阅 <see cref="MainMenuReadyEvent"/> 事件, 在主菜单就绪后显示 Toast 通知
    /// </summary>
    /// <param name="modList">误标的 Mod 列表字符串</param>
    /// <param name="mislabeledMods">误标的 Mod ID 列表, 用于点击 Toast 时一键加入白名单</param>
    private static void ScheduleMislabeledToast(string modList, IReadOnlyList<string> mislabeledMods)
    {
        // 如果已经订阅过, 则直接返回, 避免重复订阅
        if (_toastSubscribed) { return; }

        // 设置订阅标记, 避免重复订阅
        _toastSubscribed = true;

        try
        {
            // 获取 I18N 实例
            var i18n = ModLoc.Instance;

            // 订阅主菜单就绪事件
            _ = RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>((e, sub) =>
            {
                // 取消订阅, 避免重复显示 Toast
                sub.Dispose();

                // 显示 Toast 通知, 点击后一键将全部误标 Mod 加入白名单
                RitsuToastService.Show(new(
                    i18n.Get("toast.mislabeled.body", string.Empty)
                        .Replace(PlaceholderModList, modList),
                    i18n.Get("toast.mislabeled.title", string.Empty)
                        .Replace(PlaceholderCount, mislabeledMods.Count.ToString()),
                    level: RitsuToastLevel.Warning,
                    durationSeconds: 8,
                    onClick: () =>
                    {
                        foreach (var modId in mislabeledMods)
                        {
                            _ = ModSettingsHelper.AddToWhitelist(modId);
                        }
                        ModLog.Info("已将全部误标 Mod 加入白名单");
                    }
                ));
            });
        }
        catch (Exception ex)
        {
            ModLog.Warn($"无法注册 Toast 通知: {ex.Message}");
        }
    }
}
