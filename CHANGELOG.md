# Changelog

<!--
  🔧 开发者注意事项

  本文件被 release.yml 两次读取：

  1. GitHub Release 正文 — 提取整个 ## [版本] 区块全部内容
  2. Steam 工坊 changenote — 只取第一个非 ### 分类标题的列表项（最多 200 字符）

    ✅ 正确做法：
       - 把面向玩家的一句话摘要放在第一个 ### 分类的第一行
       - 内部/CI/文档变更放在后面的 ### Internal 等分类
       - 第一个分类名建议用 ### Note 作为标题，便于区分 changenote 与其他分类

    ❌ 避免：
       - 把内部细节（构建/CI/格式化）放在第一个分类的第一行
       - changenote 超过 200 字符会触发 release.yml 的截断
-->

本文件记录了 `Respect Affects Gameplay`  Mod 的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/),
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/).

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

## [Unreleased]

[0.1.3]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.3
[0.1.2]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.2
[0.1.1]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.1
[0.1.0]: https://github.com/xiting910/RespectAffectsGameplay/releases/tag/v0.1.0
[Unreleased]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.1.3...HEAD
