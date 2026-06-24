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
        using (RitsuLibFramework.BeginModDataRegistration(ModInfo.Id))
        {
            _store = RitsuLibFramework.GetDataStore(ModInfo.Id);
            _store.Register(
                key: DataKey,
                fileName: DataFileName,
                scope: DataScope,
                defaultFactory: () => new ModSettingsData(),
                autoCreateIfMissing: true
            );
        }
    }

    /// <summary>
    /// 获取当前设置
    /// </summary>
    /// <returns>当前的 <see cref="ModSettingsData"/> 实例</returns>
    public static ModSettingsData GetSettings()
    {
        // 如果数据存储未初始化, 返回默认设置
        if (_store is null) { return new(); }

        try
        {
            // 尝试从存储中获取设置数据
            return _store.Get<ModSettingsData>(DataKey);
        }
        catch
        {
            // 如果获取设置数据失败, 返回默认设置
            return new();
        }
    }

    /// <summary>
    /// 重置所有设置为默认值并持久化到磁盘
    /// </summary>
    public static void ResetToDefaults()
    {
        var settings = GetSettings();
        settings.Mode = ModdedMode.Auto;
        settings.PatchModManagerIsRunningModded = false;
        SaveSettings();
    }

    /// <summary>
    /// 将待处理的更改持久化到磁盘
    /// </summary>
    public static void SaveSettings() => _store?.Save(DataKey);
}
