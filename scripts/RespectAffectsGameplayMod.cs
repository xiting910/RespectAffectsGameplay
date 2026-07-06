using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Core;
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
    /// mod 初始化入口: 依次注册持久化设置、注册游戏内设置页面、应用 Harmony 补丁
    /// </summary>
    public static void Initialize()
    {
        // 0. 初始化本地化
        ModLoc.Initialize();
        ModLog.Info($"开始初始化 ({nameof(ModInfo.Id)}: {ModInfo.Id}, {nameof(ModInfo.Version)}: {ModInfo.Version})");


        // 1. 加载设置
        ModLog.Verbose("步骤 1: 加载设置...");

        // 获取设置
        var settings = ModSettingsHelper.GetSettings();
        ModLog.Info($"设置已加载 ({nameof(settings.Mode)}={settings.Mode}, {nameof(settings.PatchModManagerIsRunningModded)}={settings.PatchModManagerIsRunningModded}, {nameof(settings.VerboseLogging)}={settings.VerboseLogging})");


        // 2. 注册游戏内设置页面
        ModLog.Verbose("步骤 2: 注册游戏内设置页面...");
        RegisterSettingsPage();


        // 3. 注册并应用 Harmony 补丁 (RitsuLib 已处理 Linux 原生库预加载)
        ModLog.Verbose("步骤 3: 注册并应用补丁...");

        // 创建补丁器
        var patcher = RitsuLibFramework.CreatePatcher(ModInfo.Id, ModInfo.Version);

        // 注册补丁
        patcher.RegisterPatch<PatchGetAccountDir>();
        patcher.RegisterPatch<PatchCopyUnmoddedSaveFilesIfNeeded>();

        // 根据设置决定是否注册 PatchModManagerIsRunningModded 补丁
        if (settings.PatchModManagerIsRunningModded)
        {
            patcher.RegisterPatch<PatchModManagerIsRunningModded>();
            ModLog.Info($"{nameof(PatchModManagerIsRunningModded)} 已启用, 将拦截所有 {nameof(ModManager.IsRunningModded)} 调用");
        }
        else
        {
            ModLog.Info($"{nameof(PatchModManagerIsRunningModded)} 已禁用");
        }

        // 应用补丁并输出日志
        if (patcher.PatchAll())
        {
            ModLog.Info("所有 Harmony 补丁已成功应用");
        }
        else
        {
            ModLog.Warn("部分 Harmony 补丁应用失败, 请检查日志以获取详细信息");
        }


        // 4. 订阅主菜单就绪事件, 补触发存档复制检查
        ModLog.Verbose("步骤 4: 订阅主菜单就绪事件补触发存档复制检查...");
        _ = RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>((evt, sub) =>
        {
            sub.Dispose();
            EnsureSaveFilesCopiedIfNeeded();
        });


        // 输出初始化完成日志
        ModLog.Info($"初始化完成");
    }

    /// <summary>
    /// 检查是否需要补触发存档复制
    /// </summary>
    private static void EnsureSaveFilesCopiedIfNeeded()
    {
        try
        {
            // 如果按照当前设置判断, 不是 gameplay modded 状态, 则无需补触发存档复制
            if (!GameplayStateHelper.IsEffectivelyModded(true))
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
