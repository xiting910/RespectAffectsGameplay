using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Godot;

namespace RespectAffectsGameplay;

/// <summary>
/// 本地化辅助类
/// </summary>
internal static class ModLoc
{
    /// <summary>
    /// 已加载的本地化键值对
    /// </summary>
    private static Dictionary<string, string> _entries = [];

    /// <summary>
    /// 当前语言代码 (如 "eng", "zhs")
    /// </summary>
    private static string _language = "eng";

    /// <summary>
    /// 是否已初始化
    /// </summary>
    private static bool _initialized;

    /// <summary>
    /// 初始化本地化: 检测游戏语言并从对应 JSON 文件加载文本
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) { return; }
        _initialized = true;

        _language = DetectLanguage();
        LoadLocalization(_language);
    }

    /// <summary>
    /// 检测当前语言, 返回 STS2 标准的语言代码。
    /// 优先使用 <see cref="OS.GetLocale"/> 读取玩家在游戏内设置的语言,
    /// Godot API 不可用时 (如 CI 环境) 回退到 <see cref="CultureInfo.CurrentUICulture"/>。
    /// </summary>
    /// <returns>STS2 标准语言代码, 如 <c>"eng"</c>, <c>"zhs"</c>, <c>"zht"</c>, <c>"jpn"</c>, <c>"kor"</c></returns>
    private static string DetectLanguage()
    {
        string? locale = null;

        // 1. 优先读取游戏内语言设置
        try { locale = OS.GetLocale(); }
        catch { ModLog.Debug("Godot.OS.GetLocale() 不可用, 回退到系统语言"); }

        // 2. 回退到系统 UI 语言
        if (string.IsNullOrEmpty(locale))
        {
            try { locale = CultureInfo.CurrentUICulture.Name; }
            catch { return "eng"; }
        }

        return ParseLocale(locale);
    }

    /// <summary>
    /// 将 locale 字符串解析为 STS2 标准语言代码
    /// </summary>
    /// <param name="locale">locale 字符串, 如 <c>"zh_CN"</c>, <c>"zh-Hans"</c>, <c>"ja"</c></param>
    /// <returns>STS2 标准语言代码</returns>
    private static string ParseLocale(string locale)
    {
        if (locale.StartsWith("zh_CN", StringComparison.OrdinalIgnoreCase) ||
            locale.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase) ||
            locale.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase) ||
            locale.StartsWith("zh-SG", StringComparison.OrdinalIgnoreCase))
        {
            return "zhs";
        }
        if (locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return "zht";
        }
        if (locale.StartsWith("ja", StringComparison.OrdinalIgnoreCase))
        {
            return "jpn";
        }
        if (locale.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
        {
            return "kor";
        }
        return "eng";
    }

    /// <summary>
    /// 从 JSON 文件加载指定语言的本地化文本
    /// </summary>
    /// <param name="language">STS2 标准语言代码 (如 "eng", "zhs")</param>
    private static void LoadLocalization(string language)
    {
        try
        {
            var modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(modDir))
            {
                ModLog.Warn("无法确定 mod 目录路径, 本地化不可用");
                return;
            }

            var locPath = Path.Combine(modDir, "localization", $"{language}.json");

            if (!File.Exists(locPath))
            {
                if (language != "eng")
                {
                    ModLog.Warn($"本地化文件不存在: {locPath}, 回退到 eng");
                    LoadLocalization("eng");
                    return;
                }
                ModLog.Warn($"本地化文件不存在: {locPath}");
                return;
            }

            var json = File.ReadAllText(locPath);
            _entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
            _language = language;
            ModLog.Info($"本地化已加载: {language} ({_entries.Count} 条)");
        }
        catch (Exception ex)
        {
            ModLog.Error($"加载本地化文件失败: {ex.Message}");
            if (language != "eng")
            {
                ModLog.Warn("回退到 eng");
                LoadLocalization("eng");
            }
        }
    }

    /// <summary>
    /// 获取指定键的本地化文本
    /// </summary>
    /// <param name="key">本地化键</param>
    /// <returns>本地化后的文本; 如果键不存在则返回键名本身</returns>
    private static string Get(string key)
    {
        if (_entries.TryGetValue(key, out var value))
        {
            return value;
        }
        ModLog.Warn($"本地化键缺失: {key}");
        return key;
    }

    /// <summary>
    /// 获取指定键的本地化文本, 并用参数替换 <c>{Key}</c> 占位符
    /// </summary>
    /// <param name="key">本地化键, 对应 JSON 中的模板字符串</param>
    /// <param name="args">占位符键值对数组, 例如 <c>("Count", 5)</c> 会替换模板中的 <c>{Count}</c></param>
    /// <returns>替换占位符后的本地化文本</returns>
    private static string Format(string key, params (string Key, object Value)[] args)
    {
        var template = Get(key);
        foreach (var (k, v) in args)
        {
            template = template.Replace($"{{{k}}}", v?.ToString() ?? "null");
        }
        return template;
    }

    /// <summary>
    /// 设置页面 "通用" Section 的标题
    /// </summary>
    public static string SettingsTitleGeneral => Get("settings.section.general");

    /// <summary>
    /// 设置页面 Modded Mode 选项的标签
    /// </summary>
    public static string SettingsTitleModdedMode => Get("settings.mode.label");

    /// <summary>
    /// 设置页面 "拦截 IsRunningModded()" Toggle 的标签
    /// </summary>
    public static string SettingsTitlePatchModManager => Get("settings.patchModManager.label");

    /// <summary>
    /// 设置页面 "详细日志" Toggle 的标签
    /// </summary>
    public static string SettingsTitleVerboseLogging => Get("settings.verboseLogging.label");

    /// <summary>
    /// 设置页面 "重置为默认设置" Button 的标签
    /// </summary>
    public static string SettingsTitleResetDefaults => Get("settings.resetDefaults.label");

    /// <summary>
    /// 设置页面 "重置为默认设置" Button 上显示的文本
    /// </summary>
    public static string SettingsButtonReset => Get("settings.resetDefaults.button");

    /// <summary>
    /// Modded Mode 选项 "自动" 的显示文本
    /// </summary>
    public static string ModeOptionAuto => Get("settings.mode.option.auto");

    /// <summary>
    /// Modded Mode 选项 "强制原版" 的显示文本
    /// </summary>
    public static string ModeOptionAlwaysVanilla => Get("settings.mode.option.alwaysVanilla");

    /// <summary>
    /// Modded Mode 选项 "游戏默认" 的显示文本
    /// </summary>
    public static string ModeOptionDefault => Get("settings.mode.option.default");

    /// <summary>
    /// Modded Mode 选项的详细描述文本
    /// </summary>
    public static string DescModdedMode => Get("settings.mode.desc");

    /// <summary>
    /// "拦截 IsRunningModded()" Toggle 的详细描述文本
    /// </summary>
    public static string DescPatchModManager => Get("settings.patchModManager.desc");

    /// <summary>
    /// "详细日志" Toggle 的详细描述文本
    /// </summary>
    public static string DescVerboseLogging => Get("settings.verboseLogging.desc");

    /// <summary>
    /// "重置为默认设置" Button 的详细描述文本
    /// </summary>
    public static string DescResetDefaults => Get("settings.resetDefaults.desc");

    /// <summary>
    /// 初始化开始日志消息
    /// </summary>
    /// <param name="id">mod ID</param>
    /// <param name="version">mod 版本号</param>
    public static string LogInitStart(string id, string version) => Format("log.initStart", ("Id", id), ("Version", version));

    /// <summary>
    /// 详细日志已启用的日志消息
    /// </summary>
    public static string LogVerboseEnabled => Get("log.verboseEnabled");

    /// <summary>
    /// 初始化步骤进度日志消息
    /// </summary>
    /// <param name="step">当前步骤编号</param>
    /// <param name="total">总步骤数</param>
    /// <param name="desc">步骤描述</param>
    public static string LogStep(int step, int total, string desc) => Format("log.step", ("Step", step), ("Total", total), ("Desc", desc));

    /// <summary>
    /// Harmony 补丁应用完成日志消息
    /// </summary>
    /// <param name="count">已补丁的方法数量</param>
    public static string LogPatchesApplied(int count) => Format("log.patchesApplied", ("Count", count));

    /// <summary>
    /// PatchModManagerIsRunningModded 已禁用的日志消息
    /// </summary>
    public static string LogPatchModManagerDisabled => Get("log.patchModManagerDisabled");

    /// <summary>
    /// PatchModManagerIsRunningModded 已启用的日志消息
    /// </summary>
    public static string LogPatchModManagerEnabled => Get("log.patchModManagerEnabled");

    /// <summary>
    /// 初始化完成日志消息
    /// </summary>
    /// <param name="mode">当前 ModdedMode 名称</param>
    /// <param name="patchModManager">是否启用了 PatchModManagerIsRunningModded</param>
    public static string LogInitComplete(string mode, bool patchModManager) => Format("log.initComplete", ("Mode", mode), ("PatchModManager", patchModManager));

    /// <summary>
    /// Auto 模式统计日志消息
    /// </summary>
    /// <param name="known">已知 mod 总数</param>
    /// <param name="loaded">已加载 mod 数</param>
    /// <param name="gameplay">gameplay mod 数</param>
    /// <param name="nonGameplay">非 gameplay mod 数</param>
    public static string LogAutoMode(int known, int loaded, int gameplay, int nonGameplay) => Format("log.autoMode", ("Known", known), ("Loaded", loaded), ("Gameplay", gameplay), ("NonGameplay", nonGameplay));

    /// <summary>
    /// 检测到 gameplay mod 的日志消息
    /// </summary>
    /// <param name="modList">gameplay mod ID 列表, 逗号分隔</param>
    public static string LogGameplayModsDetected(string modList) => Format("log.gameplayModsDetected", ("ModList", modList));

    /// <summary>
    /// Default 模式统计日志消息
    /// </summary>
    /// <param name="total">ModManager 中 mod 总数</param>
    /// <param name="loadedOrFailed">已加载或失败的 mod 数</param>
    public static string LogDefaultMode(int total, int loadedOrFailed) => Format("log.defaultMode", ("Total", total), ("LoadedOrFailed", loadedOrFailed));

    /// <summary>
    /// Auto 模式回退到 Default 模式的日志消息
    /// </summary>
    public static string LogAutoFallback => Get("log.autoFallback");

    /// <summary>
    /// 开始注册设置页面的日志消息
    /// </summary>
    public static string LogRegisteringSettings => Get("log.registeringSettings");

    /// <summary>
    /// 设置页面注册完成的日志消息
    /// </summary>
    public static string LogSettingsRegistered => Get("log.settingsRegistered");
}
