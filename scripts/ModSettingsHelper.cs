using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace RespectAffectsGameplay;

/// <summary>
/// 辅助类: 提供对 mod 持久化设置的访问, 首次调用 <see cref="GetSettings"/> 时自动初始化
/// </summary>
public static class ModSettingsHelper
{
    /// <summary>
    /// 设置数据的存储键
    /// </summary>
    public const string DataKey = "settings";

    /// <summary>
    /// 保存设置的文件名
    /// </summary>
    public const string DataFileName = "settings.json";

    /// <summary>
    /// 设置数据的保存作用域
    /// </summary>
    public const SaveScope DataScope = SaveScope.Global;

    /// <summary>
    /// 标记是否已完成初始化
    /// </summary>
    private static bool _initialized;

    /// <summary>
    /// 设置数据的缓存
    /// </summary>
    private static ModDataStoreCache<ModSettingsData>? _settingsCache;

    /// <summary>
    /// 确保已完成初始化
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_initialized) { return; }
        _initialized = true;

        ModDataStore store;
        using (RitsuLibFramework.BeginModDataRegistration(ModInfo.Id))
        {
            store = RitsuLibFramework.GetDataStore(ModInfo.Id);
            store.Register<ModSettingsData>(
                key: DataKey,
                fileName: DataFileName,
                scope: DataScope,
                defaultFactory: static () => new(),
                autoCreateIfMissing: true
            );
        }
        _settingsCache = store.CreateCache<ModSettingsData>(DataKey);

        ModLog.Verbose($"持久化数据已注册 (Key={DataKey}, File={DataFileName}, Scope={DataScope})");
    }

    /// <summary>
    /// 获取当前设置, 首次调用时自动初始化持久化数据存储
    /// </summary>
    /// <returns>当前的 <see cref="ModSettingsData"/> 实例</returns>
    public static ModSettingsData GetSettings()
    {
        EnsureInitialized();

        try
        {
            return _settingsCache?.Value ?? throw new InvalidOperationException("设置缓存未初始化");
        }
        catch (Exception ex)
        {
            ModLog.Error($"读取设置失败, 返回默认设置: {ex}");
            return new();
        }
    }

    /// <summary>
    /// 重置所有设置为默认值并持久化到磁盘
    /// </summary>
    public static void ResetToDefaults()
    {
        if (_settingsCache is null)
        {
            ModLog.Warn("设置缓存未初始化, 无法持久化重置操作。设置将在重启后恢复。");
            return;
        }

        _settingsCache.Modify(static settings =>
        {
            settings.Mode = ModdedMode.Auto;
            settings.WhitelistedModIds.Clear();
            settings.PatchModManagerIsRunningModded = false;
            settings.VerboseLogging = false;
        });
        ModLog.Info("设置已重置为默认值并保存");
    }

    /// <summary>
    /// 将待处理的更改持久化到磁盘
    /// </summary>
    public static void SaveSettings()
    {
        if (_settingsCache is null)
        {
            ModLog.Warn($"{nameof(SaveSettings)} 被调用但设置缓存未初始化, 设置未持久化");
            return;
        }
        _settingsCache.Save();
        ModLog.Verbose("设置已保存到本地存储");
    }

    /// <summary>
    /// 判断指定 Mod ID 是否在白名单中
    /// </summary>
    /// <param name="modId">要判断的 Mod ID</param>
    /// <returns><see langword="true"/> 表示在白名单中, <see langword="false"/> 否则</returns>
    public static bool IsWhitelisted(string modId)
    {
        return GetSettings().WhitelistedModIds.Contains(modId);
    }

    /// <summary>
    /// 将指定 Mod ID 加入白名单并持久化
    /// </summary>
    /// <param name="modId">要加入的 Mod ID</param>
    /// <returns><see langword="true"/> 表示新增成功, <see langword="false"/> 表示已存在或输入无效</returns>
    public static bool AddToWhitelist(string modId)
    {
        if (modId.Length == 0) { return false; }

        if (!GetSettings().WhitelistedModIds.Add(modId)) { return false; }

        SaveSettings();
        ModLog.Info($"已将 {modId} 加入白名单");
        return true;
    }

    /// <summary>
    /// 将指定 Mod ID 从白名单移除并持久化
    /// </summary>
    /// <param name="modId">要移除的 Mod ID</param>
    /// <returns><see langword="true"/> 表示移除成功, <see langword="false"/> 表示不在白名单中</returns>
    public static bool RemoveFromWhitelist(string modId)
    {
        var settings = GetSettings();
        if (!settings.WhitelistedModIds.Remove(modId)) { return false; }

        SaveSettings();
        ModLog.Info($"已将 {modId} 从白名单移除");
        return true;
    }

    /// <summary>
    /// 清空白名单并持久化
    /// </summary>
    /// <returns><see langword="true"/> 表示清空成功, <see langword="false"/> 表示白名单本来就是空的</returns>
    public static bool ClearWhitelist()
    {
        var settings = GetSettings();
        if (settings.WhitelistedModIds.Count == 0) { return false; }

        settings.WhitelistedModIds.Clear();
        SaveSettings();
        ModLog.Info("已清空白名单");
        return true;
    }
}
