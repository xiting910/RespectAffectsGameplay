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

        // 5. 条件补丁: 根据用户设置决定是否启用 PatchModManagerIsRunningModded
        if (!ModSettingsHelper.GetSettings().PatchModManagerIsRunningModded)
        {
            harmony.Unpatch(typeof(ModManager).GetMethod(nameof(ModManager.IsRunningModded)),
                typeof(PatchModManagerIsRunningModded).GetMethod(nameof(PatchModManagerIsRunningModded.Prefix)));
        }
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
                ModdedMode.Default => ModManager.Mods.Any(m => m.state is ModLoadState.Loaded or ModLoadState.Failed),
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
    private static void RegisterSettingsPage() =>
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
                        (s, v) => { s.Mode = v; ModSettingsHelper.SaveSettings(); }),
                    value => value switch
                    {
                        ModdedMode.Auto => ModSettingsText.Literal("自动"),
                        ModdedMode.AlwaysVanilla => ModSettingsText.Literal("强制原版"),
                        ModdedMode.Default => ModSettingsText.Literal("游戏默认"),
                        _ => ModSettingsText.Literal(value.ToString()),
                    },
                    ModSettingsText.Literal(
                        "自动：仅当加载了 affects_gameplay: true 的 mod 时才标记游戏为 modded 状态。\n" +
                        "强制原版：即使加载了 gameplay mod 也永不标记为 modded（⚠ 可能导致存档损坏）。\n" +
                        "游戏默认：使用游戏默认逻辑，只要加载了任意 mod 即标记为 modded 状态。\n\n" +
                        "⚠ 修改此选项后需重启游戏才能生效。"),
                    ModSettingsChoicePresentation.Dropdown)
                .AddToggle(
                    "patchModManager",
                    ModSettingsText.Literal("拦截 IsRunningModded()"),
                    new ModSettingsValueBinding<ModSettingsData, bool>(
                        ModInfo.Id,
                        ModSettingsHelper.DataKey,
                        ModSettingsHelper.DataScope,
                        s => s.PatchModManagerIsRunningModded,
                        (s, v) => { s.PatchModManagerIsRunningModded = v; ModSettingsHelper.SaveSettings(); }),
                    ModSettingsText.Literal(
                        "开启：Mod 管理器、联机 Mod 列表、Sentry 上报等所有调用 IsRunningModded() 的位置\n" +
                        "      均受当前 Modded Mode 控制。副作用：主界面 mod 数量和哈希值可能不显示。\n" +
                        "关闭：仅存档路径受当前 Modded Mode 控制，UI 和联机列表恢复原始行为。\n\n" +
                        "⚠ 修改此选项后需重启游戏才能生效。"),
                    null)
                .AddButton(
                    "resetDefaults",
                    ModSettingsText.Literal("重置为默认设置"),
                    ModSettingsText.Literal("恢复默认"),
                    ModSettingsHelper.ResetToDefaults,
                    description: ModSettingsText.Literal(
                        "将所有设置恢复为默认值（Modded Mode → 自动，拦截 IsRunningModded() → 关闭）。\n" +
                        "⚠ 修改后需重启游戏才能生效。"))));
}
