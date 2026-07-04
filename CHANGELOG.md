# 更新日志 / Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/) 风格，版本号遵循 [SemVer](https://semver.org/)。

## [Unreleased]

## [0.3.2] — 2026-07-04
### 修复 / Fixed
- 修正输出格式胶囊与主题分段控件选中态文字被全局 `TextBlock` 样式覆盖的问题，确保选中后文字实际显示为白色。

## [0.3.1] — 2026-07-04
### 新增 / Added
- 转换成功后记录输出文件大小，文件行显示原大小到新大小和压缩比例。
- 顶部成果区显示完成百分比、已节省空间和平均压缩比例。
- 成功行支持打开转换后的图片、双击打开、右键在文件夹中显示、复制输出路径、打开源文件位置。
- 失败行支持重试单个文件。

### 变更 / Changed
- 进一步贴近 Inspector 设计稿：补齐 Header 帮助按钮、AddBar 浏览入口、输出标题菱形、主按钮箭头和 footer 文件夹图标。
- 修正输出格式胶囊与主题分段控件的选中态文字颜色，选中后使用白字以保持对比度。
- 调整 Inspector 格式胶囊间距，使 JPG / PNG / WebP / BMP / TIFF 在目标宽度下一行显示。

## [0.3.0] — 2026-07-04
### 新增 / Added
- 主界面重构为左侧内容区、右侧 Inspector、底部固定操作条的专业工具布局。
- 输出格式改为胶囊单选，质量、命名、输出位置、元数据、外观、语言、关于集中在右侧 Inspector。
- 主题支持「浅色 / 深色 / 跟随系统」三段选择，并继续持久化用户偏好。
- 文件列表改为单列卡片行骨架，展示格式标签、目标/源文件名、路径、大小摘要和状态胶囊。

### 变更 / Changed
- 移除 Header 右上角亮/暗切换入口，主题设置统一收进 Inspector。
- README 同步 Inspector 布局与三态主题说明。

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

[Unreleased]: https://github.com/dosheda/ImageConverter/compare/v0.3.2...HEAD
[0.3.2]: https://github.com/dosheda/ImageConverter/releases/tag/v0.3.2
[0.3.1]: https://github.com/dosheda/ImageConverter/releases/tag/v0.3.1
[0.3.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.3.0
[0.2.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.2.0
[0.1.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.1.0
