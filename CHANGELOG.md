# Changelog

本文件记录了 `Respect Affects Gameplay`  Mod 的所有重要变更。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/),
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/).

---

## [Unreleased]

### Added

- **核心功能: 尊重 `affects_gameplay` 标记**
  不再将所有已加载的 mod 都标记为 `modded`, 而是根据每个 mod 的 `affects_gameplay` 元数据判断.
  纯外观/辅助类 mod (`affects_gameplay: false`) 不再导致存档路径分离或阻止联机.

- **3 个 Harmony 补丁**, 精准控制存档路径而不影响 UI 显示:
  - `PatchGetIsRunningModded` — 拦截 `UserDataPathProvider.IsRunningModded` getter
  - `PatchSetIsRunningModded` — 拦截 `UserDataPathProvider.IsRunningModded` setter
  - `PatchGetProfileDir` — 拦截 `UserDataPathProvider.GetProfileDir()` 方法 (最终存档路径生成)

- **3 种 Modded Mode**, 游戏内设置页面下拉选择:
  - `Auto` (默认): 仅当存在 `affects_gameplay: true` 的已加载 mod 时视为 modded
  - `Always Vanilla`: 无论加载什么 mod, 始终视为 vanilla 状态
  - `Always Modded`: 只要加载了任意 mod, 始终视为 modded 状态

- **游戏内设置页面**, 通过 RitsuLib 框架注册, 包含模式下拉选择及详细说明文本

- **持久化设置存储**: 用户选择的 Modded Mode 保存到 `settings.json`, 跨游戏会话生效

- **Linux 兼容性**: 初始化时调用 `LinuxNativeHelper.EnsureLibGccLoaded()` 确保 `libgcc_s` 已加载

- **跨平台构建支持**: `.csproj` 自动检测 OS (Windows / macOS / Linux / Android) 选择正确的数据目录

- **CI/CD (GitHub Actions)**:
  - 每次 push/PR 自动编译 (ubuntu-latest + .NET 9.0)
  - CodeQL 安全分析
  - 依赖审查 (Dependency Review + Dependabot)

- **完整的中文 XML 文档注释**: 所有类、字段、属性、方法均有 `<summary>` / `<param>` / `<returns>` 注释

### Changed

- **`AlwaysModded` 模式数据源修正**:
  从 `RitsuModManager.GetKnownMods()` 改为直接读取 `ModManager.Mods` (游戏内部 mod 列表),
  与 `ModManager.IsRunningModded()` 原始实现的数据源保持一致, 消除兼容性差异.

### Removed

- **移除 `PatchModManagerIsRunningModded` 补丁**:
  不再拦截 `ModManager.IsRunningModded()` 方法. 该方法被游戏 UI (`NDebugInfoLabelManager`)、
  错误上报 (`SentryService`)、联机 mod 列表等多处调用, 统一替换返回值会导致主界面右下角
  和游戏内右上角的 mod 数量 / BASELIB 哈希值显示消失. 存档路径控制已由另外 3 个补丁完全覆盖.

### Fixed

- **修复 UI 中 mod 数量与哈希值消失**:
  移除对 `ModManager.IsRunningModded()` 的拦截后, 主界面右下角和游戏内右上角
  的 mod 加载数量及 BASELIB 模组哈希值恢复正常显示.

### Known Issues

- Steam Workshop 的 `ItemInstalled` 回调触发时, `RitsuModManager.GetKnownMods()` 可能尚未更新新安装的 mod 列表
- Android 平台上 JIT 编译受限时, Harmony 补丁可能无法生效 (取决于 Godot .NET Android 运行时的 Mono 版本)

### Planned

- **Steam 创意工坊兼容性验证**: 确保 mod 通过 Steam Workshop 分发时正确加载
- **Steam Deck 适配测试**: 在 SteamOS / Proton 环境下验证 Harmony 补丁的兼容性
- **Android 平台测试**: 验证 `.NET Android` 运行时下的 Harmony 运行时织入是否正常
- **macOS (Intel + Apple Silicon) 测试**: 确认 Rosetta 2 和原生 ARM 环境均正常工作
- **多语言支持**: 为设置页面中的 UI 文本添加本地化支持
- **Steam 创意工坊描述资源**: 准备 Workshop 页面所需的截图、描述、标签

---

[Unreleased]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.1.0...HEAD
