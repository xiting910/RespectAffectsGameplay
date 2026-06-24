using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace RespectAffectsGameplay;

/// <summary>
/// 补丁: 拦截 <see cref="UserDataPathProvider.IsRunningModded"/> 的 setter
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), $"set_{nameof(UserDataPathProvider.IsRunningModded)}")]
public static class PatchSetIsRunningModded
{
    /// <summary>
    /// 在原始 setter 执行前将 value 替换为 <see cref="RespectAffectsGameplayMod.IsEffectivelyModded"/> 的返回值
    /// </summary>
    /// <param name="value">外部传入的待写入值</param>
    /// <returns>始终返回 <see langword="true"/> 继续执行原始 setter</returns>
    [HarmonyPrefix]
    public static bool Prefix(ref bool value)
    {
        var originalValue = value;
        value = RespectAffectsGameplayMod.IsEffectivelyModded();
        if (originalValue != value)
        {
            ModLog.Debug($"UserDataPathProvider.IsRunningModded setter: {originalValue} → {value}");
        }
        return true;
    }
}
