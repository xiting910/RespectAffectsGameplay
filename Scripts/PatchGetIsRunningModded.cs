using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace RespectAffectsGameplay.Scripts;

/// <summary>
/// 补丁: 拦截 <see cref="UserDataPathProvider.IsRunningModded"/> 的 getter
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), "get_IsRunningModded")]
public static class PatchGetIsRunningModded
{
    /// <summary>
    /// 在原始 getter 执行前将返回值设为 <see cref="RespectAffectsGameplayMod.IsEffectivelyModded"/> 的结果
    /// </summary>
    /// <param name="__result">属性的返回值</param>
    /// <returns>始终返回 <see langword="false"/> 跳过原始 getter</returns>
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result)
    {
        __result = RespectAffectsGameplayMod.IsEffectivelyModded();
        return false;
    }
}
