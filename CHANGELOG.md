# Changelog

<!--
  🔧 开发者注意事项

  本文件被 release.yml 两次读取：

  1. GitHub Release 正文 — 提取整个 ## [版本] 区块全部内容
  2. Steam 工坊 changenote — 提取 Note 分类的全部内容 (无字符数限制), 拼接为单行纯文本

    ✅ 正确做法：
       - 面向玩家的摘要放在 Note 分类中, 可包含多条列表项
       - 内部/CI/文档变更放在后面的 ### Internal 等分类

    ❌ 避免：
       - 把内部细节 (构建/CI/格式化) 放在 Note 分类中
       - Note 部分不要使用反引号 `, 会导致 changenote 解析异常
-->

本文件记录了 `Respect Affects Gameplay`  Mod 的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/),
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/).

---

## [Unreleased]

### Note

- 补丁架构迁移至 RitsuLib ModPatcher + IPatchMethod 体系，手动 Harmony 管理改为框架统一注册与诊断

### Changed

- **补丁架构重构: ModPatcher + IPatchMethod**: 三个补丁类 (`PatchGetAccountDir`, `PatchCopyUnmoddedSaveFilesIfNeeded`, `PatchModManagerIsRunningModded`) 实现 `IPatchMethod` 接口——通过 `GetTargets()` 声明补丁目标，替代旧的 `[HarmonyPatch]` 和 `[HarmonyPrefix]` 属性。入口改用 `RitsuLibFramework.CreatePatcher()` + `patcher.RegisterPatch<T>()` 显式注册补丁，替代 `new Harmony(…).PatchAll(assembly)` 全程序集扫描。`PatchModManagerIsRunningModded` 的条件启用从"先应用再 Unpatch"改为注册前根据设置判断是否注册，消除补丁短暂存在的时间窗口。新增 Critical/Optional 区分——`PatchGetAccountDir` 标记为 Critical（核心功能，失败回滚所有补丁），其余两个为 Optional（失败不影响模组加载）
- **核心逻辑独立: `GameplayStateHelper`**: `IsEffectivelyModded` 及其缓存字段、`EvaluateAutoMode()`、`EvaluateDefaultMode()` 从 `RespectAffectsGameplayMod` 入口类提取到独立的 `GameplayStateHelper` 静态类。入口类职责收缩为初始化流程（设置 → 补丁 → 事件订阅）和设置页面注册
- **删除 `LinuxNativeHelper`**: 其功能（Linux 平台 Harmony 兼容所需的 libgcc_s 预加载）已由 RitsuLib 框架在初始化时通过 `LinuxHarmonyNativePreloader` 自动处理，且覆盖更多库（libgcc_s + libunwind）
- **补丁类优化**: 三个补丁类标记为 `sealed`，`IPatchMethod` 接口成员添加 `<inheritdoc/>` 文档注释

### Internal

- **创意工坊标题**: 添加中文翻译 `智能识别并尊重不影响游戏的模组`（`Respect Affects Gameplay / 智能识别并尊重不影响游戏的模组`）
- **README**: 更新项目结构（+GameplayStateHelper, -LinuxNativeHelper），重写 Harmony 补丁章节为 ModPatcher + IPatchMethod 描述，更新设计决策说明（条件注册、Critical/Optional、Linux 预加载）

---

## [0.3.0] - 2026-07-05

### Note

- 翻译文件智能更新：新增版本追踪机制，更新 Mod 后不再覆盖你自行修改的翻译文本，仅补充新增的翻译条目。详细日志开关现在输出 Info 级别（可在游戏日志中直接查看，无需特殊工具）

### Changed

- **日志系统重构 (`ModLog`)**: `Debug()` 重命名为 `Verbose()`, 内部改用 `Info()` 级别输出以便在游戏日志中可见。移除日志前缀 `[ModInfo.Id]`（游戏日志器已自动添加 Mod 上下文, 无需重复）。所有调用处（`ContentModDetector`、`ModExtensions`、`LinuxNativeHelper`、`RespectAffectsGameplayMod`、三个补丁文件）同步更新
- **`nameof()` 消除硬编码字符串**: `ModInfo`、`ModSettingsHelper`、`PatchGetAccountDir`、`PatchCopyUnmoddedSaveFilesIfNeeded`、`PatchModManagerIsRunningModded`、`RespectAffectsGameplayMod` 中的字符串字面量统一替换为 `nameof()` 表达式, 确保类型/方法/参数重命名时自动更新, 消除重构死角
- **翻译文件版本化增量更新 (`ModLoc`)**: 新增 `_version` 键（`"0.3.0"`）追踪翻译文件版本。导出逻辑从简单流复制改为 JSON 解析 + 智能合并——版本不同时全量覆盖, 版本相同时仅补充缺失键, 避免覆盖用户自行修改的翻译。通过内存流承接嵌入资源、`JsonDocument` 解析、`JsonSerializerOptions` 缓存等方式确保健壮性
- **`ModSettingsHelper` 延迟初始化 + 缓存**: 移除显式 `Initialize()` 方法, 改为 `EnsureInitialized()` 懒加载——首次调用 `GetSettings()` 时自动触发。使用 `ModDataStoreCache<T>` 缓存设置数据, `ResetToDefaults()` 改用 `Modify()` 原子操作（消除 get-modify-save 模式中潜在的中间状态）, `SaveSettings()` 对应使用 `_settingsCache.Save()`
- **`LinuxNativeHelper` 常量提取**: 硬编码的 `"libgcc_s.so.1"` 文件名提取为 `LibGccFileName` 常量; 日志消息中对文件名的引用改为插值常量, 保持一致性

### Internal

- **`RespectAffectsGameplay.json`**: 版本号从 `0.2.9` 提升至 `0.3.0`
- **`eng.json` / `zhs.json`**: 缩进从 4 空格统一为 2 空格, 新增 `_version` 键; 更新详细日志描述文本, 移除 "Debug" 措辞
- **`workshop/image.png`**: 更新工坊封面图
- **README**: 更新顶部概述描述为更全面的功能介绍, 重组目录/章节结构（"模式说明" → "设置说明", "Mod 标记验证与 Toast 警告" → "内容 Mod 检测与误标警告"）, 改进表格格式对齐, 补充致谢中技术细节描述

---

## [0.2.9] - 2026-07-05

### Note

- ContentModDetector 改为懒加载扫描：不再需要显式调用 ScanAllMods()，首次查询时自动触发；存档复制检查延迟到主菜单就绪后执行，避免初始化阶段阻塞

### Changed

- **`ContentModDetector` 懒加载扫描**: `ScanAllMods()` 公有方法替换为私有 `PerformScan()`，新增 `_scanned` 标记。`HasContentModsLoaded()` 和 `IsContentMod()` 在首次调用时自动触发扫描，无需调用方显式初始化。消除 `Initialize()` 中对 `ScanAllMods()` 的显式调用依赖
- **存档复制检查延迟执行**: `Initialize()` 步骤 6 不再立即调用 `EnsureSaveFilesCopiedIfNeeded()`，改为订阅 `MainMenuReadyEvent` 延迟到主菜单就绪后执行。原步骤 7 合并入步骤 6，初始化流程从 7 步精简为 6 步
- **移除未使用的 using**: `ContentModDetector.cs` 移除未使用的 `using MegaCrit.Sts2.Core.Helpers;`

### Internal

- **`RespectAffectsGameplay.json`**: 版本号从 `0.2.8` 提升至 `0.2.9`
- **README**: 更新 `ContentModDetector` 描述为懒加载扫描机制、更新流程图起始节点、设计决策中步骤编号从 7 改为 6

---

## [0.2.8] - 2026-07-05

### Note

- 重构内容 Mod 检测系统：存档路径隔离改为基于程序集扫描（仅检测到 AbstractModel 子类的内容 Mod 才触发），非存档功能继续基于 affects_gameplay 标记 + 内容检测，实现存档/非存档双重判定标准

### Added

- **`ContentModDetector`**: 新增内容 Mod 检测器，替代旧的 `ModAffectsGameplayValidator`。提供 `ScanAllMods()`（扫描 + Toast 警告）、`HasContentModsLoaded()`（存档路径判定查询）、`IsContentMod(string modId)`（单个 Mod 查询）三个接口。内部使用 `ModIdsWithContent` HashSet 存储检测结果
- **`ModExtensions`**: 新增 `Mod` 扩展方法类，提供 `IsLoaded()`（判断 Loaded/Failed 状态）、`GetId()`（获取 Mod ID，优先 manifest.id → manifest.name → path → "UnknownMod"）、`ContainsAbstractModel()`（扫描程序集检测 AbstractModel 子类），消除代码中多处重复的内联逻辑
- **双重判定标准**: `IsEffectivelyModded` 新增 `isForSaveDir` 参数。存档路径判定 (`isForSaveDir: true`) 基于 `ContentModDetector.HasContentModsLoaded()` 程序集扫描结果，非存档判定 (`isForSaveDir: false`) 基于 `EvaluateAutoMode()` 的 affects_gameplay 标记 + 内容检测。两个判定独立缓存，互不干扰

### Changed

- **Manifest 描述更新**: `RespectAffectsGameplay.json` 和 `workshop/workshop.json` 中描述内容大幅扩展，反映程序集扫描、双重标准、误标警告、智能存档迁移等新特性
- **本地化描述更新**: `eng.json` / `zhs.json` 中 `mod.description` 和 `settings.mode.desc` 更新为双重标准说明——存档路径隔离仅对程序集扫描检测到的内容 Mod 生效，非存档功能继续基于 affects_gameplay 标记
- **`ModInfo` fallback 值**: `Id` 和 `Name` 属性在无法获取 ModInfo 时的回退值从硬编码的 `"RespectAffectsGameplay"` / `"Respect Affects Gameplay"` 改为通用 `"unknown"`，与 `Version` 属性保持一致
- **`ModdedMode` 枚举注释精简**: 移除各枚举值的冗长描述，保留核心语义
- **日志级别调整**: `IsEffectivelyModded` 异常日志从 `Error` 降为 `Warn`，更准确反映"保守假设为 modded"的回退行为并非错误
- **代码现代化**: `EvaluateAutoMode()` / `EvaluateDefaultMode()` 中 `.Where().Any()` 替换为 `.Where().ToList()` + `.Count == 0`（提前物化，避免多次迭代）；`new ModSettingsData()` 替换为 `new()`

### Removed

- **`ModAffectsGameplayValidator`**: 移除（200 行）。拆分为 `ContentModDetector`（检测 + Toast 通知）和 `ModExtensions`（Mod 扩展方法）。旧 `MislabeledGameplayMods` HashSet 被 `ContentModDetector.ModIdsWithContent` + `IsContentMod()` 替代，`EvaluationResult` 结构体不再需要
- **`PatchGetIsRunningModded`**: 移除。v0.108.0 后 `GetAccountDir(bool? forceModState)` 已成为所有路径构造的唯一决策点，不再需要拦截 `IsRunningModded` getter
- **`PatchSetIsRunningModded`**: 移除。同上原因，不再需要拦截 `IsRunningModded` setter

### Internal

- **`stubs/sts2/Stubs.cs`**: 移除不再使用的 `ModManager._mods` 私有字段桩和 `UserDataPathProvider.IsRunningModded` 属性桩；移除未使用的 `IDE0052` pragma
- **`stubs/0Harmony/Stubs.cs`**: 移除不再使用的 `Harmony.PatchAll(Type)` 重载桩方法
- **README**: 更新项目结构（新增/移除文件）、补丁表格（5→3 补丁）、设计决策（双重判定标准说明）、工作原理流程图、Mod 标记验证章节（`ModAffectsGameplayValidator` → `ContentModDetector`）、模式说明表格
- **`RespectAffectsGameplay.json`**: 版本号从 `0.2.7` 提升至 `0.2.8`
- **`workshop/image.png`**: 更新工坊封面图

---

## [0.2.7] - 2026-07-05

### Note

- 修复首次存档复制逻辑缺口：当非 gameplay Mod 跳过复制后、后续安装 gameplay Mod 时，补触发存档迁移，避免原版进度"丢失"

### Added

- **`EnsureSaveFilesCopiedIfNeeded`**: `Initialize()` 步骤 7 新增存档复制补触发检查。原版逻辑仅在 `_settings.ModList` 首次非空时触发 `CopyUnmoddedSaveFilesIfNeeded`，但 `PatchCopyUnmoddedSaveFilesIfNeeded` 会在非 gameplay 状态跳过复制，导致 `ModList` 被填充后即使后续安装 gameplay Mod 也不会再触发复制。本方法在 `IsEffectivelyModded()` 为 true 且 `ModManager.UnmoddedSavesWereCopied` 为 false 时补调用 `CopyUnmoddedSaveFilesIfNeeded`，确保 gameplay Mod 首次出现时存档一定会被迁移

### Changed

- **Manifest 描述更新**: `RespectAffectsGameplay.json` 中 `description` 移除"阻止原版联机"措辞（已在 v0.108.0 官方修复），聚焦存档路径保护

### Internal

- **`stubs/sts2/Stubs.cs`**: `ModManager` 新增 `UnmoddedSavesWereCopied` 属性桩
- **README**: 设计决策补充 `PatchCopyUnmoddedSaveFilesIfNeeded` 与 `EnsureSaveFilesCopiedIfNeeded` 的协同说明

---

## [0.2.6] - 2026-07-03

### Note

- 适配 STS2 v0.108.0 API 变更：替换存档路径补丁、移除已由官方修复的联机哈希补丁、新增首次存档复制拦截。正式版 v0.107.1 请继续使用 v0.2.5 版本，v0.2.6 及以上版本仅适用于 STS2 v0.108.0

### Added

- **`PatchGetAccountDir`**: 新增补丁拦截 `UserDataPathProvider.GetAccountDir(bool? forceModState)`——v0.108.0 将所有路径构造统一到该单一决策点。当 `forceModState` 为 null 时根据 `IsEffectivelyModded()` 返回 `""`（vanilla）或 `"modded"`；非 null 时透传原始逻辑
- **`PatchCopyUnmoddedSaveFilesIfNeeded`**: 新增补丁拦截 `ModManager.CopyUnmoddedSaveFilesIfNeeded()`——v0.108.0 引入的首次存档迁移方法。仅在 gameplay modded 状态时放行，避免纯外观 Mod 触发无用的存档副本

### Changed

- **代码风格统一**: `ModLog.cs`、`stubs/0Harmony/Stubs.cs`、`stubs/sts2/Stubs.cs` 中将表达式体成员（`=>`）转换为块体语句（`{}`），消除 IDE 编码风格建议
- **Using 指令排序**: `ModLoc.cs` 中项目级 using（`STS2RitsuLib`）移至前，系统级 using（`System.Text.RegularExpressions`）移至后
- **`min_game_version` 提升**: 从 `0.107.1` 提升至 `0.108.0`，因新增补丁依赖 v0.108.0 API
- **本地化描述更新**: `eng.json` / `zhs.json` 中 `mod.description` 移除"阻止原版联机"措辞（联机哈希问题已在 v0.108.0 官方修复），改为聚焦存档路径保护

### Removed

- **`PatchGetProfileDir`**: 移除。v0.108.0 将 `GetProfileDir(int)` 替换为 `GetAccountDir(bool?)` 作为路径构造入口，原补丁不再适用
- **`PatchModelIdSerializationCache`**: 移除。联机哈希污染问题已在 STS2 v0.108.0 官方修复——`Init()` 重写为基于 `ContentSorter<T>` 并加入 `affectsGameplay` 过滤，`JoinFlow` 通过 `GetGameplayRelevantModNameList()` 正确区分 gameplay 与非 gameplay Mod
- **RitsuLib 哈希补丁冲突检测**: 随 `PatchModelIdSerializationCache` 移除，`Initialize()` 中步骤 5（检测 `ModelIdSerializationCacheDynamicContentPatch` 并自动 Unpatch）同步移除，初始化步骤从 7 步精简为 6 步

### Fixed

- **`ModAffectsGameplayValidator` 程序集扫描**: `mod.assembly`（单个）→ `mod.assemblies`（列表），适配 v0.108.0 Mod 支持多程序集的 API 变更。使用 `SelectMany` 遍历所有程序集中的 `AbstractModel` 子类
- **验证器异常日志级别**: 扫描 `AbstractModel` 子类时的异常日志从 `Debug` 提升为 `Warn`，并输出完整异常信息而非仅 `Message`

### Internal

- **`.editorconfig` 重构**: 所有 `dotnet_style_*` 和 `dotnet_diagnostic.CA*` 规则严重级别从 `warning` 降为 `suggestion`；移除约 50+ 条冗余 CA 诊断规则；新增安全相关 CA 规则（CA30xx–CA53xx）；行尾从 LF 改为 CRLF；增加中文注释；移除 XML 专属节，新增 C#/VB 专属节
- **`stubs/sts2/Stubs.cs`**: `ModManifest` 从 `class` 改为 `record`（匹配 v0.108.0）；`Mod.assembly` → `Mod.assemblies`（`Assembly?` → `List<Assembly>`）；新增 `ModManager.CopyUnmoddedSaveFilesIfNeeded()` 桩方法；`UserDataPathProvider.GetProfileDir(int)` → `GetAccountDir(bool?)`；移除 `ModelIdSerializationCache` 桩类及其命名空间 `MegaCrit.Sts2.Core.Multiplayer.Serialization`；新增 `#pragma warning disable CA1051`
- **`stubs/0Harmony/Stubs.cs`**: 移除约 100 行未使用的桩定义——`HarmonyPostfix`、`HarmonyFinalizer` 特性、`HarmonyPatchType` 枚举、`AccessTools` 静态类（含 `Field`、`Property`、`StaticFieldRefAccess`、`TypeByName`、`Method`、`PropertyGetter`）、`Harmony.Unpatch(MethodBase, HarmonyPatchType, string?)` 重载、`HarmonyPatch(Type)` 及 `HarmonyPatch(Type, string, Type[])` 构造函数重载；新增 `#pragma warning disable IDE0290` 和 `CA1710`
- **补丁文件注释完善**: `PatchGetIsRunningModded`、`PatchSetIsRunningModded`、`PatchModManagerIsRunningModded` 中将紧凑表达式体展开为块体语句并添加逐行注释
- **`RespectAffectsGameplayMod.cs`**: 移除 `using MegaCrit.Sts2.Core.Multiplayer.Serialization`；移除 `RitsuLibHashPatchTypeName` 常量
- **`RespectAffectsGameplay.json`**: 缩进从 4 空格统一为 2 空格
- **README**: 更新项目结构、补丁表格、设计决策说明，新增 "首次存档复制的副作用" 问题描述，联机哈希污染标注为"已在 v0.108.0 官方修复"

---

## [0.2.5] - 2026-07-01

### Note

- 设置页面标题描述现支持本地化，根据游戏语言自动切换中英文

### Added

- **设置页面描述本地化**: `RegisterSettingsPage()` 新增 `.WithDescription(ModSettingsText.I18N(...))`，使用本地化键 `mod.description` 替代 manifest `description` 回退。`eng.json` / `zhs.json` 分别添加对应的中英文描述文本

### Changed

- **Workshop VDF 描述换行处理**: `release.yml` 中 workshop 描述从压扁为空格改为转义 VDF 特殊字符（`\`、`"`）+ 保留换行符，Steam 工坊页面按段落显示

---

## [0.2.4] - 2026-07-01

### Note

- 重构本地化系统使用 RitsuLib I18N 框架，移除冗余的 ModManifestHelper 反射层，新增 RitsuLib 联机哈希补丁冲突检测

### Added

- **RitsuLib 联机哈希补丁冲突检测**: 初始化步骤 5 中检测 RitsuLib 是否已安装 `ModelIdSerializationCacheDynamicContentPatch`，若存在则自动禁用本 Mod 的 `PatchModelIdSerializationCache`，避免两个补丁冲突导致异常

### Changed

- **`ModLoc` 本地化系统重构**: 从自定义 JSON 文件磁盘加载方案迁移至 RitsuLib 内置 `I18N` 框架（`RitsuLibFramework.CreateModLocalization`）。翻译文件通过嵌入资源（`EmbeddedResource`）分发，初始化时自动导出到用户目录，替代原有的手动 `.lang` 文件管理
- **移除 `ModManifestHelper` 反射层**: 游戏已稳定提供 `Mod.manifest` 字段访问，不再需要反射安全回退。`ModAffectsGameplayValidator`、`RespectAffectsGameplayMod.EvaluateAutoMode()` 和 `PatchModelIdSerializationCache` 直接通过 `mod.manifest` 及其属性访问元数据
- **设置页面文本绑定**: 从静态 `ModLoc` 属性改为 `ModSettingsText.I18N` 动态本地化绑定，简化代码并提高可维护性

### Fixed

- **构建产物目录逻辑**: `.csproj` Post-build 目标简化，移除手动拼接文件列表，直接从 `workshop/content/` 统一输出

### Internal

- **`.csproj`**: 本地化文件从 `*.lang` 文件复制改为 `EmbeddedResource` 嵌入方式；新增 `WorkshopContentDir` 属性简化路径管理；移除 localization 复制步骤（现由嵌入资源处理）
- **`release.yml`**: Package mod 步骤改为直接从 `workshop/content/` 复制，移除重复的手动文件拷贝逻辑
- **`stubs/0Harmony/Stubs.cs`**: 新增 `HarmonyPatchType` 枚举、`Harmony.Unpatch(MethodBase, HarmonyPatchType, string?)` 重载、`AccessTools.TypeByName`、`AccessTools.Method`、`AccessTools.PropertyGetter` 桩方法
- **`stubs/sts2/Stubs.cs`**: 移除未使用的 `ModInfo` 和 `LogLevel` 桩类型（IDE1006 抑制符同步移除）
- **`workshop/image.png`**: 更新工坊封面图
- **README 徽章更新**: 移除 Release 和 STS2 版本徽章，新增 Dependency Review 徽章

---

## [0.2.3] - 2026-06-30

### Note

- 改进 manifest 反射访问策略，优先使用字段访问并回退到属性

### Changed

- **`ModManifestHelper` 反射策略优化**: 静态成员从 `AccessTools.Property` 改为 `AccessTools.Field` 作为优先访问方式，失败时回退到 `AccessTools.Property`。

### Internal

- `stubs/0Harmony/Stubs.cs`: 新增 `AccessTools.Field` 桩方法和 `Harmony.PatchAll(Type)` 重载
- `stubs/sts2/Stubs.cs`: `ModManifest` 成员和 `Mod.manifest` 从属性改为公共字段，匹配游戏 v0.107.1 实际定义

---

## [0.2.2] - 2026-06-30

### Note

- 修复游戏 v0.107.1 中 Mod.manifest 属性缺失导致的 MissingMethodException 崩溃，改用反射安全访问 Mod 元数据

### Fixed

- **`MissingMethodException` 崩溃**: 游戏 v0.107.1 的 `Mod` 类型移除了 `manifest` 属性（getter `get_manifest()`），导致 `ValidateAll()`、`EvaluateAutoMode()` 和联机哈希过滤在 JIT 编译时崩溃。新增 `ModManifestHelper` 反射辅助类，通过 `AccessTools.Property` 安全访问 `Mod.manifest`、`ModManifest.affectsGameplay`、`id`、`name`，属性不存在时返回安全默认值
- **本地化文件被误识别为 Mod manifest**: 游戏递归扫描 `.json` 文件寻找 Mod 清单，`localization/eng.json` 和 `zhs.json` 因缺少 `id` 字段而报错。将扩展名改为 `.lang`（内容仍为 JSON），避免被游戏扫描器发现

### Changed

- `ModLoc.LoadLocalization()`: 从磁盘读取 `.lang` 文件
- `ModAffectsGameplayValidator.ValidateAll()`: 改用 `ModManifestHelper` 反射访问 manifest
- `RespectAffectsGameplayMod.EvaluateAutoMode()`: 改用 `ModManifestHelper` 反射访问 manifest
- `PatchModelIdSerializationCache.GetModsPrefix()`: 改用 `ModManifestHelper` 反射访问 manifest

### Added

- **`ModManifestHelper`** 反射辅助类: 封装 `AccessTools.Property` 调用，提供 `GetManifest()`、`GetAffectsGameplay()`、`GetId()`、`GetName()` 安全方法

### Internal

- `stubs/0Harmony/Stubs.cs`: 新增 `AccessTools.Property` 桩方法，确保 CI 环境编译通过
- `simulate-ci.bat`: 文件检查改为 `.lang` 扩展名

---

## [0.2.1] - 2026-06-30

### Note

- 新增误标 Mod 检测与 Toast 警告：自动识别 affects_gameplay 标记不准确的 Mod 并在主菜单弹出提醒；Auto 模式下还会强制将其视为玩法 Mod

### Added

- **误标 Mod 检测与 Toast 通知**: `ModAffectsGameplayValidator` 在初始化阶段自动扫描每个已加载 Mod 的程序集（通过 `ReflectionHelper.GetSubtypesFromAssembly`），若 `affects_gameplay: false` 的 Mod 包含 `AbstractModel` 子类型，则加入 `MislabeledGameplayMods` 集合。验证完成后订阅 `MainMenuReadyEvent`，主菜单就绪时通过 `RitsuToastService` 弹出 5 秒 `Warning` 级 Toast，列出问题 Mod ID 并建议玩家联系 Mod 作者修复或暂时禁用
- **Auto 模式自动修正**: `EvaluateAutoMode()` 的分类逻辑新增 `MislabeledGameplayMods` 检查——即使 Mod 的 `affects_gameplay` 标记为 `false`，只要存在于 `MislabeledGameplayMods` 集合中，即被强制视为 gameplay Mod。这意味着验证器不仅发出警告，还能**主动修正**误标 Mod 对 modded 状态判定的影响，防止存档路径和联机哈希被污染
- **`EvaluationResult` 结构体**: 封装单个 Mod 的判定结果，包含 `ShouldTreatAsGameplay`、`Reason`（如 "包含 AbstractModel 子类: xxx"）和 `Exception` 字段
- **Toast 本地化文本**: `eng.json` / `zhs.json` 新增 `toast.mislabeled.title` 和 `toast.mislabeled.body` 键

### Changed

- **版本号回退值**: `ModInfo.Version` 在无法获取 JSON 版本时回退值从 `"0.1.0"` 改为 `"unknown"`，避免误导

### Removed

- **日志本地化方法**: 移除 `ModLoc` 中所有 `Log*` 系列方法和 `Format()` 辅助方法（日志消息无需本地化，直接硬编码即可）

### Internal

- **代码清理**: `RespectAffectsGameplayMod.cs` 移除未使用的 `using STS2RitsuLib.Compat;`
- **`.gitignore`**: 新增 `simulate-ci.bat`

---

## [0.2.0] - 2026-06-27

### Note

- 新增多语言本地化支持（中文/英文），优化 modded 状态判断性能与稳定性，修复可能存在的 Linux 平台兼容性问题

### Added

- **本地化系统**: 新增 `ModLoc.cs` 及 `localization/` 目录下的 JSON 语言文件 (`eng.json`、`zhs.json`)，所有设置页面 UI 文本和日志消息均支持多语言。
- **设置页面标识符常量化**: `RegisterSettingsPage()` 中所有 UI 控件的标识符 (`"general"`、`"mode"` 等) 改为 `private const` 命名常量，消除 magic string

### Changed

- **日志级别修正**: `ModLog.Info()` 现在始终输出，不再受 `VerboseLogging` 开关控制。仅 `Debug()` 级别受详细日志开关控制，符合标准日志惯例
- **本地化文本**: `Initialize()` 和 `RegisterSettingsPage()` 中所有硬编码的中文/英文文本全部迁入 `ModLoc` 本地化属性，由 JSON 语言文件驱动
- **README 项目结构**: 更新 Scripts 目录文件列表，新增 `ModLoc.cs` 和 `localization/` 条目

### Fixed

- **Linux 原生库字符串编码**: `dlopen()` 的 `StringMarshalling` 从 `Utf16` 修正为 `Utf8`，确保 Linux 平台上 `libgcc_s.so.1` 文件名以正确编码传递
- **Release 界面划分线**: 修复 GitHub Release 页面中的分割线显示异常问题

### Optimized

- **`IsEffectivelyModded()` 结果缓存**: 添加 `_cachedIsEffectivelyModded` 字段。首次计算后缓存结果，后续调用直接返回，避免每次存档路径查询都遍历所有 mod
- **Auto 模式安全回退**: 当 `RitsuModManager.GetKnownMods()` 返回 0 个已知 mod 时，自动回退到 `EvaluateDefaultMode()` 以保守评估
- **Steam Workshop 重复过滤**: `EvaluateAutoMode()` 中额外过滤 `IsDisabledSteamWorkshopDuplicate` 的条目，防止同名本地 + Workshop mod 被重复计数

### Internal

- **构建脚本更新**: `.csproj` 新增 `localization/*.json` 到 `workshop/content/localization/` 的复制步骤
- **Release 工作流更新**: `release.yml` Package mod 步骤新增 `cp -r Scripts/localization`，确保 GitHub Release zip 包和 Steam 工坊上传均包含本地化文件

---

## [0.1.8] - 2026-06-26

### Note

- 本版本无游戏功能变更，完善了项目文档、CI 配置并修复了工坊描述错误

### Changed

- **Dependabot 检查频率**: 从每周一改为每日检查，更快获取依赖更新
- **README 文档完善**: 新增安装说明章节（Steam 创意工坊推荐 + 手动安装）、补充项目结构中各工作流和 Issue 模板说明、修正问题描述中的措辞

### Fixed

- **workshop.json 描述格式错误**: GitHub 链接与描述文本合并时缺少空格导致 URL 粘连，现已修正

### Internal

- Release body 精简：移除安装方法和依赖章节（README 已包含），移除冗余的源代码链接
- `.gitignore` 新增 `tag-and-push.bat`
- README 新增 CodeQL 徽章

---

## [0.1.7] - 2026-06-25

### Note

- 本版本无游戏功能变更，修复了 GitHub Release 页面的问题。

### Fixed

- **Release 贡献者头像不显示**: HTML 注释中的 @mentions 不会被 GitHub 解析。改为 `<details>` 折叠块，默认收起不影响阅读，同时能正确触发底部贡献者头像

---

## [0.1.6] - 2026-06-25

### Note

- 本版本无游戏功能变更，修复了 v0.1.5 Steam 创意工坊上传仍失败的问题。

### Fixed

- **Steam 工坊 VDF 依赖格式错误**: `dependencies` 块中每个条目必须是 `"index" "id"` 键值对，不能只有单独的 ID。修复后 steamcmd 可正确解析 `workshop.vdf`

### Internal

- Release body 贡献者 @mentions 移至 HTML 注释中，正文不可见但仍触发 GitHub 底部头像

---

## [0.1.5] - 2026-06-25

### Note

- 本版本无游戏功能变更，仅为 Steam 创意工坊发布流程的基础设施更新。

### Fixed

-  `workshop_build_commit` 已弃用，改为 `workshop_build_item`，修复了 CI 发布流程中 Steam 工坊上传失败的问题

### Internal

- **cache-dependency-path**: CI 构建缓存依赖路径改为 `**/*.csproj` + `**/*.slnx`

---

## [0.1.4] - 2026-06-25

### Note

- 本版本无游戏功能变更，修复了 Steam 创意工坊自动上传的一些问题，并更新了依赖项。

### Changed

- **STS2.RitsuLib 依赖**: 从 0.4.35 升级到 0.4.36

### Fixed

- **Steam 创意工坊上传失败**: `config.vdf` 缓存凭据解压路径错误，导致 steamcmd 报告 "Cached credentials not found" 并回退到密码登录失败。现修正为 steamcmd 实际读取的 `~/Steam/config/` 路径
- **工坊依赖项未同步**: 生成 `workshop.vdf` 时遗漏了 `dependencies` 字段，导致工坊物品的依赖关系未随上传更新。现已从 `workshop.json` 读取并写入 VDF
- **工坊 VDF 描述兼容性**: 包含换行符的描述文本现会被压平为单行，避免 VDF 解析异常

### Internal

- GitHub Release 正文新增提交历史与贡献者列表（通过 `fetch-depth: 0` + `git log` 生成）
- Dependabot 自动合并流程优化：新增 `rebase-strategy: auto`、`update-branch` 步骤、squash 合并
- GitHub Actions 依赖批量升级：`upload-artifact` v4→v7、`download-artifact` v4→v8、`action-gh-release` v2→v3

---

## [0.1.3] - 2026-06-25

### Note

- 本版本无游戏功能变更，仅为 Steam 创意工坊发布流程的基础设施更新。

### Changed

- **构建产物路径**: Post-build 目标从游戏 `mods/` 文件夹改为 `workshop/content/`，适配 Steam 工坊上传流程
- **文档精简**: README 移除冗余发布章节，新增 stubs 桩代码维护说明

### Internal

- CI 全工作流启用 .NET SDK 缓存加速构建
- Release 工作流重构：工坊元数据改为从 `workshop.json` + `mod_id.txt` 文件读取，支持多种可见性
- Release 工作流自动从 CHANGELOG.md 提取 changenote 用于工坊更新说明
- 新增 `workshop/` 目录结构，`.gitignore` 忽略 `workshop/content/`
- 统一 `.github/` 模板、`LICENSE` 等多文件代码风格

---

## [0.1.2] - 2025-06-25

### Fixed

- **Stubs `Harmony.Unpatch` 签名不匹配**: 将第一个参数从 `MethodInfo` 改为 `MethodBase`，匹配真实 HarmonyLib 2.4.2 的 API，修复运行时抛出 `MissingMethodException` 的问题

---

## [0.1.1] - 2025-06-25

### Fixed

- **Stubs 类名与真实 HarmonyLib 不匹配**: 将 `HarmonyPatchAttribute` → `HarmonyPatch`、`HarmonyPrefixAttribute` → `HarmonyPrefix`、`HarmonyPostfixAttribute` → `HarmonyPostfix`、`HarmonyFinalizerAttribute` → `HarmonyFinalizer`，修复 Release 产物在运行时抛出 `TypeLoadException` 的问题

---

## [0.1.0] - 2025-06-25

### Added

- **统一日志系统 `ModLog`**: 全局共享的日志包装类, 自动为每条日志添加 `[ModInfo.Id]` 前缀,
  无需调用方硬编码 mod ID

- **"详细日志" 设置开关**: 游戏内设置页面新增 Toggle, 开启后输出 Debug/Info 级别日志,
  关闭后仅输出 Warn/Error。仅影响本 mod, 不影响游戏或其他 mod 的日志。修改后即时生效, 无需重启

- **全面的异常处理与容错**:
  - `PatchModelIdSerializationCache.InitFinalizer` 不再静默吞异常, 改为记录完整错误信息
  - `GetModsPrefix` 反射访问 `_mods` 添加 try-catch, 失败时回退到原始 getter
  - `ModSettingsHelper.ResetToDefaults()` 添加 `_store` null 检查, 防止静默失败
  - `ModInfo.Cached` 添加 try-catch, 查询自身 ModInfo 失败时使用 fallback 值
  - `LinuxNativeHelper.EnsureLibGccLoaded` 检查 `dlopen` 返回值并记录结果
  - `IsEffectivelyModded()` catch 块记录异常详情, 不再静默吞错

- **详细运行时日志**: 所有 5 个 Harmony 补丁、设置加载/保存、模式判断等关键路径
  均添加 Debug/Info 级别日志, 方便排查游戏更新后的兼容性问题

- **桩文件完善** (CI 构建支持):
  - sts2 桩新增 `Logger`、`LogLevel`、`LogType`、`ModelIdSerializationCache.Hash`
  - 修正 `ModManager.IsRunningModded` 为方法 (匹配实际游戏 API)
  - 修正 `UserDataPathProvider` 为静态类
  - 0Harmony 桩新增 `Harmony.GetPatchedMethods()`

- **`.editorconfig` 代码风格配置**: 统一项目代码风格（空格缩进、LF 换行、UTF-8 编码），
  为 `[*.{yml,yaml}]` 单独设置 2 空格缩进，不影响 C# 代码的 4 空格缩进

- **核心功能: 尊重 `affects_gameplay` 标记**
  不再将所有已加载的 mod 都标记为 `modded`, 而是根据每个 mod 的 `affects_gameplay` 元数据判断.
  外观/基础库/辅助类等 mod (`affects_gameplay: false`) 不再导致存档路径分离或阻止联机.

- **5 个 Harmony 补丁** (4 个始终启用 + 1 个可选):
  - `PatchGetIsRunningModded` — 拦截 `UserDataPathProvider.IsRunningModded` getter
  - `PatchSetIsRunningModded` — 拦截 `UserDataPathProvider.IsRunningModded` setter
  - `PatchGetProfileDir` — 拦截 `UserDataPathProvider.GetProfileDir()` 方法 (最终存档路径生成)
  - `PatchModelIdSerializationCache` — 通过 Prefix+Postfix+Finalizer 临时标志位方案,
    在 `ModelIdSerializationCache.Init()` 执行期间过滤 `ModManager.Mods`,
    使联机 XXH32 哈希仅由 gameplay Mod 决定, 防止非 gameplay Mod（外观/基础库/辅助类等）导致 "版本不匹配" 错误
  - `PatchModManagerIsRunningModded` — 可选拦截 `ModManager.IsRunningModded()`,
    默认关闭 (用户设置中的开关控制), 开启后 UI / Sentry / 联机列表也受 Modded Mode 控制

- **"拦截 IsRunningModded()" 开关**: 游戏内设置页面新增 `AddToggle` 布尔开关,
  允许用户选择是否让 `ModManager.IsRunningModded()` 也受当前 Modded Mode 控制.
  默认关闭 (仅存档路径和哈希受控, UI 和联机列表不受影响).
  所有设置项均标注 "⚠ 修改后需重启游戏才能生效"

- **3 种 Modded Mode**, 游戏内设置页面 `AddEnumChoice` 下拉选择:
  - `Auto` (默认): 仅当存在 `affects_gameplay: true` 的已加载 mod 时视为 modded
  - `Always Vanilla`（强制原版）: 强制视为 vanilla 状态，无论加载了什么 mod（⚠ 可能导致存档损坏）
  - `Default`（游戏默认）: 使用游戏原版逻辑, 数据源为 `ModManager.Mods`, 与原始 `IsRunningModded()` 行为一致

- **游戏内设置页面**, 通过 RitsuLib 框架注册, 包含模式下拉选择、布尔开关、重置按钮及详细说明文本

- **"重置为默认设置" 按钮**: 设置页底部 `AddButton`，一键将所有设置恢复默认值并即时持久化

- **持久化设置存储**: 用户设置保存到 `settings.json` (Global 作用域), 跨游戏会话生效

- **Linux 兼容性**: 初始化时调用 `LinuxNativeHelper.EnsureLibGccLoaded()` 确保 `libgcc_s` 已全局加载

- **跨平台构建支持**: `.csproj` 自动检测 OS (Windows / macOS / Linux / Android) 选择对应的 `data_sts2_<platform>_<arch>` 数据目录;
  CI 环境通过 `stubs/` 桩项目提供编译所需的空游戏程序集引用 (sts2.dll, 0Harmony.dll)

- **CI/CD (GitHub Actions)**:
  - `ci.yml`: 每次 push/PR 自动编译验证 (ubuntu-latest + .NET 9.0)
  - `release.yml`: 推送 `v*` 标签自动创建 GitHub Release，附带自动打包和 CHANGELOG 提取；支持可选 Steam 创意工坊自动更新
  - CodeQL 安全分析 / 依赖审查 (Dependency Review + Dependabot)

- **完整的中文 XML 文档注释**: 所有类、字段、属性、方法均有 `<summary>` / `<param>` / `<returns>` 注释

### Changed

- **枚举 `AlwaysModded` 重命名为 `Default`**: 消除 "始终" 歧义，更准确反映其"使用游戏原版逻辑"的行为
- **中文标签优化**: `AlwaysVanilla` → "强制原版"（原 "始终原版"），`Default` → "游戏默认"（原 "原始行为"），避免 "原" 字混淆
- **设置绑定即时持久化**: 用户修改下拉选择或开关后立即写入 `settings.json`，不再依赖框架延迟 flush
- **文档术语修正**: "外观 Mod" → "非 gameplay Mod（外观/基础库/辅助类等）"，准确覆盖所有 `affects_gameplay: false` 的 Mod 类型
- **修正 `BeginModDataRegistration` 用法**: 改用 `using` 模式，scope 在数据注册完毕后立即 dispose，确保框架正确调用 `InitializeGlobal`

---

[Unreleased]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.2.9...v0.3.0
[0.2.9]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.2.8...v0.2.9
[0.2.8]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.2.7...v0.2.8
[0.2.7]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.7
[0.2.6]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.6
[0.2.5]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.5
[0.2.4]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.4
[0.2.3]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.3
[0.2.2]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.2
[0.2.1]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.1
[0.2.0]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.2.0
[0.1.8]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.8
[0.1.7]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.7
[0.1.6]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.6
[0.1.5]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.5
[0.1.4]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.4
[0.1.3]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.3
[0.1.2]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.2
[0.1.1]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.1
[0.1.0]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.0
