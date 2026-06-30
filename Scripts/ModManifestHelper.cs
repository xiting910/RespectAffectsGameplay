using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RespectAffectsGameplay;

/// <summary>
/// 使用反射安全访问 Mod / ModManifest 成员的辅助类
/// </summary>
internal static class ModManifestHelper
{
    /// <summary>
    /// <see cref="Mod.manifest"/> 字段的 FieldInfo (优先), 可能为 null
    /// </summary>
    private static readonly FieldInfo? ManifestField = AccessTools.Field(typeof(Mod), "manifest");

    /// <summary>
    /// <see cref="ModManifest.affectsGameplay"/> 字段的 FieldInfo (优先), 可能为 null
    /// </summary>
    private static readonly FieldInfo? AffectsGameplayField = AccessTools.Field(typeof(ModManifest), "affectsGameplay");

    /// <summary>
    /// <see cref="ModManifest.id"/> 字段的 FieldInfo (优先), 可能为 null
    /// </summary>
    private static readonly FieldInfo? IdField = AccessTools.Field(typeof(ModManifest), "id");

    /// <summary>
    /// <see cref="ModManifest.name"/> 字段的 FieldInfo (优先), 可能为 null
    /// </summary>
    private static readonly FieldInfo? NameField = AccessTools.Field(typeof(ModManifest), "name");

    /// <summary>
    /// 安全获取 Mod 的 manifest, 优先尝试字段访问, 失败时回退到属性
    /// </summary>
    /// <param name="mod">Mod 实例</param>
    /// <returns>ModManifest 实例; 如果无法获取则返回 null</returns>
    public static ModManifest? GetManifest(Mod mod)
    {
        try
        {
            return ManifestField?.GetValue(mod) as ModManifest ?? AccessTools.Property(typeof(Mod), "manifest")?.GetValue(mod) as ModManifest;
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 Mod.manifest 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 安全获取 <see cref="ModManifest.affectsGameplay"/> 的值, 优先尝试字段访问, 失败时回退到属性
    /// </summary>
    /// <param name="manifest">ModManifest 实例</param>
    /// <returns>affectsGameplay 的值; 无法获取时返回 true</returns>
    public static bool GetAffectsGameplay(ModManifest manifest)
    {
        try
        {
            if (AffectsGameplayField?.GetValue(manifest) is bool value) { return value; }
            if (AccessTools.Property(typeof(ModManifest), "affectsGameplay")?.GetValue(manifest) is bool v2) { return v2; }
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 ModManifest.affectsGameplay 失败: {ex.Message}");
        }
        return true;
    }

    /// <summary>
    /// 安全获取 <see cref="ModManifest.id"/> 的值, 优先尝试字段访问, 失败时回退到属性
    /// </summary>
    /// <param name="manifest">ModManifest 实例</param>
    /// <returns>id 字符串; 无法获取时返回 null</returns>
    public static string? GetId(ModManifest manifest)
    {
        try
        {
            return IdField?.GetValue(manifest) as string ?? AccessTools.Property(typeof(ModManifest), "id")?.GetValue(manifest) as string;
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 ModManifest.id 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 安全获取 <see cref="ModManifest.name"/> 的值, 优先尝试字段访问, 失败时回退到属性
    /// </summary>
    /// <param name="manifest">ModManifest 实例</param>
    /// <returns>name 字符串; 无法获取时返回 null</returns>
    public static string? GetName(ModManifest manifest)
    {
        try
        {
            return NameField?.GetValue(manifest) as string ?? AccessTools.Property(typeof(ModManifest), "name")?.GetValue(manifest) as string;
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 ModManifest.name 失败: {ex.Message}");
            return null;
        }
    }
}
