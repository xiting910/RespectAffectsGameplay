using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace RespectAffectsGameplay;

/// <summary>
/// 补丁: 拦截 <see cref="UserDataPathProvider.GetProfileDir"/> 方法
/// </summary>
[HarmonyPatch(typeof(UserDataPathProvider), nameof(UserDataPathProvider.GetProfileDir))]
public static class PatchGetProfileDir
{
    /// <summary>
    /// 根据 <see cref="RespectAffectsGameplayMod.IsEffectivelyModded"/> 的结果决定存档路径
    /// </summary>
    /// <param name="profileId">存档槽位编号</param>
    /// <param name="__result">方法的返回值</param>
    /// <returns><see langword="true"/> 表示继续执行原始方法; <see langword="false"/> 表示跳过原始方法</returns>
    [HarmonyPrefix]
    public static bool Prefix(int profileId, ref string __result)
    {
        if (!RespectAffectsGameplayMod.IsEffectivelyModded())
        {
            __result = $"profile{profileId}";
            ModLog.Debug($"GetProfileDir({profileId}) → vanilla 路径: {__result}");
            return false;
        }
        ModLog.Debug($"GetProfileDir({profileId}) → 委托原始方法 (modded 路径)");
        return true;
    }
}
