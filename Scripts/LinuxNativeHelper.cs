using System.Runtime.InteropServices;

namespace RespectAffectsGameplay;

/// <summary><para>
/// Linux 原生辅助类: 提供在 Linux 平台上确保 libgcc_s.so.1 全局加载的功能
/// </para><para>
/// 必须在 <see cref="HarmonyLib.Harmony.PatchAll(System.Reflection.Assembly)"/> 之前调用 <see cref="EnsureLibGccLoaded"/> 方法, 以确保 libgcc_s 已全局加载
/// </para></summary>
internal static partial class LinuxNativeHelper
{
    /// <summary>
    /// <see cref="dlopen(string, int)"/> 标志: 立即解析所有未定义符号
    /// </summary>
    private const int RTLD_NOW = 0x00002;

    /// <summary>
    /// <see cref="dlopen(string, int)"/> 标志: 将库的符号添加到全局符号表中, 使得其他库可以引用该库的符号
    /// </summary>
    private const int RTLD_GLOBAL = 0x00100;

    /// <summary>
    /// 标记是否已执行过加载操作
    /// </summary>
    private static bool _initialized;

    /// <summary>
    /// 确保 <c>libgcc_s.so.1</c> 已以全局符号可见性加载
    /// </summary>
    internal static void EnsureLibGccLoaded()
    {
        if (_initialized) { return; }

        _initialized = true;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return; }

        _ = dlopen("libgcc_s.so.1", RTLD_NOW | RTLD_GLOBAL);
    }

    /// <summary>
    /// 动态加载共享库并返回句柄
    /// </summary>
    /// <param name="fileName">要加载的共享库文件名</param>
    /// <param name="flags">加载标志, 例如 <see cref="RTLD_NOW"/> 和 <see cref="RTLD_GLOBAL"/></param>
    /// <returns>共享库句柄, 如果加载失败则返回 <see cref="IntPtr.Zero"/></returns>
    [LibraryImport("libdl.so.2", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr dlopen(string fileName, int flags);
}
