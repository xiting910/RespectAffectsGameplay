using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace RespectAffectsGameplay;

/// <summary>
/// 补丁: 拦截 <see cref="UserDataPathProvider.GetAccountDir"/> 方法
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), nameof(UserDataPathProvider.GetAccountDir))]
public static class PatchGetAccountDir
{
    /// <summary>
    /// 在原始方法执行前, 如果 <paramref name="forceModState"/> 为 <see langword="null"/>, 则根据
    /// <see cref="RespectAffectsGameplayMod.IsEffectivelyModded"/> 的结果返回账号目录
    /// </summary>
    /// <param name="forceModState">外部传入的强制 mod 状态</param>
    /// <param name="__result">方法的返回值</param>
    /// <returns><see langword="true"/> 表示跳过原始方法, <see langword="false"/> 表示执行原始方法</returns>
    [HarmonyPrefix]
    public static bool Prefix(bool? forceModState, ref string __result)
    {
        // 如果 forceModState 不为 null, 则不拦截原始方法, 让原始方法继续执行
        if (forceModState.HasValue)
        {
            return true;
        }

        // 否则, 根据 RespectAffectsGameplayMod.IsEffectivelyModded(true) 的结果返回 "modded" 或 "" 作为账号目录
        __result = RespectAffectsGameplayMod.IsEffectivelyModded(true) ? "modded" : "";

        // 输出调试日志
        ModLog.Debug($"UserDataPathProvider.GetAccountDir(forceModState: null) → \"{__result}\" (已拦截)");

        // 跳过原始方法
        return false;
    }
}
