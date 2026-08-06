using MegaCrit.Sts2.Core.Modding;

namespace RespectAffectsGameplay;

/// <summary>
/// 辅助类: 用于判断当前是否应视为 "modded" 状态
/// </summary>
public static class GameplayStateHelper
{
    /// <summary>
    /// 缓存 <see cref="IsEffectivelyModded"/> 的结果 (非存档目录场景)
    /// </summary>
    private static bool? _cachedIsEffectivelyModded;

    /// <summary>
    /// 缓存 <see cref="IsEffectivelyModded"/> 的结果 (存档目录场景)
    /// </summary>
    private static bool? _cachedIsEffectivelyModdedForSaveDir;

    /// <summary>
    /// 判断当前是否应视为 "modded" 状态
    /// </summary>
    /// <param name="isForSavingDir">是否为用于存档目录判断</param>
    /// <returns><see langword="true"/> 表示应视为 modded 状态;
    /// <see langword="false"/> 表示应视为 vanilla 状态</returns>
    /// <exception cref="InvalidOperationException">当 ModdedMode 设置为未知值时抛出</exception>
    public static bool IsEffectivelyModded(bool isForSavingDir)
    {
        // 如果是用于存档目录判断, 且缓存值存在, 则直接返回缓存值
        if (isForSavingDir && _cachedIsEffectivelyModdedForSaveDir.HasValue)
        {
            return _cachedIsEffectivelyModdedForSaveDir.Value;
        }

        // 如果不是用于存档目录判断, 且缓存值存在, 则直接返回缓存值
        if (!isForSavingDir && _cachedIsEffectivelyModded.HasValue)
        {
            return _cachedIsEffectivelyModded.Value;
        }

        try
        {
            // 获取当前 mod 设置
            var settings = ModSettingsHelper.GetSettings();

            // 根据设置的 ModdedMode 决定是否应视为 modded 状态
            var result = settings.Mode switch
            {
                ModdedMode.Auto => EvaluateAutoMode(isForSavingDir),
                ModdedMode.AlwaysVanilla => false,
                ModdedMode.Default => ModManager.Mods.Any(static m => m.IsLoaded()),
                _ => throw new InvalidOperationException($"Unknown {nameof(ModdedMode)}: {settings.Mode}"),
            };

            // 缓存结果
            if (isForSavingDir)
            {
                _cachedIsEffectivelyModdedForSaveDir = result;
            }
            else
            {
                _cachedIsEffectivelyModded = result;
            }

            // 输出日志并返回结果
            ModLog.Info($"{nameof(IsEffectivelyModded)}({nameof(isForSavingDir)}={isForSavingDir}) => {result} ({nameof(settings.Mode)}={settings.Mode})");
            return result;
        }
        catch (Exception ex)
        {
            // 如果发生异常, 则视为 modded 状态, 并输出警告日志 (不缓存结果)
            ModLog.Warn($"判断 {nameof(IsEffectivelyModded)} 时发生异常, 将视为 modded 状态: {ex}");
            return true;
        }
    }

    /// <summary>
    /// Auto 模式: 遍历所有已加载 mod, 检测是否有 gameplay mod
    /// </summary>
    /// <param name="isForSavingDir">是否为用于存档目录判断</param>
    /// <returns><see langword="true"/> 表示检测到 gameplay mod; <see langword="false"/> 表示未检测到 gameplay mod</returns>
    private static bool EvaluateAutoMode(bool isForSavingDir)
    {
        // 获取所有已加载的 mod (Loaded 或 Failed)
        var loadedMods = ModManager.Mods.Where(static m => m.IsLoaded()).ToList();

        // 如果没有已加载的 Mod, 则视为 vanilla
        if (loadedMods.Count == 0)
        {
            ModLog.Info($"{nameof(ModdedMode.Auto)} 模式: 没有已加载的 Mod, 视为 vanilla");
            return false;
        }

        // 筛选出所有已加载且有 manifest 的 Mod
        var modsWithManifest = loadedMods.Where(static m => m.manifest is not null).ToList();

        // 如果没有已加载的 Mod 有 manifest, 则视为 vanilla
        if (modsWithManifest.Count == 0)
        {
            ModLog.Info($"{nameof(ModdedMode.Auto)} 模式: 没有已加载的 Mod 有 manifest, 视为 vanilla");
            return false;
        }

        // 将所有已加载且有 manifest 的 Mod 分为 gameplay mod 和 non-gameplay mod
        List<Mod> gameplayMods = [];

        // 遍历所有已加载且有 manifest 的 Mod 进行分类
        foreach (var mod in modsWithManifest)
        {
            // 获取 Mod 的 id
            var id = mod.GetId();

            // 白名单中的 Mod 强制视为非 gameplay mod
            if (ModSettingsHelper.IsWhitelisted(id))
            {
                ModLog.Verbose($"{id} 在白名单中, 被视为非 gameplay mod");
                continue;
            }

            // 内容性 Mod 检测的结果
            var result = ContentModDetector.IsContentMod(id);

            // 获取 Mod 的 manifest
            var manifest = mod.manifest!;

            // 在当前模式下该模组是否应被视为 gameplay mod
            var isGameplayMod = isForSavingDir
                ? (result is null ? manifest.affectsGameplay : result.Value)
                : manifest.affectsGameplay || result == true;

            // 如果该模组应被视为 gameplay mod, 则记录日志并加入 gameplayMods 列表
            if (isGameplayMod)
            {
                gameplayMods.Add(mod);
                ModLog.Verbose($"{id} 在 {nameof(isForSavingDir)}={isForSavingDir} 时被视为 gameplay mod");
            }
            else
            {
                ModLog.Verbose($"{id} 在 {nameof(isForSavingDir)}={isForSavingDir} 时被视为非 gameplay mod");
            }
        }

        // 记录日志并返回结果
        ModLog.Info($"{nameof(ModdedMode.Auto)} 模式 ({nameof(isForSavingDir)}={isForSavingDir}): 共检测到 {loadedMods.Count} 个已加载 mod, 其中 {gameplayMods.Count} 个为 gameplay mod");
        return gameplayMods.Count > 0;
    }
}
