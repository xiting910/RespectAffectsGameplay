// =============================================================================
// 桩类型定义: 模拟 0Harmony (HarmonyLib) 程序集中的类型
// 仅用于 CI 编译, 不包含任何实际补丁逻辑
// =============================================================================
#pragma warning disable IDE0052
#pragma warning disable IDE0060
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

    /// <summary>
    /// 移除指定方法的指定类型补丁 (桩)
    /// </summary>
    /// <param name="original">原始方法</param>
    /// <param name="patchType">要移除的补丁类型</param>
    /// <param name="harmonyId">Harmony 实例 ID (可选)</param>
    public void Unpatch(MethodBase? original, HarmonyPatchType patchType, string? harmonyId = null)
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
    /// 指定补丁目标类型
    /// </summary>
    /// <param name="declaringType">声明目标方法的类型</param>
    public HarmonyPatch(Type declaringType) { }

    /// <summary>
    /// 指定补丁目标类型和方法名
    /// </summary>
    /// <param name="declaringType">声明目标方法的类型</param>
    /// <param name="methodName">目标方法名称</param>
    public HarmonyPatch(Type declaringType, string methodName) { }

    /// <summary>
    /// 指定补丁目标类型、方法名和参数类型
    /// </summary>
    /// <param name="declaringType">声明目标方法的类型</param>
    /// <param name="methodName">目标方法名称</param>
    /// <param name="argumentTypes">目标方法参数类型数组</param>
    public HarmonyPatch(Type declaringType, string methodName, Type[] argumentTypes) { }
}

/// <summary>
/// Harmony 前缀补丁特性: 在目标方法执行前运行 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyPrefix : Attribute { }

/// <summary>
/// Harmony 后缀补丁特性: 在目标方法执行后运行 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyPostfix : Attribute { }

/// <summary>
/// Harmony 最终器补丁特性: 在目标方法执行后始终运行, 类似 finally 块 (桩)
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class HarmonyFinalizer : Attribute { }

/// <summary>
/// Harmony 补丁类型枚举: 指定要操作的补丁类型 (桩)
/// </summary>
public enum HarmonyPatchType
{
    /// <summary>所有补丁类型 (前缀、后缀、最终器等)</summary>
    All,
    /// <summary>前缀补丁</summary>
    Prefix,
    /// <summary>后缀补丁</summary>
    Postfix,
    /// <summary>最终器补丁</summary>
    Finalizer,
    /// <summary>反编译器补丁</summary>
    ReversePatch,
}

/// <summary>
/// Harmony 反射访问工具类 (桩)
/// </summary>
public static class AccessTools
{
    /// <summary>
    /// 通过反射获取类型的字段信息 (桩)
    /// </summary>
    /// <param name="type">声明字段的类型</param>
    /// <param name="name">字段名称</param>
    /// <returns>字段信息; 如果不存在则返回 null</returns>
    public static FieldInfo? Field(Type type, string name)
    {
        return type.GetField(name);
    }

    /// <summary>
    /// 通过反射获取类型的属性信息 (桩)
    /// </summary>
    /// <param name="type">声明属性的类型</param>
    /// <param name="name">属性名称</param>
    /// <returns>属性信息; 如果不存在则返回 null</returns>
    public static PropertyInfo? Property(Type type, string name)
    {
        return type.GetProperty(name);
    }

    /// <summary>
    /// 通过反射获取类型的静态字段引用 (桩)
    /// </summary>
    /// <typeparam name="T">字段类型</typeparam>
    /// <param name="type">声明字段的类型</param>
    /// <param name="fieldName">字段名称</param>
    /// <returns>字段值的引用</returns>
    public static T StaticFieldRefAccess<T>(Type type, string fieldName)
    {
        return default!;
    }

    /// <summary>
    /// 通过类型名称获取 Type 对象 (桩)
    /// </summary>
    /// <param name="name">类型全名 (包含命名空间)</param>
    /// <returns>Type 对象; 如果不存在则返回 null</returns>
    public static Type? TypeByName(string name)
    {
        return Type.GetType(name);
    }

    /// <summary>
    /// 通过反射获取方法信息 (桩)
    /// </summary>
    /// <param name="type">声明方法的类型</param>
    /// <param name="name">方法名称</param>
    /// <param name="parameters">方法参数类型 (可选)</param>
    /// <returns>方法信息; 如果不存在则返回 null</returns>
    public static MethodInfo? Method(Type type, string name, Type[]? parameters = null, Type[]? generics = null)
    {
        return type.GetMethod(name, parameters ?? Type.EmptyTypes);
    }

    /// <summary>
    /// 通过反射获取属性 getter 方法信息 (桩)
    /// </summary>
    /// <param name="type">声明属性的类型</param>
    /// <param name="name">属性名称</param>
    /// <returns>属性 getter 方法信息; 如果不存在则返回 null</returns>
    public static MethodInfo? PropertyGetter(Type type, string name)
    {
        return type.GetProperty(name)?.GetGetMethod();
    }
}
