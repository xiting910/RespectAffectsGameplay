using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
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
    /// RitsuLib 的联机哈希补丁类型名称
    /// </summary>
    private const string RitsuLibHashPatchTypeName = "STS2RitsuLib.Content.Patches.ModelIdSerializationCacheDynamicContentPatch";

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
    /// mod 初始化入口: 依次注册持久化设置、注册游戏内设置页面、应用 Harmony 补丁
    /// </summary>
    public static void Initialize()
    {
        // 0. 初始化本地化
        ModLoc.Initialize();
        ModLog.Info($"开始初始化 (ID: {ModInfo.Id}, Version: {ModInfo.Version})");

        // 1. 注册持久化设置数据存储
        ModLog.Debug("步骤 1: 注册持久化设置...");
        ModSettingsHelper.Initialize();

        // 读取设置
        var settings = ModSettingsHelper.GetSettings();
        if (settings.VerboseLogging)
        {
            ModLog.Info("详细日志已启用");
        }

        // 2. 注册游戏内设置页面
        ModLog.Debug("步骤 2: 注册游戏内设置页面...");
        RegisterSettingsPage();

        // 3. 在 Linux 上确保 libgcc_s 已全局加载
        ModLog.Debug("步骤 3: 检查 Linux 原生库...");
        LinuxNativeHelper.EnsureLibGccLoaded();

        // 4. 应用 Harmony 补丁
        ModLog.Debug("步骤 4: 应用 Harmony 补丁...");
        var harmony = new Harmony(ModInfo.HarmonyId);
        harmony.PatchAll(typeof(RespectAffectsGameplayMod).Assembly);
        var patchedMethods = harmony.GetPatchedMethods();
        ModLog.Info($"Harmony 补丁已应用, 本 mod 共 {patchedMethods.Count()} 个补丁方法:" +
            string.Concat(patchedMethods.Select(m => $"\n  - {m.DeclaringType?.FullName}.{m.Name}")));

        // 5. 检测 RitsuLib 的联机哈希补丁, 避免冲突
        ModLog.Debug("步骤 5: 检测 RitsuLib 的联机哈希补丁...");
        if (AccessTools.TypeByName(RitsuLibHashPatchTypeName) is not null)
        {
            harmony.Unpatch(
                AccessTools.Method(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init)),
                HarmonyPatchType.All,
                ModInfo.HarmonyId);
            harmony.Unpatch(
                AccessTools.PropertyGetter(typeof(ModManager), nameof(ModManager.Mods)),
                HarmonyPatchType.All,
                ModInfo.HarmonyId);

            ModLog.Info($"联机哈希补丁 {nameof(PatchModelIdSerializationCache)} 已禁用");
        }
        else
        {
            ModLog.Debug($"未检测到 RitsuLib 的联机哈希补丁, 无需禁用 {nameof(PatchModelIdSerializationCache)}");
        }

        // 6. 条件补丁: 根据用户设置决定是否启用 PatchModManagerIsRunningModded
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

        // 7. 验证所有 Mod 的 affects_gameplay 标记是否准确
        ModLog.Debug("步骤 7: 验证所有 mods 的 affects_gameplay 标记...");
        ModAffectsGameplayValidator.ValidateAll();

        // 输出初始化完成日志
        ModLog.Info($"初始化完成 (Mode={settings.Mode}, PatchModManager={settings.PatchModManagerIsRunningModded})");
    }

    /// <summary>
    /// 判断当前是否应视为 "modded" 状态
    /// </summary>
    /// <returns><see langword="true"/> 表示应视为 modded 状态;
    /// <see langword="false"/> 表示应视为 vanilla 状态</returns>
    /// <exception cref="InvalidOperationException">当 ModdedMode 设置为未知值时抛出</exception>
    internal static bool IsEffectivelyModded()
    {
        if (_cachedIsEffectivelyModded.HasValue) { return _cachedIsEffectivelyModded.Value; }

        try
        {
            var settings = ModSettingsHelper.GetSettings();

            var result = settings.Mode switch
            {
                ModdedMode.Auto => EvaluateAutoMode(),
                ModdedMode.AlwaysVanilla => false,
                ModdedMode.Default => EvaluateDefaultMode(),
                _ => throw new InvalidOperationException($"Unknown ModdedMode value: {settings.Mode}"),
            };

            _cachedIsEffectivelyModded = result;

            ModLog.Debug($"IsEffectivelyModded() = {result} (Mode={settings.Mode})");
            return result;
        }
        catch (Exception ex)
        {
            ModLog.Error($"IsEffectivelyModded() 异常, 保守假设为 modded: {ex}");
            _cachedIsEffectivelyModded = true;
            return true;
        }
    }

    /// <summary>
    /// Auto 模式: 遍历所有已加载 mod, 检测是否有 gameplay mod
    /// </summary>
    private static bool EvaluateAutoMode()
    {
        // 获取所有已加载的 mod (Loaded 或 Failed)
        var loadedMods = ModManager.Mods.Where(m => m.state is ModLoadState.Loaded or ModLoadState.Failed);

        // 如果没有已加载的 Mod, 则视为 vanilla
        if (!loadedMods.Any())
        {
            ModLog.Debug("Auto 模式: 没有已加载的 Mod, 视为 vanilla");
            return false;
        }

        // 筛选出所有已加载且有 manifest 的 Mod
        var modsWithManifest = loadedMods.Where(m => m.manifest is not null);

        // 如果没有已加载的 Mod 有 manifest, 则视为 vanilla
        if (!modsWithManifest.Any())
        {
            ModLog.Debug("Auto 模式: 没有已加载的 Mod 有 manifest, 视为 vanilla");
            return false;
        }

        // 将所有已加载且有 manifest 的 Mod 分为 gameplay mod 和 non-gameplay mod
        List<Mod> gameplayMods = [], nonGameplayMods = [];

        // 遍历所有已加载且有 manifest 的 Mod 进行分类
        foreach (var mod in modsWithManifest)
        {
            // 获取 Mod 的 manifest
            var manifest = mod.manifest!;

            // 获取 mod 的 ID, 如果没有 ID 则使用 name, 如果都没有则使用 "<unknown>"
            var modId = manifest.id ?? manifest.name ?? "<unknown>";

            // 如果 affects_gameplay 标记为 true, 或者在 MislabeledGameplayMods 中, 则视为 gameplay mod
            if (manifest.affectsGameplay || ModAffectsGameplayValidator.MislabeledGameplayMods.Contains(modId))
            {
                gameplayMods.Add(mod);
            }
            else
            {
                nonGameplayMods.Add(mod);
            }
        }

        // 输出日志: Auto 模式下的检测结果
        ModLog.Debug($"Auto 模式: 共检测 {ModManager.Mods.Count} 个 mod, 已加载 {loadedMods.Count()} 个 (gameplay: {gameplayMods.Count}, 非 gameplay: {nonGameplayMods.Count})");

        // 是否应该被视为 modded 模式
        var isModded = gameplayMods.Count > 0;
        if (isModded)
        {
            ModLog.Info($"检测到 gameplay mod: [{string.Join(", ", gameplayMods.Select(m => m.manifest?.id ?? m.manifest?.name ?? "<unknown>"))}]");
        }
        return isModded;
    }

    /// <summary>
    /// Default 模式: 使用游戏原版逻辑
    /// </summary>
    /// <returns><see langword="true"/> 表示游戏原版逻辑认为 modded; <see langword="false"/> 表示游戏原版逻辑认为 vanilla</returns>
    private static bool EvaluateDefaultMode()
    {
        var count = ModManager.Mods.Count(m => m.state is ModLoadState.Loaded or ModLoadState.Failed);
        ModLog.Debug($"Default 模式: ModManager.Mods 共 {ModManager.Mods.Count} 个, Loaded/Failed: {count}");
        return count > 0;
    }

    /// <summary>
    /// 注册游戏内 mod 设置页面
    /// </summary>
    private static void RegisterSettingsPage()
    {
        ModLog.Debug("注册 RitsuLib 设置页面...");

        RitsuLibFramework.RegisterModSettings(ModInfo.Id, page => page
            .WithTitle(ModSettingsText.Literal(ModInfo.Name))
            .WithModDisplayName(ModSettingsText.Literal(ModInfo.Name))
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

        ModLog.Debug("设置页面注册完成");
    }
}
