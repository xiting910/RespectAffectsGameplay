using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace RespectAffectsGameplay;

/// <summary>
/// <see cref="Mod"/> 的扩展方法
/// </summary>
public static class ModExtensions
{
    /// <summary>
    /// 判断 Mod 是否已加载
    /// </summary>
    /// <param name="mod">要判断的 Mod</param>
    /// <returns><see langword="true"/> 表示已加载, <see langword="false"/> 表示未加载</returns>
    public static bool IsLoaded(this Mod mod)
    {
        return mod.state is ModLoadState.Loaded or ModLoadState.Failed;
    }

    /// <summary>
    /// 获取 Mod 的唯一标识符 (ID)
    /// </summary>
    /// <param name="mod">要获取 ID 的 Mod</param>
    /// <returns>Mod 的 ID</returns>
    public static string GetId(this Mod mod)
    {
        return mod.manifest?.id ?? mod.manifest?.name ?? mod.path ?? "UnknownMod";
    }

    /// <summary>
    /// 检测指定的 Mod 是否包含 <see cref="AbstractModel"/> 子类
    /// </summary>
    /// <param name="mod">要检测的 Mod</param>
    /// <returns><see langword="true"/> 表示包含, <see langword="false"/> 表示不包含,
    /// <see langword="null"/> 表示无法确定</returns>
    public static bool? ContainsAbstractModel(this Mod mod)
    {
        // 如果 mod 没有程序集, 则无法扫描 AbstractModel 子类
        if (mod.assemblies.Count == 0)
        {
            ModLog.Verbose($"[{mod.GetId()}] 没有程序集, 无法扫描 {typeof(AbstractModel)} 子类");
            return null;
        }

        try
        {
            return mod.assemblies.SelectMany(
                assembly => ReflectionHelper.GetSubtypesFromAssembly(assembly, typeof(AbstractModel))).Any();
        }
        catch (Exception ex)
        {
            ModLog.Warn($"扫描 [{mod.GetId()}] 的 {typeof(AbstractModel)} 子类时发生异常: {ex.Message}");
            return null;
        }
    }
}
