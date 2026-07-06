using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Patching.Models;

namespace RespectAffectsGameplay;

/// <summary>
/// 补丁: 拦截 <see cref="ModManager.CopyUnmoddedSaveFilesIfNeeded"/> 方法
/// </summary>
public sealed class PatchCopyUnmoddedSaveFilesIfNeeded : IPatchMethod
{
    /// <inheritdoc/>
    public static string PatchId => $"{ModInfo.HarmonyId}.{nameof(PatchCopyUnmoddedSaveFilesIfNeeded)}";

    /// <inheritdoc/>
    public static bool IsCritical => false;

    /// <inheritdoc />
    public static string Description => $"拦截 {nameof(ModManager.CopyUnmoddedSaveFilesIfNeeded)} 方法, 非 gameplay 状态时跳过存档复制";

    /// <inheritdoc/>
    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(ModManager), nameof(ModManager.CopyUnmoddedSaveFilesIfNeeded))];
    }

    /// <summary>
    /// 在原始方法执行前, 如果当前不是 gameplay modded 状态, 则直接跳过原始方法, 避免首次存档复制
    /// </summary>
    /// <returns><see langword="true"/> 表示继续执行原始方法, <see langword="false"/> 表示跳过原始方法</returns>
    public static bool Prefix()
    {
        // 如果当前不是 gameplay modded 状态, 则直接跳过原始方法, 避免首次存档复制
        if (!GameplayStateHelper.IsEffectivelyModded(true))
        {
            ModLog.Verbose($"拦截 {nameof(ModManager.CopyUnmoddedSaveFilesIfNeeded)} 方法, 因为当前不是 gameplay modded 状态");
            return false;
        }
        return true;
    }
}
