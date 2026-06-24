namespace RespectAffectsGameplay.Scripts;

/// <summary>
/// Modded 模式枚举
/// </summary>
public enum ModdedMode
{
    /// <summary>
    /// 自动判断: 如果加载了任意 affects_gameplay 为 true 的 mod 则视为 modded, 否则视为 vanilla
    /// </summary>
    Auto = 0,

    /// <summary>
    /// 始终在 vanilla 模式下运行
    /// </summary>
    AlwaysVanilla = 1,

    /// <summary>
    /// 始终在 modded 模式下运行
    /// </summary>
    AlwaysModded = 2,
}
