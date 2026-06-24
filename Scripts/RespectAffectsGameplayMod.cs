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
        ModLog.Info($"开始初始化 (ID: {ModInfo.Id}, Version: {ModInfo.Version})");

        // 1. 注册持久化设置数据存储
        ModLog.Debug("步骤 1/5: 注册持久化设置...");
        ModSettingsHelper.Initialize();

        // 读取设置
        var settings = ModSettingsHelper.GetSettings();
        if (settings.VerboseLogging)
        {
            ModLog.Info("详细日志已启用");
        }

        // 2. 注册游戏内设置页面
        ModLog.Debug("步骤 2/5: 注册游戏内设置页面...");
        RegisterSettingsPage();

        // 3. 在 Linux 上确保 libgcc_s 已全局加载
        ModLog.Debug("步骤 3/5: 检查 Linux 原生库...");
        LinuxNativeHelper.EnsureLibGccLoaded();

        // 4. 应用 Harmony 补丁
        ModLog.Debug("步骤 4/5: 应用 Harmony 补丁...");
        var harmony = new Harmony(ModInfo.HarmonyId);
        harmony.PatchAll(typeof(RespectAffectsGameplayMod).Assembly);
        var patchedMethods = harmony.GetPatchedMethods();
        ModLog.Info($"Harmony 补丁已应用, 本 mod 共 {patchedMethods.Count()} 个补丁方法:" +
            string.Concat(patchedMethods.Select(m => $"\n  - {m.DeclaringType?.FullName}.{m.Name}")));

        // 5. 条件补丁: 根据用户设置决定是否启用 PatchModManagerIsRunningModded
        if (!settings.PatchModManagerIsRunningModded)
        {
            ModLog.Info("PatchModManagerIsRunningModded 已禁用 (用户设置), 执行 Unpatch...");
            harmony.Unpatch(typeof(ModManager).GetMethod(nameof(ModManager.IsRunningModded)),
                typeof(PatchModManagerIsRunningModded).GetMethod(nameof(PatchModManagerIsRunningModded.Prefix)));
        }
        else
        {
            ModLog.Info("PatchModManagerIsRunningModded 已启用, 将拦截所有 IsRunningModded() 调用");
        }

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
        try
        {
            // 获取当前 mod 设置
            var settings = ModSettingsHelper.GetSettings();

            // 根据设置的 Modded Mode 决定是否视为 modded 状态
            var result = settings.Mode switch
            {
                ModdedMode.Auto => EvaluateAutoMode(),
                ModdedMode.AlwaysVanilla => false,
                ModdedMode.Default => EvaluateDefaultMode(),
                _ => throw new InvalidOperationException($"Unknown ModdedMode value: {settings.Mode}"),
            };

            ModLog.Debug($"IsEffectivelyModded() = {result} (Mode={settings.Mode})");
            return result;
        }
        catch (Exception ex)
        {
            // 出错时保守假设为 modded 状态
            ModLog.Error($"IsEffectivelyModded() 异常, 保守假设为 modded: {ex}");
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
        var loadedMods = knownMods.Where(m => m.IsLoaded).ToList();
        var gameplayMods = loadedMods.Where(m => m.AffectsGameplay).ToList();
        var nonGameplayMods = loadedMods.Where(m => !m.AffectsGameplay).ToList();

        ModLog.Debug($"Auto 模式: 共检测 {knownMods.Count} 个 mod, " +
        $"已加载 {loadedMods.Count} 个 (gameplay: {gameplayMods.Count}, 非 gameplay: {nonGameplayMods.Count})");

        if (gameplayMods.Count > 0)
        {
            ModLog.Info($"检测到 gameplay mod: [{string.Join(", ", gameplayMods.Select(m => m.Id))}]");
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
        ModLog.Debug($"Default 模式: ModManager.Mods 共 {ModManager.Mods.Count} 个, " + $"Loaded/Failed: {loadedOrFailed.Count}");
        return loadedOrFailed.Count > 0;
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
                .AddToggle(
                    "verboseLogging",
                    ModSettingsText.Literal("详细日志"),
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
                    ModSettingsText.Literal(
                        "开启：输出详细的 Debug 级别日志, 方便排查问题。\n" +
                        "关闭：仅输出 Warn / Error 日志。\n\n" +
                        "⚠ 仅影响本 mod, 不影响游戏或其他 mod 的日志输出。修改后即时生效。"),
                    null)
                .AddButton(
                    "resetDefaults",
                    ModSettingsText.Literal("重置为默认设置"),
                    ModSettingsText.Literal("恢复默认"),
                    ModSettingsHelper.ResetToDefaults,
                    description: ModSettingsText.Literal(
                        "将所有设置恢复为默认值。\n" +
                        "⚠ 某些修改需重启游戏才能生效。"))));

        ModLog.Debug("设置页面注册完成");
    }
}
