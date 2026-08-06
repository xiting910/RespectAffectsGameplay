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
    /// 设置页面 Section 标识符: 白名单区域
    /// </summary>
    private const string WhitelistSection = "whitelist";

    /// <summary>
    /// 设置页面条目标识符: 白名单 Mod ID 列表 (多行文本)
    /// </summary>
    private const string WhitelistList = "whitelistList";

    /// <summary>
    /// 设置页面条目标识符: 从已加载 Mod 选择添加的下拉框
    /// </summary>
    private const string WhitelistAddSelect = "whitelistAddSelect";

    /// <summary>
    /// 设置页面条目标识符: 添加所选 Mod 按钮
    /// </summary>
    private const string WhitelistAddButton = "whitelistAddButton";

    /// <summary>
    /// 设置页面条目标识符: 从白名单选择移除的下拉框
    /// </summary>
    private const string WhitelistRemoveSelect = "whitelistRemoveSelect";

    /// <summary>
    /// 设置页面条目标识符: 移除所选 Mod 按钮
    /// </summary>
    private const string WhitelistRemoveButton = "whitelistRemoveButton";

    /// <summary>
    /// 设置页面条目标识符: 清空白名单按钮
    /// </summary>
    private const string WhitelistClearButton = "whitelistClearButton";

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
        ModLog.Info($"设置已加载 ({nameof(settings.Mode)}={settings.Mode}, {nameof(settings.WhitelistedModIds)}={settings.WhitelistedModIds.Count}, {nameof(settings.PatchModManagerIsRunningModded)}={settings.PatchModManagerIsRunningModded}, {nameof(settings.VerboseLogging)}={settings.VerboseLogging})");


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
        _ = RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(static (evt, sub) =>
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

            // 调用 ModManager.CopyUnmoddedSaveFilesIfNeeded() 方法, 补触发存档复制检查
            ModManager.CopyUnmoddedSaveFilesIfNeeded();

            // 记录日志: 补触发存档复制检查
            ModLog.Info("当前为 gameplay modded 状态, 且游戏未完成首次存档复制, 已补触发存档复制检查");
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
        var addSelectBinding = new InMemoryModSettingsValueBinding<string>(
            ModInfo.Id,
            ModSettingsHelper.DataKey,
            string.Empty
        );
        var removeSelectBinding = new InMemoryModSettingsValueBinding<string>(
            ModInfo.Id,
            ModSettingsHelper.DataKey,
            string.Empty
        );

        RitsuLibFramework.RegisterModSettings(ModInfo.Id, page => page
            .WithTitle(ModSettingsText.Literal(ModInfo.Name))
            .WithModDisplayName(ModSettingsText.Literal(ModInfo.Name))
            .WithDescription(ModSettingsText.I18N(ModLoc.Instance, "mod.description", string.Empty))
            .AddSection(SectionGeneral, static section => section
                .WithTitle(ModSettingsText.I18N(ModLoc.Instance, "settings.section.general", "General"))
                .AddEnumChoice(
                    ChoiceMode,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.mode.label", "Modded Mode"),
                    new ModSettingsValueBinding<ModSettingsData, ModdedMode>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        static s => s.Mode,
                        static (s, v) => { s.Mode = v; ModSettingsHelper.SaveSettings(); }),
                    static value => value switch
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
                        static s => s.PatchModManagerIsRunningModded,
                        static (s, v) => { s.PatchModManagerIsRunningModded = v; ModSettingsHelper.SaveSettings(); }),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.patchModManager.desc", string.Empty))
                .AddToggle(
                    ToggleVerboseLogging,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.verboseLogging.label", "Verbose Logging"),
                    new ModSettingsValueBinding<ModSettingsData, bool>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        static s => s.VerboseLogging,
                        static (s, v) =>
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
                    description: ModSettingsText.I18N(ModLoc.Instance, "settings.resetDefaults.desc", string.Empty)))
            .AddSection(WhitelistSection, section => section
                .WithTitle(ModSettingsText.I18N(ModLoc.Instance, "settings.section.whitelist", "Whitelist"))
                .AddInfoCard(
                    WhitelistList,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.list.label", "Current whitelist"),
                    ModSettingsText.Dynamic(GetWhitelistDisplayText),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.list.desc", string.Empty))
                .AddChoice(
                    WhitelistAddSelect,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.add.label", "Add from loaded mods"),
                    addSelectBinding,
                    GetLoadedModChoiceOptions(),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.add.desc", string.Empty),
                    ModSettingsChoicePresentation.Dropdown)
                .AddButton(
                    WhitelistAddButton,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.add.buttonLabel", "Add selected mod"),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.add.button", "Add"),
                    host =>
                    {
                        if (ModSettingsHelper.AddToWhitelist(addSelectBinding.Read()))
                        {
                            host.RequestRefresh();
                        }
                    })
                .AddDynamicChoice(
                    WhitelistRemoveSelect,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.remove.label", "Remove from whitelist"),
                    removeSelectBinding,
                    GetWhitelistChoiceOptions,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.remove.desc", string.Empty),
                    ModSettingsChoicePresentation.Dropdown)
                .AddButton(
                    WhitelistRemoveButton,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.remove.buttonLabel", "Remove selected mod"),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.remove.button", "Remove"),
                    host =>
                    {
                        if (ModSettingsHelper.RemoveFromWhitelist(removeSelectBinding.Read()))
                        {
                            host.RequestRefresh();
                        }
                    })
                .AddButton(
                    WhitelistClearButton,
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.clear.label", "Clear whitelist"),
                    ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.clear.button", "Clear"),
                    host =>
                    {
                        if (ModSettingsHelper.ClearWhitelist())
                        {
                            host.RequestRefresh();
                        }
                    },
                    description: ModSettingsText.I18N(ModLoc.Instance, "settings.whitelist.clear.desc", string.Empty))));

        ModLog.Verbose("设置页面注册完成");
    }

    /// <summary>
    /// 生成白名单的实时显示文本
    /// </summary>
    /// <returns>白名单显示文本</returns>
    private static string GetWhitelistDisplayText()
    {
        var ids = ModSettingsHelper.GetSettings().WhitelistedModIds;
        return ids.Count == 0
            ? ModLoc.Instance.Get("settings.whitelist.list.empty", "(Empty)")
            : string.Join("\n", ids.Select(static id => $"• {id}"));
    }

    /// <summary>
    /// 生成所有已加载 Mod 的选项列表, 用于添加白名单的下拉选择
    /// </summary>
    /// <returns>已加载 Mod 的选项列表</returns>
    private static IEnumerable<ModSettingsChoiceOption<string>> GetLoadedModChoiceOptions()
    {
        return ModManager.Mods.Where(static m => m.IsLoaded())
            .Select(static m =>
                new ModSettingsChoiceOption<string>(m.GetId(), ModSettingsText.Literal(m.GetId()))
            );
    }

    /// <summary>
    /// 生成当前白名单中 Mod 的选项列表, 用于移除白名单的下拉选择
    /// </summary>
    /// <returns>白名单 Mod 的选项列表</returns>
    private static IReadOnlyList<ModSettingsChoiceOption<string>> GetWhitelistChoiceOptions()
    {
        return [.. ModSettingsHelper.GetSettings().WhitelistedModIds
            .Select(static id => new ModSettingsChoiceOption<string>(id, ModSettingsText.Literal(id)))];
    }
}
