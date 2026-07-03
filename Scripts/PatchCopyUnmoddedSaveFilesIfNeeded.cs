using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RespectAffectsGameplay;

/// <summary>
/// 补丁: 拦截 <see cref="ModManager.CopyUnmoddedSaveFilesIfNeeded"/> 方法
/// </summary>
[HarmonyPatch(typeof(ModManager), nameof(ModManager.CopyUnmoddedSaveFilesIfNeeded))]
public static class PatchCopyUnmoddedSaveFilesIfNeeded
{
    /// <summary>
    /// 在原始方法执行前, 如果当前不是 gameplay modded 状态, 则直接跳过原始方法, 避免首次存档复制
    /// </summary>
    /// <returns><see langword="true"/> 表示继续执行原始方法, <see langword="false"/> 表示跳过原始方法</returns>
    [HarmonyPrefix]
    public static bool Prefix()
    {
        // 如果当前不是 gameplay modded 状态, 则直接跳过原始方法, 避免首次存档复制
        if (!RespectAffectsGameplayMod.IsEffectivelyModded())
        {
            ModLog.Info("非 gameplay modded 状态, 跳过首次存档复制");
            return false;
        }
        return true;
    }
}
