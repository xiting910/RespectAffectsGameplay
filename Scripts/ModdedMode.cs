namespace RespectAffectsGameplay;

/// <summary>
/// Modded 模式枚举
/// </summary>
public enum ModdedMode
{
    /// <summary>
    /// 自动判断
    /// </summary>
    Auto = 0,

    /// <summary>
    /// 强制视为 vanilla 状态, 无论加载了什么 mod
    /// </summary>
    AlwaysVanilla = 1,

    /// <summary>
    /// 使用游戏原版逻辑
    /// </summary>
    Default = 2
}
