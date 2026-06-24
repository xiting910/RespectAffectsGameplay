// =============================================================================
// 桩类型定义: 模拟 Slay the Spire 2 游戏程序集中的类型
// 仅用于 CI 编译, 不包含任何实际游戏逻辑
// =============================================================================
#pragma warning disable IDE0130
#pragma warning disable IDE1006
#pragma warning disable IDE0060
#pragma warning disable CA1822

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
    /// 模组信息类 (桩)
    /// </summary>
    public class ModInfo
    {
        /// <summary>模组加载状态</summary>
        public ModLoadState state { get; set; } = ModLoadState.None;
    }

    /// <summary>
    /// 模组管理器 (桩)
    /// </summary>
    public static class ModManager
    {
        /// <summary>已加载的模组列表</summary>
        public static IReadOnlyList<ModInfo> Mods { get; } = [];
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
    public class UserDataPathProvider
    {
        /// <summary>是否以模组模式运行</summary>
        public bool IsRunningModded { get; set; }

        /// <summary>
        /// 获取存档目录路径
        /// </summary>
        /// <param name="profileId">存档槽位编号</param>
        /// <returns>存档目录路径</returns>
        public string GetProfileDir(int profileId) => string.Empty;
    }
}
