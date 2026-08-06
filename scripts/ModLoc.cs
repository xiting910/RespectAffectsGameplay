using Godot;
using STS2RitsuLib;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using System.Reflection;
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
    /// JSON 序列化选项
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// 查找嵌入资源的程序集, 用于导出内置翻译文件
    /// </summary>
    private static readonly Assembly _resourceAssembly = typeof(RespectAffectsGameplayMod).Assembly;

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
        // 获取用户基础路径
        var userDir = ProjectSettings.GlobalizePath(ProfileManager.GetAccountBasePath(ModInfo.Id));

        // 迁移旧版模组数据目录到新目录
        MigrateLegacyIfNeeded(userDir);

        // 获取用户本地化目录的真实路径
        var userLocDir = Path.Combine(userDir, LocalizationFolderName);

        // 确保将内置的默认翻译文件导出到用户本地化目录
        EnsureDefaultTranslationsExtracted(userLocDir);

        // 创建本地化实例, 优先使用用户目录下的翻译文件, 如果缺失则使用内置的默认翻译
        _instance = RitsuLibFramework.CreateModLocalization(
            ModInfo.Id, ModInfo.Id,
            fileSystemFolders: [userLocDir],
            resourceFolders: [LocalizationFolderName],
            resourceAssembly: _resourceAssembly
        );

        // 记录日志
        ModLog.Info("本地化已初始化");
    }

    /// <summary>
    /// 将旧版模组数据目录 (user://{modId}) 迁移到新目录
    /// </summary>
    /// <param name="userDir">新目录</param>
    private static void MigrateLegacyIfNeeded(string userDir)
    {
        // 旧版模组数据目录: user://{modId}
        var legacyLocDir = Path.Combine(OS.GetUserDataDir(), ModInfo.Id);

        // 获取旧目录的 DirectoryInfo 对象
        var legacyDirInfo = new DirectoryInfo(legacyLocDir);

        // 获取路径比较器, Windows 下忽略大小写, 其他平台区分大小写
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        // 旧目录不存在或路径相同, 无需迁移
        if (!legacyDirInfo.Exists || pathComparer.Equals(legacyLocDir, userDir)) { return; }

        // 迁移旧目录到新目录
        Migrate(legacyDirInfo, userDir);
    }

    /// <summary>
    /// 确保将内置的默认翻译文件导出到用户本地化目录
    /// </summary>
    /// <param name="userLocDir">用户本地化目录</param>
    private static void EnsureDefaultTranslationsExtracted(string userLocDir)
    {
        try
        {
            // 确保用户本地化目录存在, 如果不存在则创建
            _ = Directory.CreateDirectory(userLocDir);

            // 遍历程序集中的所有嵌入资源, 匹配 localization.<lang>.json 格式的文件
            foreach (var name in _resourceAssembly.GetManifestResourceNames())
            {
                // 匹配嵌入资源名称是否符合 localization.<lang>.json 的模式
                var match = LocalizationResourceRegex().Match(name);
                if (match.Success)
                {
                    // 从嵌入资源流中读取翻译文件的原始字节
                    using var s = _resourceAssembly.GetManifestResourceStream(name);
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

                    // 获取导出到用户本地化目录的文件名, 例如 localization.zh.json -> zh.json
                    var fn = match.Groups[1].Value + LocalizationFileExtension;

                    // 获取导出到用户本地化目录的完整文件路径
                    var filePath = Path.Combine(userLocDir, fn);

                    // 获取嵌入资源的 JSON 文档
                    using var embeddedDoc = JsonDocument.Parse(embeddedBytes);

                    // 提取内置翻译的版本号
                    var embeddedVersion = ExtractVersion(embeddedDoc.RootElement);

                    // 如果用户本地化目录中不存在该翻译文件, 则直接写入内置翻译
                    if (!File.Exists(filePath))
                    {
                        File.WriteAllBytes(filePath, embeddedBytes);
                        ModLog.Info($"翻译文件已导出到用户本地化目录: {fn} (版本: {embeddedVersion})");
                        continue;
                    }

                    // 读取用户文件的 JSON 文档
                    using var userDoc = JsonDocument.Parse(File.ReadAllText(filePath));

                    // 提取用户文件的版本号
                    var userVersion = ExtractVersion(userDoc.RootElement);

                    // 如果版本不同, 直接使用内置翻译完全覆盖
                    if (userVersion != embeddedVersion)
                    {
                        File.WriteAllBytes(filePath, embeddedBytes);
                        ModLog.Info($"翻译文件更新: {fn} (版本: {userVersion} -> {embeddedVersion})");
                        continue;
                    }

                    // 获取嵌入资源的字典
                    var embeddedDict = GetUserDict(embeddedDoc.RootElement);

                    // 获取用户文件的字典
                    var userDict = GetUserDict(userDoc.RootElement);

                    // 标记是否有缺失的键
                    var missingKeysCount = 0;

                    // 遍历嵌入资源的键值对
                    foreach (var kv in embeddedDict)
                    {
                        // 如果用户文件中缺失该键, 则将其添加到用户字典中, 并标记为有缺失键
                        if (!userDict.ContainsKey(kv.Key))
                        {
                            userDict[kv.Key] = kv.Value;
                            missingKeysCount++;
                        }
                    }

                    // 如果有缺失的键, 则将更新后的用户字典写回到用户文件中
                    if (missingKeysCount > 0)
                    {
                        File.WriteAllText(filePath, JsonSerializer.Serialize(userDict, _jsonOptions));
                        ModLog.Info($"翻译文件已更新: {fn} (新增 {missingKeysCount} 个缺失键)");
                    }
                    else
                    {
                        ModLog.Verbose($"翻译文件已是最新: {fn}");
                    }
                }
            }
            ModLog.Info($"内置翻译已更新到用户本地化目录: {userLocDir}");
        }
        catch (Exception ex)
        {
            ModLog.Error($"无法导出内置翻译到用户本地化目录: {ex.Message}");
        }
    }

    /// <summary>
    /// 将旧目录递归迁移到新目录, 如果新目录已存在则跳过同名文件
    /// </summary>
    /// <param name="oldDir">旧目录</param>
    /// <param name="newDirPath">新目录路径</param>
    private static void Migrate(DirectoryInfo oldDir, string newDirPath)
    {
        try
        {
            if (Directory.Exists(newDirPath))
            {
                // 新目录已存在: 逐文件移动 (同名文件保留新目录的)
                foreach (var file in oldDir.GetFiles("*", SearchOption.TopDirectoryOnly))
                {
                    var destPath = Path.Combine(newDirPath, file.Name);
                    if (File.Exists(destPath))
                    {
                        ModLog.Verbose($"文件 {destPath} 已存在, 跳过迁移: {file.FullName}");
                    }
                    else
                    {
                        file.MoveTo(destPath);
                        ModLog.Verbose($"模组文件已迁移: {file.FullName} -> {destPath}");
                    }
                }

                // 递归迁移子目录
                foreach (var subDir in oldDir.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    Migrate(subDir, Path.Combine(newDirPath, subDir.Name));
                }

                // 删除旧目录 (不用递归删除, 因为子目录已被迁移)
                oldDir.Delete();
            }
            else
            {
                // 新目录不存在: 整目录移动
                oldDir.MoveTo(newDirPath);
            }

            // 记录日志
            ModLog.Info($"迁移 {oldDir.FullName} 到 {newDirPath} 成功");
        }
        catch (Exception ex)
        {
            ModLog.Warn($"迁移 {oldDir.FullName} 到 {newDirPath} 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从指定的 <see cref="JsonElement"/> 中提取版本号
    /// </summary>
    /// <param name="element">要提取版本号的 JSON 元素</param>
    /// <returns>版本号字符串, 如果不存在则返回 <see langword="null"/></returns>
    private static string? ExtractVersion(JsonElement element)
    {
        return element.TryGetProperty(VersionKey, out var versionProp) ? versionProp.GetString() : null;
    }

    /// <summary>
    /// 从指定的 <see cref="JsonElement"/> 中提取键值对字典
    /// </summary>
    /// <param name="root">要提取键值对的 JSON 根元素</param>
    /// <returns>键值对字典</returns>
    private static Dictionary<string, JsonElement> GetUserDict(JsonElement root)
    {
        return root.EnumerateObject().ToDictionary(static prop => prop.Name, static prop => prop.Value);
    }
}
