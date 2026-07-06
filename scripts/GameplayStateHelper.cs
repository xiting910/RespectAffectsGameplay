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
    /// <param name="isForSaveDir">是否为用于存档目录判断</param>
    /// <returns><see langword="true"/> 表示应视为 modded 状态;
    /// <see langword="false"/> 表示应视为 vanilla 状态</returns>
    /// <exception cref="InvalidOperationException">当 ModdedMode 设置为未知值时抛出</exception>
    public static bool IsEffectivelyModded(bool isForSaveDir)
    {
        // 如果是用于存档目录判断, 且缓存值存在, 则直接返回缓存值
        if (isForSaveDir && _cachedIsEffectivelyModdedForSaveDir.HasValue)
        {
            return _cachedIsEffectivelyModdedForSaveDir.Value;
        }

        // 如果不是用于存档目录判断, 且缓存值存在, 则直接返回缓存值
        if (!isForSaveDir && _cachedIsEffectivelyModded.HasValue)
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
                ModdedMode.Auto => isForSaveDir ? ContentModDetector.HasContentModsLoaded() : EvaluateAutoMode(),
                ModdedMode.AlwaysVanilla => false,
                ModdedMode.Default => EvaluateDefaultMode(),
                _ => throw new InvalidOperationException($"Unknown {nameof(ModdedMode)} value: {settings.Mode}"),
            };

            // 缓存结果
            if (isForSaveDir)
            {
                _cachedIsEffectivelyModdedForSaveDir = result;
            }
            else
            {
                _cachedIsEffectivelyModded = result;
            }

            // 输出日志并返回结果
            ModLog.Verbose($"{nameof(IsEffectivelyModded)}({nameof(isForSaveDir)}={isForSaveDir}) => {result} ({nameof(settings.Mode)}={settings.Mode})");
            return result;
        }
        catch (Exception ex)
        {
            // 如果发生异常, 则视为 modded 状态, 缓存结果并输出警告日志
            if (isForSaveDir)
            {
                _cachedIsEffectivelyModdedForSaveDir = true;
            }
            else
            {
                _cachedIsEffectivelyModded = true;
            }
            ModLog.Warn($"判断 {nameof(IsEffectivelyModded)} 时发生异常, 将视为 modded 状态: {ex}");
            return true;
        }
    }

    /// <summary>
    /// Auto 模式: 遍历所有已加载 mod, 检测是否有 gameplay mod
    /// </summary>
    /// <returns><see langword="true"/> 表示检测到 gameplay mod; <see langword="false"/> 表示未检测到 gameplay mod</returns>
    private static bool EvaluateAutoMode()
    {
        // 获取所有已加载的 mod (Loaded 或 Failed)
        var loadedMods = ModManager.Mods.Where(m => m.IsLoaded()).ToList();

        // 如果没有已加载的 Mod, 则视为 vanilla
        if (loadedMods.Count == 0)
        {
            ModLog.Verbose($"{nameof(ModdedMode.Auto)} 模式: 没有已加载的 Mod, 视为 vanilla");
            return false;
        }

        // 筛选出所有已加载且有 manifest 的 Mod
        var modsWithManifest = loadedMods.Where(m => m.manifest is not null).ToList();

        // 如果没有已加载的 Mod 有 manifest, 则视为 vanilla
        if (modsWithManifest.Count == 0)
        {
            ModLog.Verbose($"{nameof(ModdedMode.Auto)} 模式: 没有已加载的 Mod 有 manifest, 视为 vanilla");
            return false;
        }

        // 将所有已加载且有 manifest 的 Mod 分为 gameplay mod 和 non-gameplay mod
        List<Mod> gameplayMods = [], nonGameplayMods = [];

        // 遍历所有已加载且有 manifest 的 Mod 进行分类
        foreach (var mod in modsWithManifest)
        {
            // 获取 Mod 的 manifest
            var manifest = mod.manifest!;

            // 如果 affects_gameplay 标记为 true, 或者是 ContentModDetector 检测到的内容 mod, 则视为 gameplay mod
            if (manifest.affectsGameplay || ContentModDetector.IsContentMod(mod.GetId()))
            {
                gameplayMods.Add(mod);
            }
            else
            {
                nonGameplayMods.Add(mod);
            }
        }

        // 输出日志: Auto 模式下的检测结果
        ModLog.Verbose($"{nameof(ModdedMode.Auto)} 模式: 共检测 {ModManager.Mods.Count} 个 mod, 已加载 {loadedMods.Count} 个 (gameplay: {gameplayMods.Count}, 非 gameplay: {nonGameplayMods.Count})");

        // 是否应该被视为 modded 模式
        var isModded = gameplayMods.Count > 0;
        if (isModded)
        {
            ModLog.Info($"检测到 gameplay mod: [{string.Join(", ", gameplayMods.Select(m => m.GetId()))}]");
        }
        return isModded;
    }

    /// <summary>
    /// Default 模式: 使用游戏原版逻辑
    /// </summary>
    /// <returns><see langword="true"/> 表示游戏原版逻辑认为 modded; <see langword="false"/> 表示游戏原版逻辑认为 vanilla</returns>
    private static bool EvaluateDefaultMode()
    {
        var count = ModManager.Mods.Count(m => m.IsLoaded());
        ModLog.Verbose($"{nameof(ModdedMode.Default)} 模式: 共检测到 {ModManager.Mods.Count} 个 mod, Loaded/Failed: {count}");
        return count > 0;
    }
}
