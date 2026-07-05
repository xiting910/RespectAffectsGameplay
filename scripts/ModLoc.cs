using STS2RitsuLib;
using STS2RitsuLib.Utils;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RespectAffectsGameplay;

/// <summary>
/// 工具类: 用于管理本地化资源
/// </summary>
public static partial class ModLoc
{
    /// <summary>
    /// 本地化版本键, 用于检测内置翻译是否有更新
    /// </summary>
    public const string VersionKey = "_version";

    /// <summary>
    /// 本地化资源所在的文件夹名称
    /// </summary>
    public const string LocalizationFolderName = "localization";

    /// <summary>
    /// 本地化资源文件的扩展名
    /// </summary>
    public const string LocalizationFileExtension = ".json";

    /// <summary>
    /// 匹配嵌入资源中本地化文件的模式: localization.&lt;lang&gt;.json
    /// </summary>
    [GeneratedRegex("^" + LocalizationFolderName + "\\.([^.]+)\\" + LocalizationFileExtension + "$")]
    private static partial Regex LocalizationResourceRegex();

    /// <summary>
    /// JSON 序列化选项 (缓存复用)
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// 本地化实例, 在 <see cref="Initialize"/> 后可用
    /// </summary>
    private static I18N? _instance;

    /// <summary>
    /// 获取本地化实例, 在 <see cref="Initialize"/> 前访问将抛出异常
    /// </summary>
    public static I18N Instance => _instance ?? throw new InvalidOperationException("本地化尚未初始化！");

    /// <summary>
    /// 初始化本地化系统
    /// </summary>
    public static void Initialize()
    {
        // 获取用户目录下的本地化资源文件夹路径
        var userLocDir = Path.Combine(Godot.OS.GetUserDataDir(), ModInfo.Id, LocalizationFolderName);

        // 确保将内置的默认翻译文件导出到用户目录
        EnsureDefaultTranslationsExtracted(userLocDir);

        // 创建本地化实例, 优先使用用户目录下的翻译文件, 如果缺失则使用内置的默认翻译
        _instance = RitsuLibFramework.CreateModLocalization(
            ModInfo.Id, ModInfo.Id,
            fileSystemFolders: [userLocDir],
            resourceFolders: [LocalizationFolderName],
            resourceAssembly: typeof(RespectAffectsGameplayMod).Assembly);

        // 记录日志
        ModLog.Info("本地化已初始化");
    }

    /// <summary>
    /// 确保将内置的默认翻译文件导出到用户目录
    /// </summary>
    /// <param name="userLocDir">用户目录下的本地化资源文件路径</param>
    private static void EnsureDefaultTranslationsExtracted(string userLocDir)
    {
        try
        {
            // 创建用户目录下的本地化资源文件夹（如果不存在）
            _ = Directory.CreateDirectory(userLocDir);

            // 获取内置翻译资源的程序集
            var asm = typeof(RespectAffectsGameplayMod).Assembly;

            // 遍历程序集中的所有嵌入资源, 匹配 localization.<lang>.json 格式的文件
            foreach (var name in asm.GetManifestResourceNames())
            {
                // 匹配嵌入资源名称是否符合 localization.<lang>.json 的模式
                var match = LocalizationResourceRegex().Match(name);
                if (match.Success)
                {
                    // 从嵌入资源流中读取翻译文件的原始字节
                    using var s = asm.GetManifestResourceStream(name);
                    if (s is null)
                    {
                        ModLog.Warn($"无法获取内置翻译资源流: {name}");
                        continue;
                    }

                    // 将嵌入资源流复制到内存流中, 以便后续处理
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);

                    // 获取嵌入资源的字节数组
                    var embeddedBytes = ms.ToArray();

                    // 获取导出到用户目录的文件名, 例如 localization.zh.json -> zh.json
                    var fn = match.Groups[1].Value + LocalizationFileExtension;

                    // 获取导出到用户目录的完整文件路径
                    var filePath = Path.Combine(userLocDir, fn);

                    // 获取嵌入资源的 JSON 文档
                    using var embeddedDoc = JsonDocument.Parse(embeddedBytes);

                    // 提取内置翻译的版本号
                    var embeddedVersion = ExtractVersion(embeddedDoc);

                    // 获取用户文件的版本号
                    var userVersion = (string?)null;

                    // 获取用户文件的字典
                    var userDict = new Dictionary<string, JsonElement>();

                    // 如果用户文件存在, 读取用户文件的版本号和键值对
                    if (File.Exists(filePath))
                    {
                        // 读取用户文件的 JSON 文档
                        using var userDoc = JsonDocument.Parse(File.ReadAllText(filePath));

                        // 提取用户文件的版本号
                        userVersion = ExtractVersion(userDoc);

                        // 遍历用户文件的根元素, 将键值对存入字典
                        foreach (var prop in userDoc.RootElement.EnumerateObject())
                        {
                            userDict[prop.Name] = prop.Value;
                        }
                    }

                    // 如果版本不同, 直接使用内置翻译完全覆盖
                    if (userVersion != embeddedVersion)
                    {
                        File.WriteAllBytes(filePath, embeddedBytes);
                        ModLog.Verbose($"翻译文件已更新: {fn} (版本: {userVersion} -> {embeddedVersion})");
                        continue;
                    }

                    // 获取嵌入资源的字典
                    var embeddedDict = new Dictionary<string, JsonElement>();

                    // 遍历嵌入资源的根元素, 将键值对存入字典
                    foreach (var prop in embeddedDoc.RootElement.EnumerateObject())
                    {
                        embeddedDict[prop.Name] = prop.Value;
                    }

                    // 标记是否有缺失的键
                    var hasMissingKeys = false;

                    // 遍历嵌入资源的键值对
                    foreach (var kv in embeddedDict)
                    {
                        // 如果用户文件中缺失该键, 则将其添加到用户字典中, 并标记为有缺失键
                        if (!userDict.ContainsKey(kv.Key))
                        {
                            userDict[kv.Key] = kv.Value;
                            hasMissingKeys = true;
                        }
                    }

                    // 如果有缺失的键, 则将更新后的用户字典写回到用户文件中
                    if (hasMissingKeys)
                    {
                        File.WriteAllText(filePath, JsonSerializer.Serialize(userDict, _jsonOptions));
                        ModLog.Verbose($"翻译文件已更新: {fn}");
                    }
                    else
                    {
                        ModLog.Verbose($"翻译文件已是最新: {fn}");
                    }
                }
            }
            ModLog.Info($"内置翻译已更新到用户目录: {userLocDir}");
        }
        catch (Exception ex)
        {
            ModLog.Warn($"无法导出内置翻译到用户目录: {ex.Message}");
        }
    }

    /// <summary>
    /// 从指定的 <see cref="JsonDocument"/> 中提取版本号
    /// </summary>
    /// <param name="doc">要提取版本号的 JSON 文档</param>
    /// <returns>版本号字符串, 如果不存在则返回 <see langword="null"/></returns>
    private static string? ExtractVersion(JsonDocument doc)
    {
        return doc.RootElement.TryGetProperty(VersionKey, out var versionProp) ? versionProp.GetString() : null;
    }
}
