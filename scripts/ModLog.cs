using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib;

namespace RespectAffectsGameplay;

/// <summary>
/// 全局日志记录器, 供整个 mod 共享使用
/// </summary>
internal static class ModLog
{
    /// <summary>
    /// 日志记录器实例
    /// </summary>
    private static readonly Logger _instance = RitsuLibFramework.CreateLogger(ModInfo.Id);

    /// <summary>
    /// 检查是否启用了详细日志
    /// </summary>
    private static bool IsVerboseEnabled => ModSettingsHelper.GetSettings().VerboseLogging;

    /// <summary>
    /// 输出详细日志 (仅在详细日志启用时输出, 实际以 Info 级别输出到游戏日志以便可见)
    /// </summary>
    /// <param name="text">日志内容</param>
    public static void Verbose(string text)
    {
        if (IsVerboseEnabled) { _instance.Info(text); }
    }

    /// <summary>
    /// 输出 Info 级别日志 (始终输出)
    /// </summary>
    /// <param name="text">日志内容</param>
    public static void Info(string text)
    {
        _instance.Info(text);
    }

    /// <summary>
    /// 输出 Warn 级别日志 (始终输出)
    /// </summary>
    /// <param name="text">日志内容</param>
    public static void Warn(string text)
    {
        _instance.Warn(text);
    }

    /// <summary>
    /// 输出 Error 级别日志 (始终输出)
    /// </summary>
    /// <param name="text">日志内容</param>
    public static void Error(string text)
    {
        _instance.Error(text);
    }
}
