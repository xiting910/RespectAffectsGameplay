namespace RespectAffectsGameplay;

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
    /// 强制视为 vanilla 状态，无论加载了什么 mod
    /// </summary>
    AlwaysVanilla = 1,

    /// <summary>
    /// 使用游戏原版逻辑判断 modded 状态
    /// </summary>
    Default = 2,
}
