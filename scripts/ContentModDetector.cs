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
    /// 标记是否已完成扫描
    /// </summary>
    private static bool _scanned;

    /// <summary>
    /// 标记是否已订阅主菜单就绪事件, 避免重复订阅
    /// </summary>
    private static bool _toastSubscribed;

    /// <summary>
    /// 存储所有注册了 <see cref="AbstractModel"/> 子类的 Mod ID
    /// </summary>
    private static readonly HashSet<string> ModIdsWithContent = [];

    /// <summary>
    /// 判断是否有实现了 <see cref="AbstractModel"/> 的 Mod 被加载
    /// </summary>
    /// <returns><see langword="true"/> 如果有实现了 <see cref="AbstractModel"/> 的 Mod 被加载,
    /// <see langword="false"/> 否则</returns>
    public static bool HasContentModsLoaded()
    {
        if (!_scanned)
        {
            PerformScan();
            _scanned = true;
        }
        return ModIdsWithContent.Count > 0;
    }

    /// <summary>
    /// 判断某个 Mod 是否是内容性 Mod (即是否包含 <see cref="AbstractModel"/> 子类)
    /// </summary>
    /// <param name="modId">要检测的 Mod 的 ID</param>
    /// <returns><see langword="true"/> 如果该 Mod 是内容性 Mod, <see langword="false"/> 否则</returns>
    public static bool IsContentMod(string modId)
    {
        if (!_scanned)
        {
            PerformScan();
            _scanned = true;
        }
        return ModIdsWithContent.Contains(modId);
    }

    /// <summary>
    /// 执行扫描: 遍历所有已加载的 Mod, 检测是否包含 <see cref="AbstractModel"/> 子类
    /// </summary>
    private static void PerformScan()
    {
        try
        {
            // 记录误标的 Mod 列表
            var mislabeledMods = new List<string>();

            // 遍历所有已加载的 Mod
            foreach (var mod in ModManager.Mods.Where(m => m.IsLoaded()))
            {
                // 获取 Mod ID
                var modId = mod.GetId();

                // 检测 Mod 是否包含 AbstractModel 子类
                if (mod.ContainsAbstractModel() == true)
                {
                    // 包含 AbstractModel 子类, 记录 Mod ID
                    _ = ModIdsWithContent.Add(modId);

                    // 如果 Mod 标记 affects_gameplay 为 false, 则认为是误标, 记录 Mod 名称
                    if (mod.manifest?.affectsGameplay == false)
                    {
                        mislabeledMods.Add(modId);
                    }
                }
            }

            // 如果发现误标的 Mod, 则显示 Toast 通知
            if (mislabeledMods.Count > 0)
            {
                // 构建误标的 Mod 列表字符串
                var modList = string.Join("\n", mislabeledMods.Select(id => $"  • {id}"));

                // 显示 Toast 通知
                ScheduleMislabeledToast(modList, mislabeledMods.Count);
            }
        }
        catch (Exception ex)
        {
            ModLog.Error($"扫描 Mod 内容时发生异常 (不影响 mod 核心功能, 但存档可能被错误隔离): {ex}");
            ModIdsWithContent.Clear();
        }
    }

    /// <summary>
    /// 订阅 <see cref="MainMenuReadyEvent"/> 事件, 在主菜单就绪后显示 Toast 通知
    /// </summary>
    /// <param name="modList">误标的 Mod 列表字符串</param>
    /// <param name="count">误标的 Mod 数量</param>
    private static void ScheduleMislabeledToast(string modList, int count)
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
}
