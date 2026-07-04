# 更新日志 / Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/) 风格，版本号遵循 [SemVer](https://semver.org/)。

## [Unreleased]

## [0.4.2] — 2026-07-05
### 修复 / Fixed
- 拖放事件冒泡导致 `OnDrop` 触发两次、扫描重复执行，且状态栏被覆盖为“没有发现新的受支持图片文件”。现标记事件已处理，仅执行一次。

### 变更 / Changed
- 清理旧版界面遗留的约 10 个未使用本地化键（列头、隐私提示等），减少各语言文件冗余。

## [0.4.1] — 2026-07-05
### 新增 / Added
- 补全其余 11 种界面语言的翻译（de/es/fr/it/ja/ko/pl/pt-BR/ru/tr/cs），此前 v0.3/v0.4 新增文案在这些语言下仍为英文占位。

### 变更 / Changed
- 更新 README 截图为最新界面。

## [0.4.0] — 2026-07-04
### 修复 / Fixed
- 修复**添加文件即闪退**的严重问题:文件行模板中两处只读属性(文件大小)被 `Run` 默认的 TwoWay 绑定劫持，导致模板实例化时抛出未捕获异常。
- 修复浅色主题下按钮/链接文字被全局 `TextBlock` 样式染成深色、在强调色底上看不清的问题（"浏览""开始转换"等）。

### 新增 / Added
- 全局未捕获异常兜底：出错时写入 `logs/crash-*.log` 并弹窗提示，而非静默退出。
- 空文件列表时展示大号引导拖拽区（图标 + 说明 + 浏览按钮），有文件后恢复统计与列表。
- 文件列表新增「清空」入口。
- 新增「保留原始文件日期」开关（默认开启）：转换后输出文件沿用源文件的创建/修改时间。

### 变更 / Changed
- 字体统一为系统字体栈 `Segoe UI, Microsoft YaHei UI`。
- 建立统一字号尺阶（Caption 11 / Small 12 / Body 13 / Title 18 / Display 22），替换原先散乱的十余种字号。
- 优化浅色主题各层明度与边线，页眉/主体/页脚过渡更协调一致。
- 顶部成果区「已完成 / 已节省空间」两列改为网格对齐，基准线一致。

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

[Unreleased]: https://github.com/dosheda/ImageConverter/compare/v0.4.2...HEAD
[0.4.2]: https://github.com/dosheda/ImageConverter/releases/tag/v0.4.2
[0.4.1]: https://github.com/dosheda/ImageConverter/releases/tag/v0.4.1
[0.4.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.4.0
[0.3.2]: https://github.com/dosheda/ImageConverter/releases/tag/v0.3.2
[0.3.1]: https://github.com/dosheda/ImageConverter/releases/tag/v0.3.1
[0.3.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.3.0
[0.2.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.2.0
[0.1.0]: https://github.com/dosheda/ImageConverter/releases/tag/v0.1.0
