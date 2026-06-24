namespace RespectAffectsGameplay;

/// <summary>
/// Mod 设置数据类
/// </summary>
public sealed class ModSettingsData
{
    /// <summary>
    /// Modded 模式设置 (重启生效)
    /// </summary>
    public ModdedMode Mode { get; set; } = ModdedMode.Auto;

    /// <summary>
    /// 是否拦截 <c>ModManager.IsRunningModded()</c> 方法
    /// </summary>
    public bool PatchModManagerIsRunningModded { get; set; } = false;
}
