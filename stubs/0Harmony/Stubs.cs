// =============================================================================
// 桩类型定义: 模拟 0Harmony (HarmonyLib) 程序集中的类型
// 仅用于 CI 编译, 不包含任何实际补丁逻辑
// =============================================================================
#pragma warning disable IDE0052
#pragma warning disable IDE0060
#pragma warning disable IDE0290
#pragma warning disable CA1710
#pragma warning disable CA1822

using System.Reflection;

namespace HarmonyLib;

/// <summary>
/// Harmony 实例类 (桩): 用于创建和管理补丁
/// </summary>
/// <param name="id">唯一标识符</param>
public class Harmony(string id)
{
    private readonly string _id = id;

    /// <summary>
    /// 应用指定程序集中所有 Harmony 补丁 (桩)
    /// </summary>
    /// <param name="assembly">包含补丁类型的程序集</param>
    public void PatchAll(Assembly assembly)
    {
        // 桩实现: 不执行任何实际操作
    }

    /// <summary>
    /// 获取此 Harmony 实例已补丁的所有方法 (桩)
    /// </summary>
    public IEnumerable<MethodBase> GetPatchedMethods()
    {
        return [];
    }

    /// <summary>
    /// 应用指定类型中所有 Harmony 补丁 (桩)
    /// </summary>
    /// <param name="type">包含补丁方法的类型</param>
    public void PatchAll(Type type)
    {
        // 桩实现: 不执行任何实际操作
    }

    /// <summary>
    /// 移除指定方法的指定补丁 (桩)
    /// </summary>
    /// <param name="original">原始方法</param>
    /// <param name="patch">补丁方法</param>
    public void Unpatch(MethodBase? original, MethodInfo? patch)
    {
        // 桩实现: 不执行任何实际操作
    }
}

/// <summary>
/// Harmony 补丁特性: 标记需要补丁的目标方法 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HarmonyPatch : Attribute
{
    /// <summary>
    /// 指定补丁目标类型和方法名
    /// </summary>
    /// <param name="declaringType">声明目标方法的类型</param>
    /// <param name="methodName">目标方法名称</param>
    public HarmonyPatch(Type declaringType, string methodName) { }
}

/// <summary>
/// Harmony 前缀补丁特性: 在目标方法执行前运行 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyPrefix : Attribute { }
