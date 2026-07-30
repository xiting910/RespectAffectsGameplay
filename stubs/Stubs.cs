// =============================================================================
// 桩类型定义: 模拟 Slay the Spire 2 游戏程序集中的类型
// 仅用于 CI 编译, 不包含任何实际游戏逻辑
// =============================================================================
#pragma warning disable IDE0130
#pragma warning disable IDE0060
#pragma warning disable CA1051
#pragma warning disable CA1716
#pragma warning disable CA1822
#pragma warning disable CS9113

using System.Reflection;

namespace MegaCrit.Sts2.Core.Modding
{
    /// <summary>
    /// 模组加载状态枚举 (桩)
    /// </summary>
    public enum ModLoadState
    {
        /// <summary>未加载</summary>
        None,
        /// <summary>已加载</summary>
        Loaded,
        /// <summary>加载失败</summary>
        Failed,
    }

    /// <summary>
    /// 模组清单 (桩)
    /// </summary>
    public record ModManifest
    {
        /// <summary>是否影响游戏性</summary>
        public bool affectsGameplay;

        /// <summary>模组唯一标识符 (来自 JSON "id")</summary>
        public string? id;

        /// <summary>模组名称 (来自 JSON "name")</summary>
        public string? name;
    }

    /// <summary>
    /// 模组实例 (桩)
    /// </summary>
    public class Mod
    {
        /// <summary>模组加载状态</summary>
        public ModLoadState state = ModLoadState.None;

        /// <summary>模组清单</summary>
        public ModManifest? manifest;

        /// <summary>模组的程序集列表</summary>
        public List<Assembly> assemblies = [];

        /// <summary>模组的文件系统路径</summary>
        public string? path;
    }

    /// <summary>
    /// 模组管理器 (桩)
    /// </summary>
    public static class ModManager
    {
        /// <summary>已加载的模组列表</summary>
        public static IReadOnlyList<Mod> Mods { get; } = [];

        /// <summary>是否以 modded 模式运行</summary>
        public static bool IsRunningModded()
        {
            return false;
        }

        /// <summary>是否已完成首次存档复制 (桩)</summary>
        public static bool UnmoddedSavesWereCopied { get; private set; }

        /// <summary>首次安装 mod 时复制原版存档到 modded 目录 (桩)</summary>
        public static void CopyUnmoddedSaveFilesIfNeeded() { }
    }

    /// <summary>
    /// 模组初始化器特性: 标记模组的入口方法 (桩)
    /// </summary>
    /// <param name="methodName">初始化方法名称</param>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModInitializerAttribute(string methodName) : Attribute
    {
        /// <summary>初始化方法名称</summary>
        public string MethodName { get; } = methodName;
    }
}

namespace MegaCrit.Sts2.Core.Saves
{
    /// <summary>
    /// 用户数据路径提供器 (桩)
    /// </summary>
    public static class UserDataPathProvider
    {
        /// <summary>
        /// 获取用户数据目录路径 (桩)
        /// </summary>
        /// <param name="forceModState">是否强制使用模组模式路径, 如果为 null 则使用当前运行模式</param>
        /// <returns>用户数据目录路径 (桩实现始终返回空字符串)</returns>
        public static string GetAccountDir(bool? forceModState = null)
        {
            return string.Empty;
        }
    }
}

namespace MegaCrit.Sts2.Core.Logging
{
    /// <summary>
    /// 日志类型枚举 (桩)
    /// </summary>
    public enum LogType
    {
        /// <summary> 通用日志</summary>
        Generic,
        /// <summary> 联机日志</summary>
        Network,
        /// <summary> 动作日志</summary>
        Actions,
        /// <summary> 游戏同步日志</summary>
        GameSync
    }

    /// <summary>
    /// 日志记录器 (桩)
    /// </summary>
    public class Logger(string? context, LogType logType)
    {
        /// <summary>日志上下文</summary>
        public string? Context { get; set; } = context;

        /// <summary>输出 Debug 日志</summary>
        public void Debug(string text, int skipFrames = 1) { }

        /// <summary>输出 Info 日志</summary>
        public void Info(string text, int skipFrames = 1) { }

        /// <summary>输出 Warn 日志</summary>
        public void Warn(string text, int skipFrames = 1) { }

        /// <summary>输出 Error 日志</summary>
        public void Error(string text, int skipFrames = 1) { }
    }
}

namespace MegaCrit.Sts2.Core.Helpers
{
    /// <summary>
    /// 反射辅助类 (桩): 提供程序集类型扫描功能
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// 从指定程序集中获取指定类型的所有子类型 (桩)
        /// </summary>
        /// <param name="assembly">要扫描的程序集</param>
        /// <param name="baseType">基类或接口类型</param>
        /// <returns>子类型枚举 (桩实现始终返回空)</returns>
        public static IEnumerable<Type> GetSubtypesFromAssembly(Assembly assembly, Type baseType)
        {
            return [];
        }
    }
}

namespace MegaCrit.Sts2.Core.Models
{
    /// <summary>
    /// 抽象模型基类 (桩): 游戏中所有数据模型的基类
    /// </summary>
    public abstract class AbstractModel { }
}
