# Changelog

本文件记录了 `Respect Affects Gameplay`  Mod 的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/),
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/).

---

## [Unreleased]

### Added

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
  - 每次 push/PR 自动编译 (ubuntu-latest + .NET 9.0)
  - CodeQL 安全分析
  - 依赖审查 (Dependency Review + Dependabot)

- **完整的中文 XML 文档注释**: 所有类、字段、属性、方法均有 `<summary>` / `<param>` / `<returns>` 注释

### Changed

- **枚举 `AlwaysModded` 重命名为 `Default`**: 消除 "始终" 歧义，更准确反映其"使用游戏原版逻辑"的行为
- **中文标签优化**: `AlwaysVanilla` → "强制原版"（原 "始终原版"），`Default` → "游戏默认"（原 "原始行为"），避免 "原" 字混淆
- **设置绑定即时持久化**: 用户修改下拉选择或开关后立即写入 `settings.json`，不再依赖框架延迟 flush
- **文档术语修正**: "外观 Mod" → "非 gameplay Mod（外观/基础库/辅助类等）"，准确覆盖所有 `affects_gameplay: false` 的 Mod 类型
- **修正 `BeginModDataRegistration` 用法**: 改用 `using` 模式，scope 在数据注册完毕后立即 dispose，确保框架正确调用 `InitializeGlobal`

### Known Issues

- Steam Workshop 的 `ItemInstalled` 回调触发时, `RitsuModManager.GetKnownMods()` 可能尚未更新新安装的 mod 列表
- Android 平台上 JIT 编译受限时, Harmony 补丁可能无法生效 (取决于 Godot .NET Android 运行时的 Mono 版本)

### Planned

- **Steam 创意工坊描述资源**: 准备 Workshop 页面所需的截图、描述、标签
- **Steam 创意工坊兼容性验证**: 确保 mod 通过 Steam Workshop 分发时正确加载
- **多语言支持**: 为设置页面中的 UI 文本添加本地化支持

---

[Unreleased]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.1.0...HEAD
