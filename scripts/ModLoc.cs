using STS2RitsuLib;
using STS2RitsuLib.Utils;
using System.Text.RegularExpressions;

namespace RespectAffectsGameplay;

/// <summary>
/// 工具类: 用于管理本地化资源
/// </summary>
public static partial class ModLoc
{
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
                    // 从嵌入资源流中读取翻译文件
                    using var s = asm.GetManifestResourceStream(name);
                    if (s is null)
                    {
                        ModLog.Warn($"无法获取内置翻译资源流: {name}");
                        continue;
                    }

                    // 获取导出到用户目录的文件名, 例如 localization.zh.json -> zh.json
                    var fn = match.Groups[1].Value + LocalizationFileExtension;

                    // 创建用户目录下的翻译文件, 如果已存在则覆盖
                    using var fs = File.Create(Path.Combine(userLocDir, fn));

                    // 将内置翻译资源流复制到用户目录下的文件
                    s.CopyTo(fs);
                    ModLog.Debug($"已导出内置翻译: {fn}");
                }
            }
            ModLog.Info($"内置翻译已导出到用户目录: {userLocDir}");
        }
        catch (Exception ex)
        {
            ModLog.Warn($"无法导出内置翻译到用户目录: {ex.Message}");
        }
    }
}
