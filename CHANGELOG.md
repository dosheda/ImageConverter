# 更新日志 / Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/) 风格，版本号遵循 [SemVer](https://semver.org/)。

## [Unreleased]
- 计划：Inspector 布局重构、成果统计（已节省空间）、打开/定位交互、主题「跟随系统」。详见 `docs/DEV-SPEC-v0.3.md`。

## [0.2.0] — 2026-07-04
### 新增 / Added
- 全新 UI 视觉设计：现代靛蓝 Fluent 风格、卡片化布局、自定义控件模板。
- 暗色主题 + 一键亮/暗切换，选择持久化。
- 全新渐变应用图标（多分辨率 `.ico`，窗口与可执行文件通用）。
- 标题栏显示版本号徽标。
- GitHub Actions CI（Windows 上构建 + 测试）与 Release 工作流（打 tag 自动发布 win-x64 便携包）。
- MIT 许可证；双语（中/英）README 与界面截图。

### 变更 / Changed
- 主题/样式系统重写为完整的语义化画刷集，支持运行时切换。

## [0.1.0] — 2026-06-18
### 新增 / Added
- 首个版本：批量图片转换（JPG/PNG/WebP/BMP/TIFF ⇄ HEIC/HEIF 等）。
- 拖拽文件/文件夹、递归扫描、忽略不支持文件并计数。
- 命名规则、输出目录、质量、EXIF（默认剥离 GPS）、目录结构、覆盖选项。
- 取消后续跑、安全写入（先临时文件后移动）、绝不删除或覆盖原图。
- 14 种界面语言；设置持久化；转换日志。

[Unreleased]: https://github.com/dosheda/ImageConverter/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.2.0
[0.1.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.1.0
