using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Compat;
using STS2RitsuLib.Settings;

namespace RespectAffectsGameplay.Scripts;

/// <summary>
/// mod 入口类: 负责初始化设置、注册设置页面并应用所有 Harmony 补丁
/// </summary>
[ModInitializer("Initialize")]
public static class RespectAffectsGameplayMod
{
    /// <summary>
    /// mod 初始化入口: 依次注册持久化设置、注册游戏内设置页面、应用 Harmony 补丁
    /// </summary>
    public static void Initialize()
    {
        // 1. 注册持久化设置数据存储
        ModSettingsHelper.Initialize();

        // 2. 注册游戏内设置页面
        RegisterSettingsPage();

        // 3. 在 Linux 上确保 libgcc_s 已全局加载
        LinuxNativeHelper.EnsureLibGccLoaded();

        // 4. 应用 Harmony 补丁
        var harmony = new Harmony(ModInfo.HarmonyId);
        harmony.PatchAll(typeof(RespectAffectsGameplayMod).Assembly);
    }

    /// <summary>
    /// 判断当前是否应视为 "modded" 状态
    /// </summary>
    /// <returns><see langword="true"/> 表示应视为 modded 状态;
    /// <see langword="false"/> 表示应视为 vanilla 状态</returns>
    /// <exception cref="InvalidOperationException">当 ModdedMode 设置为未知值时抛出</exception>
    internal static bool IsEffectivelyModded()
    {
        try
        {
            // 获取当前 mod 设置
            var settings = ModSettingsHelper.GetSettings();

            // 根据设置的 Modded Mode 决定是否视为 modded 状态
            return settings.Mode switch
            {
                ModdedMode.Auto => RitsuModManager.GetKnownMods().Any(m => m.IsLoaded && m.AffectsGameplay),
                ModdedMode.AlwaysVanilla => false,
                ModdedMode.AlwaysModded => ModManager.Mods.Any(m => m.state is ModLoadState.Loaded or ModLoadState.Failed),
                _ => throw new InvalidOperationException($"Unknown ModdedMode value: {settings.Mode}"),
            };
        }
        catch
        {
            // 出错时保守假设为 modded 状态
            return true;
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
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("General"))
                .AddEnumChoice(
                    "mode",
                    ModSettingsText.Literal("Modded Mode"),
                    new ModSettingsValueBinding<ModSettingsData, ModdedMode>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        s => s.Mode,
                        (s, v) => s.Mode = v),
                    value => value switch
                    {
                        ModdedMode.Auto => ModSettingsText.Literal("自动"),
                        ModdedMode.AlwaysVanilla => ModSettingsText.Literal("始终原版"),
                        ModdedMode.AlwaysModded => ModSettingsText.Literal("始终 Modded"),
                        _ => ModSettingsText.Literal(value.ToString()),
                    },
                    ModSettingsText.Literal(
                        "自动：仅当加载了 affects_gameplay: true 的 mod 时才标记游戏为 modded 状态。\n" +
                        "始终原版：即使加载了 gameplay mod 也永不标记为 modded（可能会导致存档损坏和游戏异常等问题）。\n" +
                        "始终 Modded：只要加载了任意 mod 就始终标记为 modded 状态。"),
                    ModSettingsChoicePresentation.Dropdown)));
    }
}
