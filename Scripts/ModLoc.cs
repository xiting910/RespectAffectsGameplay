using Godot;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

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
    private static string ParseLocale(string locale) =>
        locale.StartsWith("zh_CN", StringComparison.OrdinalIgnoreCase) ||
        locale.StartsWith("zh-CN", StringComparison.OrdinalIgnoreCase) ||
        locale.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase) ||
        locale.StartsWith("zh-SG", StringComparison.OrdinalIgnoreCase) ? "zhs" :
        locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zht" :
        locale.StartsWith("ja", StringComparison.OrdinalIgnoreCase) ? "jpn" :
        locale.StartsWith("ko", StringComparison.OrdinalIgnoreCase) ? "kor" : "eng";

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
    /// Toast 通知标题: 检测到 Mislabeled Mod
    /// </summary>
    public static string ToastMislabeledTitle(int count) => Get("toast.mislabeled.title").Replace("{Count}", count.ToString());

    /// <summary>
    /// Toast 通知正文: 列出误标的 Mod
    /// </summary>
    public static string ToastMislabeledBody(string modList) => Get("toast.mislabeled.body").Replace("{ModList}", modList);
}
