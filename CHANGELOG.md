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
  - `自动` (默认): 仅当存在 `affects_gameplay: true` 的已加载 mod 时视为 modded
  - `始终原版`: 无论加载什么 mod, 始终视为 vanilla 状态
  - `始终 Modded`: 只要加载了任意 mod, 始终视为 modded 状态

- **游戏内设置页面**, 通过 RitsuLib 框架注册, 包含模式下拉选择及详细说明文本

- **持久化设置存储**: 用户选择的 Modded Mode 保存到 `settings.json`, 跨游戏会话生效

- **Linux 兼容性**: 初始化时调用 `LinuxNativeHelper.EnsureLibGccLoaded()` 确保 `libgcc_s` 已加载

- **跨平台构建支持**: `.csproj` 自动检测 OS (Windows / macOS / Linux / Android) 选择正确的数据目录；CI 环境通过 `stubs/` 桩项目提供编译所需的游戏程序集引用

- **CI/CD (GitHub Actions)**:
  - 每次 push/PR 自动编译 (ubuntu-latest + .NET 9.0)，通过根目录 `stubs/` 桩项目模拟游戏依赖
  - CodeQL 安全分析
  - 依赖审查 (Dependency Review + Dependabot)

- **完整的中文 XML 文档注释**: 所有类、字段、属性、方法均有 `<summary>` / `<param>` / `<returns>` 注释

### Known Issues

- Steam Workshop 的 `ItemInstalled` 回调触发时, `RitsuModManager.GetKnownMods()` 可能尚未更新新安装的 mod 列表
- Android 平台上 JIT 编译受限时, Harmony 补丁可能无法生效 (取决于 Godot .NET Android 运行时的 Mono 版本)

### Planned

- **Steam 创意工坊描述资源**: 准备 Workshop 页面所需的截图、描述、标签
- **Steam 创意工坊兼容性验证**: 确保 mod 通过 Steam Workshop 分发时正确加载
- **多语言支持**: 为设置页面中的 UI 文本添加本地化支持

---

[Unreleased]: https://github.com/xiting910/RespectAffectsGameplay/compare/v0.1.0...HEAD
