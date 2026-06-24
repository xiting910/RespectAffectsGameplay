using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RespectAffectsGameplay;

/// <summary>
/// 可选补丁: 拦截 <see cref="ModManager.IsRunningModded"/> 方法
/// </summary>
[HarmonyPatch(typeof(ModManager), nameof(ModManager.IsRunningModded))]
public static class PatchModManagerIsRunningModded
{
    /// <summary>
    /// 在原始方法执行前将返回值替换为 <see cref="RespectAffectsGameplayMod.IsEffectivelyModded"/> 的结果,
    /// 并跳过原始方法
    /// </summary>
    /// <param name="__result">方法的返回值</param>
    /// <returns>始终返回 <see langword="false"/>, 跳过原始方法</returns>
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        __result = RespectAffectsGameplayMod.IsEffectivelyModded();
        ModLog.Debug($"ModManager.IsRunningModded() (已拦截) → {__result}");
        return false;
    }
}
