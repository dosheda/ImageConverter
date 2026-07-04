# Diadia Image Converter — v0.3 UI 重构开发文档（交给 Codex 执行）

> 本文件是一份自包含的实现规格。执行者（Codex）拥有仓库读写权限，但**没有**设计讨论的上下文，所以下面把背景、现状、目标、逐文件改动、验收标准都写全。请**严格按本文件**实现，视觉以 `docs/design/inspector-mockup.html`（可用浏览器打开）为像素参照。

---

## 0. TL;DR（要做什么）

把主界面从"单栏卡片流"重构为**专业工具的左右分栏 Inspector 布局**：

- **左**：内容区 = 顶部「成果」统计（完成进度环 + 已节省空间）+ 拖拽细条 + 文件列表（卡片行，含 `原大小 → 新大小 · −xx%`）。
- **右**：**唯一**的设置检查器（Inspector），从上到下：输出格式 / 质量 / 命名 / 输出位置 / 元数据 / 外观（主题 + 语言）/ 关于。**所有可配置项集中一处、始终可见**，不再有抽屉或折叠展开。
- **下**：固定操作条 = 计数 + 进度 + `打开输出文件夹` + 主按钮 `开始转换 N 张`。
- **新交互**：文件行支持「打开转换后的图片」（行内按钮 / 双击 / 右键菜单），右键还可「在文件夹中显示 / 复制输出路径 / 打开源文件位置」；失败行支持「重试」。
- **新数据**：转换后记录输出文件大小，计算并展示每文件压缩比与整体已节省空间。

设计参照：`docs/design/inspector-mockup.html`（推荐方案）。`docs/design/drawer-mockup-rejected.html` 是被否决的旧方案，仅作对比，**不要照它做**。

---

## 1. 项目概览与架构（现状）

- **技术栈**：C# / .NET 9 / WPF / MVVM（`CommunityToolkit.Mvvm`）/ 图片引擎 `Magick.NET-Q16-x64`。仅 Windows 10/11。
- **解决方案**：`DiadiaHeicConverter.sln`。主项目 `src/DiadiaHeicConverter.App/`，测试 `tests/DiadiaHeicConverter.Tests/`（xUnit，37 个测试，必须保持全绿）。
- **构建/测试/运行**：
  ```powershell
  dotnet build DiadiaHeicConverter.sln -c Debug
  dotnet test  DiadiaHeicConverter.sln -c Debug
  dotnet run --project .\src\DiadiaHeicConverter.App\DiadiaHeicConverter.App.csproj
  ```
- **依赖注入**：**没有 DI 容器**。所有 service 在 `Views/MainWindow.xaml.cs` 构造函数里手动 `new` 出来，再注入 `MainViewModel`。新增 service 就在这里 new。
- **分层**：`Models/`、`Services/`（每个 service 都是 `IXxx` 接口 + `Xxx` 实现成对）、`ViewModels/`、`Views/`、`Resources/`、`Converters/`。

### 关键现有类型（实现时会用到）

| 类型 | 位置 | 说明 |
|---|---|---|
| `MainViewModel` | `ViewModels/MainViewModel.cs` | 主 VM。构造签名（7 参）见下。持有 `ObservableCollection<ConversionItemViewModel> Items`，命令 `StartConversionCommand / CancelConversionCommand / ClearListCommand / BrowseOutputDirectoryCommand / OpenOutputDirectoryCommand / ToggleThemeCommand`，属性 `OutputDirectory / JpegQuality / SelectedOutputFormat / SelectedNamingRule / SelectedLanguageCode / PreserveExif / PreserveGps / PreserveDirectoryStructure / OverwriteExistingFiles / IsConverting / ProgressValue / ProgressMaximum / CurrentMessage / TotalCount / SucceededCount / FailedCount / SkippedCount / AppVersion / IsDarkTheme / ThemeToggleGlyph`，以及 `OutputFormatOptions / NamingRuleOptions / LanguageOptions`。 |
| `ConversionItemViewModel` | `ViewModels/ConversionItemViewModel.cs` | 包装 `ConversionTaskItem Model`，有 `SourcePath / OutputPath / FileSizeDisplay / StatusText / FailureReason / Status`，方法 `RefreshFromModel() / RefreshLocalization()`。 |
| `ConversionTaskItem` | `Models/ConversionTaskItem.cs` | `SourcePath / OutputPath / FileSizeBytes / Status / FailureReason / InputFormat`。 |
| `ImageConvertService` | `Services/ImageConvertService.cs` | `ConvertAsync(items, settings, progress, ct)`；逐个调用引擎，设置每个 item 的 `Status/FailureReason`，回报 `IProgress<ConversionProgress>`。 |
| `MagickImageConvertEngine` | `Services/MagickImageConvertEngine.cs` | 真正的解码/编码/落盘（先写临时文件再 `File.Move`），返回 `ConversionResult`。 |
| `ThemeService` / `IThemeService` | `Services/ThemeService.cs` | `NormalizeTheme(string) / ApplyTheme(string)`，运行时替换 `Resources/Themes/*.xaml`。 |
| `LocalizationService` | `Services/LocalizationService.cs` | `ApplyLanguage(code)`，替换 `Resources/Languages/Strings.*.xaml`。支持 14 种语言。 |
| `AppStrings` | `Resources/AppStrings.cs` | `Get(key)` / `Format(key,args)`，带 fallback 字典。 |
| `AppSettings` | `Models/AppSettings.cs` | 已含 `Theme`（"Light"/"Dark"）与所有转换设置，`Normalized()` 做规范化，JSON 持久化。 |
| `StatusToBrushConverter` | `Converters/StatusToBrushConverter.cs` | 按 `ConversionStatus` 给状态文字上色。 |

`MainViewModel` 当前构造签名（新增 service 时在此加参数，并同步改 `MainWindow.xaml.cs` 与测试里的 5 处构造）：
```csharp
public MainViewModel(
    IFileScannerService fileScannerService,
    IImageConvertService imageConvertService,
    IOutputPathService outputPathService,
    ISettingsService settingsService,
    IDialogService dialogService,
    ILocalizationService localizationService,
    IThemeService themeService)
```

---

## 2. 设计系统 / 主题资源（已就绪，直接复用）

`Resources/Themes/Light.xaml` 与 `Dark.xaml` 已定义完整语义化画刷，两套键名一致、可运行时切换。**新 UI 一律用 `DynamicResource` 引用这些键，不要写死颜色**。可用键（节选）：

```
WindowBackgroundBrush PanelBackgroundBrush ElevatedBackgroundBrush SubtleBackgroundBrush
TextBrush MutedTextBrush SubtleTextBrush BorderBrush DividerBrush
AccentBrush AccentHoverBrush AccentPressedBrush AccentSoftBrush AccentForegroundBrush
DropZoneBackgroundBrush DropZoneBorderBrush
InputBackgroundBrush InputBorderBrush InputHoverBorderBrush
ButtonBackgroundBrush ButtonHoverBackgroundBrush ButtonPressedBackgroundBrush ButtonBorderBrush ButtonForegroundBrush
DisabledBackgroundBrush DisabledForegroundBrush
GridHeaderBackgroundBrush GridRowHoverBrush GridRowSelectedBrush
TrackBrush ThumbBrush ScrollThumbBrush ScrollThumbHoverBrush
```

`Resources/Styles.xaml` 已有控件模板与命名样式：隐式 `Button/TextBox/ComboBox/ComboBoxItem/CheckBox/Slider/ProgressBar/DataGrid/DataGridColumnHeader/DataGridRow/DataGridCell/ScrollBar`，以及 `x:Key` 样式 `Card`（卡片容器）、`PrimaryButton`（主色按钮）、`IconButton`。

**本次需要新增的样式**（放进 `Styles.xaml`）：
- `SegmentedControl` 风格（主题三段：浅色/深色/跟随系统）——用 `ListBox` + `ItemContainerStyle` 或一组 `RadioButton` 实现分段按钮，选中项 `AccentBrush` 填充。
- `FormatPill` 风格（输出格式的胶囊按钮，可选中态）——同上，用可单选的 `ToggleButton`/`RadioButton`。
- `RowActionButton` 风格（文件行内的小 ghost 按钮"打开"/"重试"，默认低透明度、悬停变 accent）。
- 语义色（成功/失败/节省）——列表里"−63%"用节省色。可在两套主题各加：`SuccessBrush(#16A34A / #4ADE80)`、`DangerBrush(#DC2626 / #F87171)`、`SaveBrush(#0EA5A0 / #2DD4BF)` 及其柔和背景 `*SoftBrush`。**注意也要在 `StatusToBrushConverter` 之外的地方统一使用**。

配色 hex 以 `inspector-mockup.html` 内 `.window`（light）与 `.window[data-theme="dark"]`（dark）的 CSS 变量为准，已与现有主题对齐。

---

## 3. 目标界面规格（Inspector 布局）

以 `docs/design/inspector-mockup.html` 为准。整体是一个 `Grid`：

```
Row0  标题（沿用系统标题栏，Window.Icon 已设置）
Row1  Header 条：◆ 品牌 + 标题 + v{AppVersion} 徽标 + 副标题（左），? 帮助（右，可选）
Row2* Body：两列 —— 左 Content(*) | 右 Inspector(固定 ~336px)
Row3  Footer 操作条
```

### 3.1 左侧 Content

从上到下：

1. **成果统计条 `Stats`**（`IsConverting` 或 `Items.Count>0` 时显示）：
   - 完成进度**环**：`已完成/总数` 百分比（用 `conic-gradient` 等价物；WPF 可用一个 `Border` + `Path`/`Arc`，或简单用一个圆环图。可接受用两层 `Ellipse` + 一个 `Path` 扇形；实现难度高的话，先用一个粗描边 `ProgressBar` 变体或圆形进度控件替代，但优先做环）。中间显示百分比数字。
   - `已完成 3 / 6`
   - `已节省空间 5.1 MB · −62%` + 一条节省色小进度条（宽度=平均压缩比）。
2. **拖拽细条 `AddBar`**：虚线圆角条，`⬆ 拖入更多图片或文件夹` + 右侧 `浏览…`。整窗和该条都要 `AllowDrop`，复用现有 `OnDragOver/OnDrop`（在 `MainWindow.xaml.cs`）。
   - （可选增强）列表为空时，这个区域放大成**英雄拖拽区**占据左侧主要空间；一旦有文件则收缩为细条、列表接管。用 `BoolToVisibilityConverter` + `Items.Count` 触发。第一版可先只做细条，英雄空状态列为 P3。
3. **文件列表 `List`**：`ItemsControl`（或保留 `DataGrid` 但重排为单列模板）。**推荐用 `ItemsControl` + 自定义 `DataTemplate` 做"卡片行"**，因为要显示缩进的多元素、右侧多按钮，`DataGrid` 不适合。每行：
   - 左：**格式标签**方块（`HEIC/PNG/WEBP/...`，按输入格式上色，40×40 圆角）。
   - 中：第一行 `目标文件名 ← 源文件名`（源名 muted）；第二行 源目录路径（subtle，省略号截断）。
   - 右：**大小列** `3.2 MB → 1.2 MB` 上、`−63%`（节省色）下；未完成显示 `4.0 MB · 待转换`，失败显示 `无法读取`（危险色）。
   - 最右：**状态胶囊**（圆角 pill + 圆点：成功=绿 / 等待中=灰 / 失败=红 / 转换中=蓝）+ **行内动作**（见 §4）。
   - 悬停整行有 `GridRowHoverBrush` 背景。

### 3.2 右侧 Inspector（唯一设置处）

一个竖向滚动面板，分节（每节标题小写字号 + 字距、`SubtleTextBrush`）：

1. **输出**
   - `格式`：**胶囊单选** `JPG / PNG / WebP / BMP / TIFF`，绑定 `OutputFormatOptions` + `SelectedOutputFormat`（不再用下拉框）。
   - `JPG / WebP 质量 {值}`：滑块，绑定 `JpegQuality`，标题右侧显示当前值（accent 色）。
2. **命名与位置**
   - `命名规则`：下拉（复用 `ComboBox`），绑定 `NamingRuleOptions` + `SelectedNamingRule`。
   - `输出到`：路径框（只读展示 `OutputDirectory`）+ `浏览` 按钮（`BrowseOutputDirectoryCommand`）。
3. **元数据**：四个复选框 `保留 EXIF / 保留 GPS / 保留原目录结构 / 允许覆盖已有输出文件`，绑定对应布尔属性。
4. **外观与语言**
   - `主题`：**分段控件** `浅色 / 深色 / 跟随系统`。见 §5。
   - `语言`：下拉，绑定 `LanguageOptions` + `SelectedLanguageCode`。
5. **关于**：应用图标 + 名称 + `v{AppVersion} · MIT · 离线运行`；链接 `GitHub / 检查更新 / 开源许可`（`GitHub` → 打开 https://github.com/dosheda/ImageConverter）。

### 3.3 Footer 操作条

- 左：计数 `共 N · 成功 g · 等待 w · 失败 r`（成功用绿、失败用红、数字 tabular）。
- 中：全局进度条（`ProgressValue/ProgressMaximum`），仅 `IsConverting` 时明显。
- 右：`📁 打开输出文件夹`（ghost，`OpenOutputDirectoryCommand`）+ 主按钮 `开始转换 {可运行数} 张`（`PrimaryButton`，`StartConversionCommand`）。运行中主按钮变 `取消`（`CancelConversionCommand`），或并列显示取消。
- **完成态**：一批全部结束后，`CurrentMessage` 区域/footer 左侧显示 `✓ 全部完成`，主按钮可禁用或变"再次转换"。

---

## 4. 交互规格：打开 / 定位 / 完成

### 4.1 新增 service：文件启动

新增 `Services/IFileLauncherService.cs` + `Services/FileLauncherService.cs`（Windows 实现）：

```csharp
public interface IFileLauncherService
{
    void OpenFile(string path);        // 用默认程序打开文件
    void RevealInExplorer(string path);// 资源管理器中定位并选中该文件
    void OpenFolder(string path);      // 打开某目录
}
```
实现要点：
- `OpenFile`：`Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true })`。文件不存在时静默/给出 `CurrentMessage` 提示，不抛未捕获异常。
- `RevealInExplorer`：`Process.Start("explorer.exe", $"/select,\"{path}\"")`。
- `OpenFolder`：`Process.Start(new ProcessStartInfo { FileName="explorer.exe", Arguments=$"\"{dir}\"", UseShellExecute=true })`（可把 `MainViewModel.OpenOutputDirectory` 现有逻辑迁进来复用）。
- 全部包 try/catch，失败写日志（`ILogService`）并设 `CurrentMessage`。

在 `MainWindow.xaml.cs` new 出来，注入需要它的 VM。

### 4.2 行内 / 双击 / 右键

`ConversionItemViewModel` 增加对 `IFileLauncherService` 的引用（构造注入），暴露命令：
- `OpenFileCommand`：`CanExecute = Status==Succeeded && File.Exists(OutputPath)`，执行 `launcher.OpenFile(Model.OutputPath)`。
- `RevealCommand`：同上，`launcher.RevealInExplorer(Model.OutputPath)`。
- `CopyOutputPathCommand`：`Clipboard.SetText(OutputPath)`。
- `OpenSourceFolderCommand`：`launcher.RevealInExplorer(Model.SourcePath)`。
- `RetryCommand`：仅失败可用；回调 `MainViewModel` 重跑该单个 item（把状态置 `Pending` 后触发一次转换）。为避免 VM 相互依赖，可用一个 `Action<ConversionItemViewModel>` 回调或事件，由 `MainViewModel` 在创建 item VM 时注入。

XAML 绑定：
- 行内 `打开` 按钮（`RowActionButton` 风格）→ `OpenFileCommand`，仅成功行可见（`Status` 转 Visibility，或按钮 `IsEnabled`/透明度）。
- 失败行 `重试` 按钮 → `RetryCommand`。
- **双击整行** → `OpenFileCommand`：在行模板根元素上加 `InputBindings`：`<MouseBinding MouseAction="LeftDoubleClick" Command="{Binding OpenFileCommand}"/>`。
- **右键菜单**：行模板根元素 `ContextMenu`，项：`打开 / 在文件夹中显示 / 复制输出路径 / 打开源文件位置`，分别绑上面四个命令。成功才启用打开/定位/复制。

> 注意：`打开输出文件夹`（整目录）保留在 footer；**行内不再放"位置"按钮**（与之重复），"在文件夹中显示"只进右键菜单。

---

## 5. 主题分段控件（Light / Dark / System）

现状：只有 `Light/Dark` 双态 + 一个图标 toggle。改为三段 `浅色 / 深色 / 跟随系统`，放进 Inspector「外观」。

改动：
- `AppSettings.Theme` 取值扩展为 `"Light" | "Dark" | "System"`；`Normalized()` 允许这三种，默认 `"Light"`。
- `ThemeService`：`NormalizeTheme` 支持 `"System"`；新增 `ResolveEffectiveTheme(string)`：`System` 时读系统主题（读注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize` 的 `AppsUseLightTheme` DWORD，1=Light,0=Dark）。`ApplyTheme` 对 `System` 应用解析后的实际主题。
- `MainViewModel`：把现有 `ToggleThemeCommand`/`IsDarkTheme`/`ThemeToggleGlyph` 替换为 `SelectedTheme`（string，绑定分段控件）与三个只读布尔 `IsLight/IsDark/IsSystem`（供分段选中态）。`SelectedTheme` setter 里 `ApplyTheme` + 持久化 + `PersistSettings`。`CaptureSettings()` 里 `Theme = SelectedTheme`。
- Header 右上角原 `IconButton` 主题切换**移除**（改由 Inspector 分段控件负责，主题从此"一眼可见"）。

---

## 6. 数据模型改动：输出大小 & 已节省空间

目的：列表显示 `原→新 · −xx%`，顶部显示"已节省空间"。

1. `Models/ConversionTaskItem.cs`：新增 `public long? OutputSizeBytes { get; set; }`。
2. `Services/ImageConvertService.cs`：某 item 转换**成功后**，读取输出文件大小并写回：
   ```csharp
   item.OutputSizeBytes = new FileInfo(item.OutputPath).Length;
   ```
   （在把 `Status` 设为 `Succeeded` 的同一处；读失败就留 `null`，不要抛出。）
3. `ConversionItemViewModel`：新增只读展示属性：
   - `OutputSizeDisplay`（如 `1.2 MB`；`null` 时空）。
   - `SizeChangeDisplay`（如 `3.2 MB → 1.2 MB`；未完成时 `{原大小} · 待转换`；失败时空/`无法读取`）。
   - `ReductionDisplay`（如 `−63%`，= 1 - out/in，四舍五入；仅成功且 out<in 时显示；负数或增大时显示 `+x%` 或留空）。
   - 复用现有 `FileSizeBytes`（源）+ 新 `OutputSizeBytes`。大小格式化可抽一个 helper（KB/MB/GB）。`RefreshFromModel()` 里 `OnPropertyChanged` 这些新属性。
4. `MainViewModel`：新增聚合只读属性（在计数刷新时一并 `OnPropertyChanged`）：
   - `CompletedCount`（= `SucceededCount`）、`CompletionRatio`（double 0..1 = 已结束/总数，用于进度环）。
   - `SavedBytes` = Σ(源 − 输出) 仅成功且有 OutputSizeBytes 者；`SavedDisplay`（格式化）。
   - `AverageReductionDisplay`（成功项平均压缩比，如 `−62%`）。
   - 在 `RefreshCounts()` 里刷新它们。

---

## 7. 需要新增 / 修改的文件清单

**新增**
- `src/DiadiaHeicConverter.App/Services/IFileLauncherService.cs`
- `src/DiadiaHeicConverter.App/Services/FileLauncherService.cs`
- （可选）`src/DiadiaHeicConverter.App/Converters/` 下若干转换器：`SizeToDisplayConverter`、`StatusToVisibilityConverter`、`RatioToAngle/DashConverter`（画进度环用）。
- （可选）`CHANGELOG.md`（见 §9 P0）。

**修改**
- `Views/MainWindow.xaml`：整体重排为 Inspector 布局（本文件 §3）。
- `Views/MainWindow.xaml.cs`：new `FileLauncherService`；注入 VM；保留拖拽处理。
- `ViewModels/MainViewModel.cs`：主题分段（§5）、聚合统计（§6）、item VM 注入 launcher + retry 回调。
- `ViewModels/ConversionItemViewModel.cs`：打开/定位/复制/重试命令 + 大小/压缩比展示属性。
- `Models/ConversionTaskItem.cs`：`OutputSizeBytes`。
- `Models/AppSettings.cs`：`Theme` 支持 `System`。
- `Services/ImageConvertService.cs`：成功后写 `OutputSizeBytes`。
- `Services/ThemeService.cs` / `IThemeService.cs`：`System` 解析。
- `Resources/Styles.xaml`：新增 `SegmentedControl / FormatPill / RowActionButton` 及语义色样式。
- `Resources/Themes/Light.xaml` & `Dark.xaml`：新增 `SuccessBrush/DangerBrush/SaveBrush` 等语义色键。
- `Resources/AppStrings.cs` + `Resources/Languages/Strings.*.xaml`：新增字符串键（§8）。
- 测试：`tests/.../MainViewModelResumeTests.cs` 若 VM 构造签名变化需同步（当前 5 处构造）；为新逻辑补测试（§10）。

---

## 8. 需要新增的本地化字符串键

所有面向用户的新文字**必须**走资源系统（项目规则：字符串集中、可本地化）。为每个键：
1. 加进 `Resources/AppStrings.cs` 的 fallback 字典（中文兜底）。
2. 加进 `Resources/Languages/Strings.zh-CN.xaml` 与 `Strings.en-US.xaml`。
3. **其余 12 个语言文件**（`cs, de, es, fr, it, ja, ko, pl, pt-BR, ru, tr, zh-Hant`；注意 `zh-Hans` 转发到 `zh-CN`、`ja` 有 `ja`/`ja-JP` 别名）至少补齐 key（可暂用英文占位，后续翻译），避免缺键回退到 key 名。

建议新增键：
```
SectionOutput           输出            Output
SectionNaming           命名与位置       Naming & location
SectionMetadata         元数据          Metadata
SectionAppearance       外观与语言       Appearance & language
SectionAbout            关于            About
OutputToLabel           输出到          Save to
ThemeLabel              主题            Theme
ThemeLight              浅色            Light
ThemeDark               深色            Dark
ThemeSystem             跟随系统         System
StatCompleted           已完成          Completed
StatSpaceSaved          已节省空间       Space saved
SizePendingSuffix       待转换          pending
SizeUnreadable          无法读取         unreadable
RowOpen                 打开            Open
RowReveal               在文件夹中显示    Show in folder
RowCopyPath             复制输出路径      Copy output path
RowOpenSource           打开源文件位置    Open source location
RowRetry                重试            Retry
AllDone                 全部完成         All done
LinkGitHub              GitHub          GitHub
LinkCheckUpdate         检查更新         Check for updates
LinkLicenses            开源许可         Licenses
StartButtonWithCountFmt 开始转换 {0} 张   Convert {0} images
ReductionFmt            −{0}%           −{0}%
```
（`AppTitle / DropSubtitle / OutputFormatLabel / NamingRuleLabel / LanguageLabel / OpenOutputButtonText / Preserve*Text / OverwriteText / Column* / Status* / Summary*` 等已存在，直接复用。）

---

## 9. 分阶段任务（建议顺序）

### P0 — 收尾现有小事（先做，快）
- 工作区已有一处**未提交**的 `README.md` 改动：在徽章区加了 `Release` 与 `Downloads` 两个 shields 徽章。**保留它**。
- 新建 `CHANGELOG.md`（Keep a Changelog 风格），至少两条：
  - `v0.2.0 — 2026-07-04`：UI 重做、暗色主题+切换、新图标、版本号、CI/Release 自动化、MIT、双语 README。
  - `v0.1.0 — 2026-06-18`：首个版本（批量转换、14 语言、EXIF/GPS 处理、续跑、安全写入）。
- 提交并推送（作者见 §11）。

### P1 — Inspector 布局骨架（本次重点）
- 新增 `SegmentedControl / FormatPill / RowActionButton` 样式与语义色。
- 重排 `MainWindow.xaml` 为 左Content / 右Inspector / 下Footer；格式改胶囊、主题改分段、设置全部收进 Inspector。
- 主题三段（§5）。
- 视觉对齐 `docs/design/inspector-mockup.html`。
- **验收**：编译零错误零警告、37 测试全绿、亮/暗/跟随系统三态可切、所有设置在右栏一处可见、拖拽仍工作。

### P2 — 数据与打开交互
- `OutputSizeBytes` + 压缩比展示 + 顶部「成果」统计（进度环 + 已节省空间）。
- `FileLauncherService` + 行内打开 / 双击 / 右键菜单 / 失败重试。
- footer 完成态。

### P3 — 打磨（可后续版本）
- 空列表英雄拖拽态；拖入时整窗高亮反馈；主题切换过渡；键盘（Enter 开始、Del 删除选中行）；Toast 反馈替代状态行；缩略图（注意大批量内存/性能，做成异步+可关）。

---

## 10. 验收标准 & 质量门槛

每个阶段结束都必须：
- `dotnet build DiadiaHeicConverter.sln` **零错误零警告**。
- `dotnet test` **全绿**（现有 37 个不许回归；VM 改签名要同步测试构造）。
- 手动或截图验证：亮/暗主题都正确渲染；无 XAML 运行时资源缺失（无 `DynamicResource` 找不到）。
- 遵守项目规则（§11）。

**建议新增测试**：
- 主题 `System` 归一化与持久化。
- `ConversionItemViewModel` 的 `ReductionDisplay/SizeChangeDisplay` 计算（给定源/输出字节，断言文案）。
- `MainViewModel.SavedBytes / CompletionRatio` 聚合。
- `OpenFileCommand.CanExecute` 在非成功状态为 false。
（`FileLauncherService` 真正启动进程的部分可不做单测，抽象成接口后用 fake 验证被调用。）

---

## 11. 必守约束（来自 `AGENTS.md`）与提交规范

- **绝不删除原始图片**；**默认绝不覆盖**用户文件（除非显式勾选）。
- 业务逻辑不放 `MainWindow.xaml.cs`；转换逻辑留在 `Services`。
- 长任务异步、**UI 线程不可阻塞**（打开文件/定位也要避免阻塞；`Process.Start` 本身很快，但包好异常）。
- 所有面向用户字符串集中、可本地化（见 §8）。
- 错误处理覆盖：文件权限、损坏图片、不支持格式、取消、文件被占用、路径过长、磁盘不足。
- 行为变化时更新 `README.md`。

**Git 提交**：本仓库所有历史的作者/committer 统一为 `dosheda <293136185+dosheda@users.noreply.github.com>`（`git config user.name/user.email` 已在本地设为此值）。新提交请保持该作者，**不要**添加其它 Co-Authored-By 尾注。远程 `origin` = https://github.com/dosheda/ImageConverter 。发新版流程：改代码 → 在 `.csproj` 里 bump `<Version>` → 打 `git tag -a vX.Y.Z` 并 `git push origin vX.Y.Z`，Release workflow 会自动编译、注入版本号、打包 `DiadiaImageConverter-<版本>-win-x64.zip` 到 GitHub Releases。

---

## 12. 参照文件
- **像素参照（照这个做）**：`docs/design/inspector-mockup.html`
- 被否决的旧方案（勿照做，仅对比）：`docs/design/drawer-mockup-rejected.html`
- 现有主题色值：`src/DiadiaHeicConverter.App/Resources/Themes/{Light,Dark}.xaml`
- 现有样式：`src/DiadiaHeicConverter.App/Resources/Styles.xaml`
