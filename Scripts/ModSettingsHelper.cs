using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace RespectAffectsGameplay;

/// <summary><para>
/// 辅助类: 初始化并提供对 mod 持久化设置的访问
/// </para><para>
/// 必须在 <see cref="RespectAffectsGameplayMod.Initialize"/> 中调用一次 <see cref="Initialize"/> 方法
/// </para></summary>
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
    /// mod 数据存储实例
    /// </summary>
    private static ModDataStore? _store;

    /// <summary>
    /// 初始化 mod 设置
    /// </summary>
    public static void Initialize()
    {
        ModLog.Debug($"注册持久化数据 (Key={DataKey}, File={DataFileName}, Scope={DataScope})...");

        using (RitsuLibFramework.BeginModDataRegistration(ModInfo.Id))
        {
            _store = RitsuLibFramework.GetDataStore(ModInfo.Id);
            _store.Register<ModSettingsData>(
                key: DataKey,
                fileName: DataFileName,
                scope: DataScope,
                defaultFactory: () => new(),
                autoCreateIfMissing: true
            );
        }

        var settings = GetSettings();
        ModLog.Info($"设置已加载 (Mode={settings.Mode}, PatchModManager={settings.PatchModManagerIsRunningModded})");
    }

    /// <summary>
    /// 获取当前设置
    /// </summary>
    /// <returns>当前的 <see cref="ModSettingsData"/> 实例</returns>
    public static ModSettingsData GetSettings()
    {
        // 如果数据存储未初始化, 返回默认设置
        if (_store is null)
        {
            ModLog.Warn("数据存储未初始化, 返回默认设置");
            return new();
        }

        try
        {
            // 尝试从存储中获取设置数据
            return _store.Get<ModSettingsData>(DataKey);
        }
        catch (Exception ex)
        {
            // 如果获取设置数据失败, 返回默认设置
            ModLog.Error($"读取设置失败, 返回默认设置: {ex}");
            return new();
        }
    }

    /// <summary>
    /// 重置所有设置为默认值并持久化到磁盘
    /// </summary>
    public static void ResetToDefaults()
    {
        if (_store is null)
        {
            ModLog.Error("数据存储未初始化, 无法持久化重置操作。设置将在重启后恢复。");
            return;
        }

        var settings = GetSettings();
        settings.Mode = ModdedMode.Auto;
        settings.PatchModManagerIsRunningModded = false;
        settings.VerboseLogging = false;
        SaveSettings();
        ModLog.Info("设置已重置为默认值并保存");
    }

    /// <summary>
    /// 将待处理的更改持久化到磁盘
    /// </summary>
    public static void SaveSettings()
    {
        if (_store is null)
        {
            ModLog.Warn("SaveSettings() 被调用但 _store 为 null, 设置未持久化");
            return;
        }
        _store.Save(DataKey);
        ModLog.Debug("设置已写入磁盘");
    }
}
