// =============================================================================
// 桩类型定义: 模拟 0Harmony (HarmonyLib) 程序集中的类型
// 仅用于 CI 编译, 不包含任何实际补丁逻辑
// =============================================================================
#pragma warning disable IDE0052
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
    /// 应用指定类型中所有 Harmony 补丁 (桩)
    /// </summary>
    /// <param name="type">包含补丁方法的类型</param>
    public void PatchAll(Type type)
    {
        // 桩实现: 不执行任何实际操作
    }

    /// <summary>
    /// 移除指定方法的补丁 (桩)
    /// </summary>
    /// <param name="original">原始方法</param>
    /// <param name="patch">补丁方法</param>
    public void Unpatch(MethodInfo? original, MethodInfo? patch)
    {
        // 桩实现: 不执行任何实际操作
    }
}

/// <summary>
/// Harmony 补丁特性: 标记需要补丁的目标方法 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HarmonyPatchAttribute : Attribute
{
    /// <summary>
    /// 指定补丁目标类型
    /// </summary>
    /// <param name="declaringType">声明目标方法的类型</param>
    public HarmonyPatchAttribute(Type declaringType) { }

    /// <summary>
    /// 指定补丁目标类型和方法名
    /// </summary>
    /// <param name="declaringType">声明目标方法的类型</param>
    /// <param name="methodName">目标方法名称</param>
    public HarmonyPatchAttribute(Type declaringType, string methodName) { }

    /// <summary>
    /// 指定补丁目标类型、方法名和参数类型
    /// </summary>
    /// <param name="declaringType">声明目标方法的类型</param>
    /// <param name="methodName">目标方法名称</param>
    /// <param name="argumentTypes">目标方法参数类型数组</param>
    public HarmonyPatchAttribute(Type declaringType, string methodName, Type[] argumentTypes) { }
}

/// <summary>
/// Harmony 前缀补丁特性: 在目标方法执行前运行 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyPrefixAttribute : Attribute { }

/// <summary>
/// Harmony 后缀补丁特性: 在目标方法执行后运行 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyPostfixAttribute : Attribute { }

/// <summary>
/// Harmony 最终器补丁特性: 在目标方法执行后始终运行, 类似 finally 块 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyFinalizerAttribute : Attribute { }

/// <summary>
/// Harmony 反射访问工具类 (桩)
/// </summary>
public static class AccessTools
{
    /// <summary>
    /// 通过反射获取类型的静态字段引用 (桩)
    /// </summary>
    /// <typeparam name="T">字段类型</typeparam>
    /// <param name="type">声明字段的类型</param>
    /// <param name="fieldName">字段名称</param>
    /// <returns>字段值的引用</returns>
    public static T StaticFieldRefAccess<T>(Type type, string fieldName) => default!;
}
