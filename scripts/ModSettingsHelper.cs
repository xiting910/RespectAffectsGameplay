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
                defaultFactory: () => new(),
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
            ModLog.Error("设置缓存未初始化, 无法持久化重置操作。设置将在重启后恢复。");
            return;
        }

        _settingsCache.Modify(settings =>
        {
            settings.Mode = ModdedMode.Auto;
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
}
