using STS2RitsuLib.Compat;

namespace RespectAffectsGameplay;

/// <summary>
/// Mod 信息工具类
/// </summary>
public static class ModInfo
{
    /// <summary>
    /// 缓存 <see cref="RitsuModInfo"/> 实例
    /// </summary>
    private static RitsuModInfo? _cached;

    /// <summary>
    /// 标记是否已解析 <see cref="_cached"/>, 避免重复查询
    /// </summary>
    private static bool _resolved;

    /// <summary>
    /// 获取 <see cref="RitsuModInfo"/> 实例, 如果未解析则尝试从 <see cref="RitsuModManager"/> 获取
    /// </summary>
    private static RitsuModInfo? Cached
    {
        get
        {
            if (!_resolved)
            {
                try
                {
                    if (RitsuModManager.TryGetModInfo("RespectAffectsGameplay", out var info) && info is not null)
                    {
                        _cached = info;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn($"无法从 RitsuModManager 获取自身 ModInfo, 使用 fallback: {ex.Message}");
                }
                _resolved = true;
            }
            return _cached;
        }
    }

    /// <summary>
    /// 获取 mod 的唯一标识符 (来自 JSON "id")
    /// </summary>
    public static string Id => Cached?.Id ?? "RespectAffectsGameplay";

    /// <summary>
    /// 获取 mod 的名称 (来自 JSON "name")
    /// </summary>
    public static string Name => Cached?.Name ?? "Respect Affects Gameplay";

    /// <summary>
    /// 获取 mod 的版本号 (来自 JSON "version")
    /// </summary>
    public static string Version => Cached?.Version ?? "unknown";

    /// <summary>
    /// 获取 mod 的作者 (来自 JSON "author")
    /// </summary>
    public static string Author => Cached?.Author ?? "xiting910";

    /// <summary>
    /// 获取 mod 的 Harmony ID (格式为 "Author.Id")
    /// </summary>
    public static string HarmonyId => $"{Author}.{Id}";
}
