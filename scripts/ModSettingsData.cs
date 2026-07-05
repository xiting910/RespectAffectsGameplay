namespace RespectAffectsGameplay;

/// <summary>
/// Mod 设置数据类
/// </summary>
public sealed class ModSettingsData
{
    /// <summary>
    /// Modded 模式设置
    /// </summary>
    public ModdedMode Mode { get; set; } = ModdedMode.Auto;

    /// <summary>
    /// 是否拦截 <c>ModManager.IsRunningModded()</c> 方法
    /// </summary>
    public bool PatchModManagerIsRunningModded { get; set; }

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool VerboseLogging { get; set; }
}
