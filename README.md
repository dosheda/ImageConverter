# Diadia Image Converter

Diadia Image Converter 是一个 Windows 桌面批量图片格式转换工具，支持把常见图片转换为 JPG、PNG、WebP、BMP 或 TIFF。软件以保护原文件为第一原则：不会删除原始图片，默认不会覆盖已有输出文件。

## 功能列表

- 支持拖拽单个图片文件或整个文件夹。
- 支持输入格式：`.jpg` / `.jpeg` / `.png` / `.webp` / `.heic` / `.heif` / `.bmp` / `.tif` / `.tiff`。
- 文件夹中的其他文件类型会自动忽略，并在底部提示已忽略的不支持文件数量。
- 支持输出格式：JPG、PNG、WebP、BMP、TIFF；默认 JPG。
- 允许同格式转换，例如 WebP 转 WebP、JPG 转 JPG，可用于压缩或重新编码。
- 拖入文件夹时递归扫描子目录。
- 文件列表显示原始路径、输出路径、文件大小、状态和失败原因。
- 支持选择输出目录、命名规则、输出格式、JPG/WebP 质量、EXIF/GPS、目录结构和覆盖选项。
- 支持开始转换、取消转换、清空列表。
- 取消后再次开始会继续未完成任务，已经成功的文件不会重复转换。
- 转换在后台执行，避免 UI 卡死。
- 默认不覆盖已有输出文件；重名时自动生成 `_1`、`_2` 后缀。
- 写入输出文件时先写临时文件，成功后再移动到最终路径。
- 支持保留 EXIF，默认不保留 GPS 位置信息。
- 支持界面语言切换：简体中文、繁體中文、English、Čeština、Deutsch、Español、Français、Italiano、日本語、한국어、Polski、Português (Brasil)、Русский、Türkçe。
- 支持三种命名规则：
  - 保留原文件名：`IMG_1234.HEIC -> IMG_1234.jpg`
  - 拍摄日期时间 + 原文件名：`IMG_1234.HEIC -> 2024-08-12_15-32-08_IMG_1234.jpg`
  - 日期 + 原文件名：`IMG_1234.HEIC -> 2024-08-12_IMG_1234.jpg`
- 设置保存到用户目录，下次打开会自动恢复。
- 每次转换在程序目录下的 `logs` 文件夹生成日志。

## 如何运行

当前 MVP 使用 `.NET 9 WPF`，目标系统是 Windows 10 / Windows 11。

```powershell
dotnet build
dotnet run --project .\src\DiadiaHeicConverter.App\DiadiaHeicConverter.App.csproj
```

运行测试：

```powershell
dotnet test
```

发布便携版：

```powershell
dotnet publish .\src\DiadiaHeicConverter.App\DiadiaHeicConverter.App.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\DiadiaImageConverter-win-x64
```

## 如何转换图片

1. 打开 Diadia Image Converter。
2. 把图片文件或包含图片的文件夹拖进窗口上方的拖拽区域。
3. 选择输出目录。
4. 按需要调整输出格式、JPG/WebP 质量、命名规则、EXIF/GPS、目录结构和覆盖选项。
5. 如需切换语言，在设置区选择“语言”。
6. 点击“开始转换”。
7. 转换完成后点击“打开输出文件夹”查看输出图片。

## 常见问题

### 会删除原始图片吗？

不会。软件没有删除原文件的功能。

### 已经存在同名输出文件会怎样？

默认不会覆盖。软件会自动生成新名字，例如 `IMG_1234_1.jpg`、`IMG_1234_2.webp`。只有勾选“允许覆盖已有输出文件”后才会覆盖已有输出文件。若同格式转换的输出路径正好等于源文件路径，软件仍会自动改名，避免破坏原图。

### 同格式转换为什么有用？

同格式转换可用于压缩或重新编码。例如一张很大的 `.webp` 可以重新输出为质量更低、体积更小的 `.webp`；一张 `.jpg` 也可以重新编码为新的 `.jpg`。

### 哪些格式支持质量设置？

当前质量滑块主要作用于 JPG 和 WebP。PNG、BMP、TIFF 暂时不暴露单独压缩参数，质量值会保留在统一设置里，后续可以扩展。

### 为什么转换失败？

常见原因包括文件损坏、HEIC/HEIF 编码暂不支持、文件正在被占用、没有写入权限、路径过长或磁盘空间不足。列表中会显示简短原因，详细信息记录在 `logs` 文件夹。

### 文件夹里有 RAR、MP3、MP4 会怎样？

这些文件会被自动忽略，不会加入转换列表。软件会在底部提示已忽略的不支持文件数量。

### 取消转换后会发生什么？

当前任务会尽快停止，未开始的任务会标记为“已取消”。已经成功生成的图片会保留。再次点击“开始转换”时，软件只会继续处理等待中、失败或已取消的任务，不会重复转换已经成功的文件。

## 已知限制

- 当前仅处理静态图片，不支持 GIF 或动画 WebP 的多帧导出。
- HEIC/HEIF 解码能力依赖 Magick.NET 当前打包的 ImageMagick 能力；部分特殊编码可能无法解码。
- PNG、BMP、TIFF 暂不提供单独压缩参数。
- 目前没有安装包，MVP 优先提供 Windows x64 便携版 zip。
- 亮色/暗色主题结构已预留，MVP 默认启用亮色主题。
- 多语言目前覆盖应用主界面、状态、常见错误提示和转换进度；README 仍以中文为主。

## 依赖说明

- `.NET 9 WPF`：Windows 桌面应用框架。
- `CommunityToolkit.Mvvm`：MVVM 命令和属性通知。
- `Magick.NET-Q16-x64`：图片读取和转换引擎，NuGet 包使用 Apache-2.0 许可证。

HEIC/HEIF 支持通常涉及 ImageMagick、libheif、HEVC/H.265 等组件。HEVC/H.265 在不同地区和用途下可能存在专利或授权风险。正式发布前需要再次确认依赖链、二进制分发方式和目标市场的合规要求。

## 隐私说明

软件离线运行，不上传用户图片，不连接云端服务。图片读取、转换、日志和设置都保存在本机。

日志会记录输入路径、输出路径、成功/失败状态和异常信息。请不要把包含隐私路径的日志公开分享。
