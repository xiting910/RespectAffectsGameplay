using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace RespectAffectsGameplay;

/// <summary>
/// 使用反射安全访问 Mod manifest 相关属性的辅助类
/// </summary>
internal static class ModManifestHelper
{
    /// <summary>
    /// <see cref="Mod.manifest"/> 属性的 PropertyInfo (可能为 null)
    /// </summary>
    private static readonly PropertyInfo? ManifestProperty = AccessTools.Property(typeof(Mod), "manifest");

    /// <summary>
    /// <see cref="ModManifest.affectsGameplay"/> 属性的 PropertyInfo (可能为 null)
    /// </summary>
    private static readonly PropertyInfo? AffectsGameplayProperty = AccessTools.Property(typeof(ModManifest), "affectsGameplay");

    /// <summary>
    /// <see cref="ModManifest.id"/> 属性的 PropertyInfo (可能为 null)
    /// </summary>
    private static readonly PropertyInfo? IdProperty = AccessTools.Property(typeof(ModManifest), "id");

    /// <summary>
    /// <see cref="ModManifest.name"/> 属性的 PropertyInfo (可能为 null)
    /// </summary>
    private static readonly PropertyInfo? NameProperty = AccessTools.Property(typeof(ModManifest), "name");

    /// <summary>
    /// 安全获取 Mod 的 manifest
    /// </summary>
    /// <param name="mod">Mod 实例</param>
    /// <returns>ModManifest 实例, 如果无法获取则返回 null</returns>
    public static ModManifest? GetManifest(Mod mod)
    {
        try
        {
            return ManifestProperty?.GetValue(mod) as ModManifest;
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 Mod.manifest 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 安全获取 <see cref="ModManifest.affectsGameplay"/> 的值
    /// </summary>
    /// <param name="manifest">ModManifest 实例</param>
    /// <returns>affectsGameplay 的值; 无法获取时返回 true</returns>
    public static bool GetAffectsGameplay(ModManifest manifest)
    {
        try
        {
            if (AffectsGameplayProperty?.GetValue(manifest) is bool value) { return value; }
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 ModManifest.affectsGameplay 失败: {ex.Message}");
        }
        return true;
    }

    /// <summary>
    /// 安全获取 <see cref="ModManifest.id"/> 的值
    /// </summary>
    /// <param name="manifest">ModManifest 实例</param>
    /// <returns>id 字符串; 无法获取时返回 null</returns>
    public static string? GetId(ModManifest manifest)
    {
        try
        {
            return IdProperty?.GetValue(manifest) as string;
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 ModManifest.id 失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 安全获取 <see cref="ModManifest.name"/> 的值
    /// </summary>
    /// <param name="manifest">ModManifest 实例</param>
    /// <returns>name 字符串; 无法获取时返回 null</returns>
    public static string? GetName(ModManifest manifest)
    {
        try
        {
            return NameProperty?.GetValue(manifest) as string;
        }
        catch (Exception ex)
        {
            ModLog.Warn($"反射获取 ModManifest.name 失败: {ex.Message}");
            return null;
        }
    }
}
