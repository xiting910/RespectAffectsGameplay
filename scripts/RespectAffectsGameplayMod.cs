using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace RespectAffectsGameplay;

/// <summary>
/// mod 入口类: 负责初始化设置、注册设置页面并应用所有 Harmony 补丁
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class RespectAffectsGameplayMod
{
    /// <summary>
    /// 设置页面 Section 标识符: 通用设置区域
    /// </summary>
    private const string SectionGeneral = "general";

    /// <summary>
    /// 设置页面 EnumChoice 标识符: Modded 模式选择
    /// </summary>
    private const string ChoiceMode = "mode";

    /// <summary>
    /// 设置页面 Toggle 标识符: 拦截 IsRunningModded() 开关
    /// </summary>
    private const string TogglePatchModManager = "patchModManager";

    /// <summary>
    /// 设置页面 Toggle 标识符: 详细日志开关
    /// </summary>
    private const string ToggleVerboseLogging = "verboseLogging";

    /// <summary>
    /// 设置页面 Button 标识符: 重置为默认设置按钮
    /// </summary>
    private const string ButtonResetDefaults = "resetDefaults";

    /// <summary>
    /// 缓存 <see cref="IsEffectivelyModded"/> 的结果
    /// </summary>
    private static bool? _cachedIsEffectivelyModded;

    /// <summary>
    /// 缓存 <see cref="IsEffectivelyModdedForSaveDir"/> 的结果
    /// </summary>
    private static bool? _cachedIsEffectivelyModdedForSaveDir;

    /// <summary>
    /// mod 初始化入口: 依次注册持久化设置、注册游戏内设置页面、应用 Harmony 补丁
    /// </summary>
    public static void Initialize()
    {
        // 0. 初始化本地化
        ModLoc.Initialize();
        ModLog.Info($"开始初始化 (ID: {ModInfo.Id}, Version: {ModInfo.Version})");

        // 1. 加载设置
        ModLog.Verbose("步骤 1: 加载设置...");
        var settings = ModSettingsHelper.GetSettings();
        ModLog.Info($"设置已加载 (Mode={settings.Mode}, PatchModManager={settings.PatchModManagerIsRunningModded})");

        // 2. 注册游戏内设置页面
        ModLog.Verbose("步骤 2: 注册游戏内设置页面...");
        RegisterSettingsPage();

        // 3. 在 Linux 上确保 libgcc_s 已全局加载
        ModLog.Verbose("步骤 3: 检查 Linux 原生库...");
        LinuxNativeHelper.EnsureLibGccLoaded();

        // 4. 应用 Harmony 补丁
        ModLog.Verbose("步骤 4: 应用 Harmony 补丁...");
        var harmony = new Harmony(ModInfo.HarmonyId);
        harmony.PatchAll(typeof(RespectAffectsGameplayMod).Assembly);
        var patchedMethods = harmony.GetPatchedMethods();
        ModLog.Info($"Harmony 补丁已应用, 本 mod 共 {patchedMethods.Count()} 个补丁方法:" +
            string.Concat(patchedMethods.Select(m => $"\n  - {m.DeclaringType?.FullName}.{m.Name}")));

        // 5. 条件补丁: 根据用户设置决定是否启用 PatchModManagerIsRunningModded
        if (!settings.PatchModManagerIsRunningModded)
        {
            ModLog.Info($"{nameof(PatchModManagerIsRunningModded)} 已禁用");
            harmony.Unpatch(typeof(ModManager).GetMethod(nameof(ModManager.IsRunningModded)),
                typeof(PatchModManagerIsRunningModded).GetMethod(nameof(PatchModManagerIsRunningModded.Prefix)));
        }
        else
        {
            ModLog.Info($"{nameof(PatchModManagerIsRunningModded)} 已启用, 将拦截所有 {nameof(ModManager.IsRunningModded)} 调用");
        }

        // 6. 订阅主菜单就绪事件, 补触发存档复制检查
        ModLog.Verbose("步骤 6: 订阅主菜单就绪事件补触发存档复制检查...");
        _ = RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>((evt, sub) =>
        {
            sub.Dispose();
            EnsureSaveFilesCopiedIfNeeded();
        });

        // 输出初始化完成日志
        ModLog.Info($"初始化完成 (Mode={settings.Mode}, PatchModManager={settings.PatchModManagerIsRunningModded})");
    }

    /// <summary>
    /// 判断当前是否应视为 "modded" 状态
    /// </summary>
    /// <param name="isForSaveDir">是否为用于存档目录判断</param>
    /// <returns><see langword="true"/> 表示应视为 modded 状态;
    /// <see langword="false"/> 表示应视为 vanilla 状态</returns>
    /// <exception cref="InvalidOperationException">当 ModdedMode 设置为未知值时抛出</exception>
    internal static bool IsEffectivelyModded(bool isForSaveDir)
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
                _ => throw new InvalidOperationException($"Unknown ModdedMode value: {settings.Mode}"),
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
            ModLog.Warn($"判断 IsEffectivelyModded 时发生异常, 将视为 modded 状态: {ex}");
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
            ModLog.Verbose("Auto 模式: 没有已加载的 Mod, 视为 vanilla");
            return false;
        }

        // 筛选出所有已加载且有 manifest 的 Mod
        var modsWithManifest = loadedMods.Where(m => m.manifest is not null).ToList();

        // 如果没有已加载的 Mod 有 manifest, 则视为 vanilla
        if (modsWithManifest.Count == 0)
        {
            ModLog.Verbose("Auto 模式: 没有已加载的 Mod 有 manifest, 视为 vanilla");
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
        ModLog.Verbose($"Auto 模式: 共检测 {ModManager.Mods.Count} 个 mod, 已加载 {loadedMods.Count} 个 (gameplay: {gameplayMods.Count}, 非 gameplay: {nonGameplayMods.Count})");

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
        ModLog.Verbose($"Default 模式: ModManager.Mods 共 {ModManager.Mods.Count} 个, Loaded/Failed: {count}");
        return count > 0;
    }

    /// <summary>
    /// 检查是否需要补触发存档复制
    /// </summary>
    private static void EnsureSaveFilesCopiedIfNeeded()
    {
        try
        {
            // 如果按照当前设置判断, 不是 gameplay modded 状态, 则无需补触发存档复制
            if (!IsEffectivelyModded(true))
            {
                ModLog.Verbose("当前不是 gameplay modded 状态, 无需补触发存档复制");
                return;
            }

            // 如果游戏已完成首次存档复制, 则无需补触发存档复制
            if (ModManager.UnmoddedSavesWereCopied)
            {
                ModLog.Verbose("游戏已完成首次存档复制, 无需补触发");
                return;
            }

            // 调用 ModManager.CopyUnmoddedSaveFilesIfNeeded() 方法, 补触发存档复制
            ModManager.CopyUnmoddedSaveFilesIfNeeded();

            // 记录日志: 补触发存档复制
            ModLog.Info("当前为 gameplay modded 状态, 且游戏未完成首次存档复制, 已补触发存档复制");
        }
        catch (Exception ex)
        {
            ModLog.Warn($"补触发存档复制时发生异常 (不影响 mod 核心功能): {ex}");
        }
    }

    /// <summary>
    /// 注册游戏内 mod 设置页面
    /// </summary>
    private static void RegisterSettingsPage()
    {
        RitsuLibFramework.RegisterModSettings(ModInfo.Id, page => page
            .WithTitle(ModSettingsText.Literal(ModInfo.Name))
            .WithModDisplayName(ModSettingsText.Literal(ModInfo.Name))
            .WithDescription(ModSettingsText.I18N(ModLoc.Instance, "mod.description", string.Empty))
            .AddSection(SectionGeneral, section => section
                .WithTitle(ModSettingsText.I18N(ModLoc.Instance, "settings.section.general", "General"))
                .AddEnumChoice(
                    ChoiceMode,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.mode.label", "Modded Mode"),
                    new ModSettingsValueBinding<ModSettingsData, ModdedMode>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        s => s.Mode,
                        (s, v) => { s.Mode = v; ModSettingsHelper.SaveSettings(); }),
                    value => value switch
                    {
                        ModdedMode.Auto => ModSettingsText.I18N(ModLoc.Instance, "settings.mode.option.auto", "Auto"),
                        ModdedMode.AlwaysVanilla => ModSettingsText.I18N(ModLoc.Instance, "settings.mode.option.alwaysVanilla", "Always Vanilla"),
                        ModdedMode.Default => ModSettingsText.I18N(ModLoc.Instance, "settings.mode.option.default", "Game Default"),
                        _ => ModSettingsText.Literal(value.ToString()),
                    },
                    ModSettingsText.I18N(ModLoc.Instance, "settings.mode.desc", string.Empty),
                    ModSettingsChoicePresentation.Dropdown)
                .AddToggle(
                    TogglePatchModManager,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.patchModManager.label", "Intercept IsRunningModded()"),
                    new ModSettingsValueBinding<ModSettingsData, bool>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        s => s.PatchModManagerIsRunningModded,
                        (s, v) => { s.PatchModManagerIsRunningModded = v; ModSettingsHelper.SaveSettings(); }),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.patchModManager.desc", string.Empty))
                .AddToggle(
                    ToggleVerboseLogging,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.verboseLogging.label", "Verbose Logging"),
                    new ModSettingsValueBinding<ModSettingsData, bool>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        s => s.VerboseLogging,
                        (s, v) =>
                        {
                            s.VerboseLogging = v;
                            ModSettingsHelper.SaveSettings();
                        }),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.verboseLogging.desc", string.Empty))
                .AddButton(
                    ButtonResetDefaults,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.resetDefaults.label", "Reset to Defaults"),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.resetDefaults.button", "Restore Defaults"),
                    ModSettingsHelper.ResetToDefaults,
                    description: ModSettingsText.I18N(ModLoc.Instance, "settings.resetDefaults.desc", string.Empty))));

        ModLog.Verbose("设置页面注册完成");
    }
}
