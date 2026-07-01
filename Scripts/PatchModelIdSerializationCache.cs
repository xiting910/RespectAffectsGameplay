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
        ModLog.Debug("ModelIdSerializationCache.Init() 开始, 启用 mod 过滤");
    }

    /// <summary>
    /// 在 <see cref="ModelIdSerializationCache.Init"/> 执行后清除过滤标志
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
    public static void InitPostfix()
    {
        _filterModsForHash = false;
        ModLog.Info($"ModelIdSerializationCache.Init() 完成, 联机哈希已生成 (Hash={ModelIdSerializationCache.Hash})");
    }

    /// <summary>
    /// 安全网: 即使 Init 抛出异常也确保标志位被重置, 同时记录异常信息
    /// </summary>
    /// <param name="__exception">原始方法抛出的异常</param>
    /// <returns>始终返回 <see langword="null"/>, 表示不抛出异常</returns>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
    public static Exception? InitFinalizer(Exception? __exception)
    {
        _filterModsForHash = false;

        if (__exception is not null)
        {
            ModLog.Error($"ModelIdSerializationCache.Init() 抛出异常, 联机哈希可能不准确: {__exception}");
        }

        return null;
    }

    /// <summary>
    /// 当过滤标志开启时, 从 <see cref="ModManager.Mods"/> 中排除 <c>affects_gameplay: false</c> 的 Mod
    /// </summary>
    /// <param name="__result">属性的返回值</param>
    /// <returns>当过滤标志开启时返回 <see langword="false"/>, 跳过原始 getter; 否则返回 <see langword="true"/>, 继续执行原始 getter</returns>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ModManager), $"get_{nameof(ModManager.Mods)}")]
    public static bool GetModsPrefix(ref IReadOnlyList<Mod> __result)
    {
        // 如果过滤标志未开启, 则继续执行原始 getter
        if (!_filterModsForHash) { return true; }

        try
        {
            // 反射获取原始 _mods 列表
            var original = AccessTools.StaticFieldRefAccess<List<Mod>>(typeof(ModManager), "_mods");

            // 获取原始列表的总数
            var totalCount = original.Count;

            // 过滤掉 affects_gameplay: false 的 Mod
            __result = [.. original.Where(m => m.manifest?.affectsGameplay ?? true)];

            // 获取过滤后的列表的数量
            var filteredCount = __result.Count;

            // 计算被排除的 Mod 数量
            var excludedCount = totalCount - filteredCount;

            // 记录日志
            ModLog.Debug($"哈希过滤: 共 {totalCount} 个 mod, 保留 {filteredCount} 个 (gameplay), 排除 {excludedCount} 个 (非 gameplay)");

            // 跳过原始 getter
            return false;
        }
        catch (Exception ex)
        {
            // 反射失败时 (例如游戏更新修改了字段名), 回退到原始 getter
            ModLog.Warn($"无法反射访问 ModManager._mods, 回退到原始行为: {ex.Message}");
            return true;
        }
    }
}
