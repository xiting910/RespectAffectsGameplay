using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Compat;
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
    /// mod 初始化入口: 依次注册持久化设置、注册游戏内设置页面、应用 Harmony 补丁
    /// </summary>
    public static void Initialize()
    {
        // 0. 初始化本地化
        ModLoc.Initialize();
        ModLog.Info(ModLoc.LogInitStart(ModInfo.Id, ModInfo.Version));

        // 1. 注册持久化设置数据存储
        ModLog.Debug(ModLoc.LogStep(1, 5, "注册持久化设置"));
        ModSettingsHelper.Initialize();

        // 读取设置
        var settings = ModSettingsHelper.GetSettings();
        if (settings.VerboseLogging)
        {
            ModLog.Info(ModLoc.LogVerboseEnabled);
        }

        // 2. 注册游戏内设置页面
        ModLog.Debug(ModLoc.LogStep(2, 5, "注册游戏内设置页面"));
        RegisterSettingsPage();

        // 3. 在 Linux 上确保 libgcc_s 已全局加载
        ModLog.Debug(ModLoc.LogStep(3, 5, "检查 Linux 原生库"));
        LinuxNativeHelper.EnsureLibGccLoaded();

        // 4. 应用 Harmony 补丁
        ModLog.Debug(ModLoc.LogStep(4, 5, "应用 Harmony 补丁"));
        var harmony = new Harmony(ModInfo.HarmonyId);
        harmony.PatchAll(typeof(RespectAffectsGameplayMod).Assembly);
        var patchedMethods = harmony.GetPatchedMethods();
        ModLog.Info(ModLoc.LogPatchesApplied(patchedMethods.Count()) +
            string.Concat(patchedMethods.Select(m => $"\n  - {m.DeclaringType?.FullName}.{m.Name}")));

        // 5. 条件补丁: 根据用户设置决定是否启用 PatchModManagerIsRunningModded
        if (!settings.PatchModManagerIsRunningModded)
        {
            ModLog.Info(ModLoc.LogPatchModManagerDisabled);
            harmony.Unpatch(typeof(ModManager).GetMethod(nameof(ModManager.IsRunningModded)),
                typeof(PatchModManagerIsRunningModded).GetMethod(nameof(PatchModManagerIsRunningModded.Prefix)));
        }
        else
        {
            ModLog.Info(ModLoc.LogPatchModManagerEnabled);
        }

        ModLog.Info(ModLoc.LogInitComplete(settings.Mode.ToString(), settings.PatchModManagerIsRunningModded));
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
    /// Auto 模式: 遍历所有已知 mod, 检测是否有已加载的 gameplay mod
    /// </summary>
    /// <returns><see langword="true"/> 表示有已加载的 gameplay mod; <see langword="false"/> 表示没有</returns>
    private static bool EvaluateAutoMode()
    {
        var knownMods = RitsuModManager.GetKnownMods();

        // 如果 RitsuLib 未返回任何已知 mod (例如 RitsuLib 不可用), 回退到 Default 模式以保守评估
        if (knownMods.Count == 0)
        {
            ModLog.Warn(ModLoc.LogAutoFallback);
            return EvaluateDefaultMode();
        }

        // 过滤掉 Steam Workshop 重复条目 (本地已有同名 mod 时 Workshop 版本会被标记为 DisabledDuplicate)
        var loadedMods = knownMods
            .Where(m => m.IsLoaded && !m.IsDisabledSteamWorkshopDuplicate)
            .ToList();
        var gameplayMods = loadedMods.Where(m => m.AffectsGameplay).ToList();
        var nonGameplayMods = loadedMods.Where(m => !m.AffectsGameplay).ToList();

        ModLog.Debug(ModLoc.LogAutoMode(knownMods.Count, loadedMods.Count, gameplayMods.Count, nonGameplayMods.Count));

        if (gameplayMods.Count > 0)
        {
            ModLog.Info(ModLoc.LogGameplayModsDetected(string.Join(", ", gameplayMods.Select(m => m.Id))));
        }

        return gameplayMods.Count > 0;
    }

    /// <summary>
    /// Default 模式: 使用游戏原版逻辑
    /// </summary>
    /// <returns><see langword="true"/> 表示游戏原版逻辑认为 modded; <see langword="false"/> 表示游戏原版逻辑认为 vanilla</returns>
    private static bool EvaluateDefaultMode()
    {
        var loadedOrFailed = ModManager.Mods.Where(m => m.state is ModLoadState.Loaded or ModLoadState.Failed).ToList();
        ModLog.Debug(ModLoc.LogDefaultMode(ModManager.Mods.Count, loadedOrFailed.Count));
        return loadedOrFailed.Count > 0;
    }

    /// <summary>
    /// 注册游戏内 mod 设置页面
    /// </summary>
    private static void RegisterSettingsPage()
    {
        ModLog.Debug(ModLoc.LogRegisteringSettings);

        RitsuLibFramework.RegisterModSettings(ModInfo.Id, page => page
            .WithTitle(ModSettingsText.Literal(ModInfo.Name))
            .WithModDisplayName(ModSettingsText.Literal(ModInfo.Name))
            .AddSection(SectionGeneral, section => section
                .WithTitle(ModSettingsText.Literal(ModLoc.SettingsTitleGeneral))
                .AddEnumChoice(
                    ChoiceMode,
                    ModSettingsText.Literal(ModLoc.SettingsTitleModdedMode),
                    new ModSettingsValueBinding<ModSettingsData, ModdedMode>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        s => s.Mode,
                        (s, v) => { s.Mode = v; ModSettingsHelper.SaveSettings(); }),
                    value => value switch
                    {
                        ModdedMode.Auto => ModSettingsText.Literal(ModLoc.ModeOptionAuto),
                        ModdedMode.AlwaysVanilla => ModSettingsText.Literal(ModLoc.ModeOptionAlwaysVanilla),
                        ModdedMode.Default => ModSettingsText.Literal(ModLoc.ModeOptionDefault),
                        _ => ModSettingsText.Literal(value.ToString()),
                    },
                    ModSettingsText.Literal(ModLoc.DescModdedMode),
                    ModSettingsChoicePresentation.Dropdown)
                .AddToggle(
                    TogglePatchModManager,
                    ModSettingsText.Literal(ModLoc.SettingsTitlePatchModManager),
                    new ModSettingsValueBinding<ModSettingsData, bool>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        s => s.PatchModManagerIsRunningModded,
                        (s, v) => { s.PatchModManagerIsRunningModded = v; ModSettingsHelper.SaveSettings(); }),
                    ModSettingsText.Literal(ModLoc.DescPatchModManager))
                .AddToggle(
                    ToggleVerboseLogging,
                    ModSettingsText.Literal(ModLoc.SettingsTitleVerboseLogging),
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
                    ModSettingsText.Literal(ModLoc.DescVerboseLogging))
                .AddButton(
                    ButtonResetDefaults,
                    ModSettingsText.Literal(ModLoc.SettingsTitleResetDefaults),
                    ModSettingsText.Literal(ModLoc.SettingsButtonReset),
                    ModSettingsHelper.ResetToDefaults,
                    description: ModSettingsText.Literal(ModLoc.DescResetDefaults))));

        ModLog.Debug(ModLoc.LogSettingsRegistered);
    }
}
