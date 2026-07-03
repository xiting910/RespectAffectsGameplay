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
        // 获取原始传入值
        var originalValue = value;

        // 将 value 替换为 RespectAffectsGameplayMod.IsEffectivelyModded() 的结果
        value = RespectAffectsGameplayMod.IsEffectivelyModded();

        // 输出调试日志, 仅在原始值与替换值不同时输出
        if (originalValue != value)
        {
            ModLog.Debug($"UserDataPathProvider.IsRunningModded setter: {originalValue} → {value}");
        }

        // 继续执行原始 setter
        return true;
    }
}
