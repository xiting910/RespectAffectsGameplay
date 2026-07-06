using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib.Patching.Models;

namespace RespectAffectsGameplay;

/// <summary>
/// 可选补丁: 拦截 <see cref="ModManager.IsRunningModded"/> 方法
/// </summary>
public sealed class PatchModManagerIsRunningModded : IPatchMethod
{
    /// <inheritdoc/>
    public static string PatchId => $"{ModInfo.HarmonyId}.{nameof(PatchModManagerIsRunningModded)}";

    /// <inheritdoc/>
    public static bool IsCritical => false;

    /// <inheritdoc />
    public static string Description => $"拦截 {nameof(ModManager.IsRunningModded)} 方法, 替换返回值为 mod 自定义逻辑";

    /// <inheritdoc/>
    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(ModManager), nameof(ModManager.IsRunningModded))];
    }

    /// <summary>
    /// 在原始方法执行前将返回值替换为 <see cref="GameplayStateHelper.IsEffectivelyModded"/> 的结果,
    /// 并跳过原始方法
    /// </summary>
    /// <param name="__result">方法的返回值</param>
    /// <returns>始终返回 <see langword="false"/>, 跳过原始方法</returns>
    public static bool Prefix(ref bool __result)
    {
        // 将返回值设为 GameplayStateHelper.IsEffectivelyModded(false) 的结果
        __result = GameplayStateHelper.IsEffectivelyModded(false);

        // 输出日志
        ModLog.Verbose($"拦截 {nameof(ModManager.IsRunningModded)} 方法, 返回值: {__result}");

        // 跳过原始方法
        return false;
    }
}
