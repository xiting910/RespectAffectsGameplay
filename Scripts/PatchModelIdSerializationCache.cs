using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace RespectAffectsGameplay;

/// <summary>
/// 补丁组: 阻止 <c>affects_gameplay: false</c> 的 Mod 影响联机哈希计算
/// </summary>
public static class PatchModelIdSerializationCache
{
    /// <summary>
    /// 标志位: 在 <see cref="ModelIdSerializationCache.Init"/> 执行期间为 <see langword="true"/>, 否则为 <see langword="false"/>
    /// </summary>
    private static bool _filterModsForHash;

    /// <summary>
    /// 在 <see cref="ModelIdSerializationCache.Init"/> 执行前设置过滤标志
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
    public static void InitPrefix()
    {
        _filterModsForHash = true;
    }

    /// <summary>
    /// 在 <see cref="ModelIdSerializationCache.Init"/> 执行后清除过滤标志
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
    public static void InitPostfix()
    {
        _filterModsForHash = false;
    }

    /// <summary>
    /// 安全网: 即使 Init 抛出异常也确保标志位被重置
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
    public static Exception? InitFinalizer(Exception? __exception)
    {
        _filterModsForHash = false;
        _ = __exception;
        return null;
    }

    /// <summary>
    /// 当过滤标志开启时, 从 <see cref="ModManager.Mods"/> 中排除 <c>affects_gameplay: false</c> 的 Mod
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ModManager), $"get_{nameof(ModManager.Mods)}")]
    public static bool GetModsPrefix(ref IReadOnlyList<Mod> __result)
    {
        // 如果过滤标志未开启, 则继续执行原始 getter
        if (!_filterModsForHash) { return true; }

        // 反射获取原始 _mods 列表
        var original = AccessTools.StaticFieldRefAccess<List<Mod>>(typeof(ModManager), "_mods");

        // 过滤掉 affects_gameplay: false 的 Mod
        __result = [.. original.Where(m => m.manifest?.affectsGameplay ?? true)];

        // 跳过原始 getter
        return false;
    }
}
