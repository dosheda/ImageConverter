using System.Windows;
using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Resources;

public static class AppStrings
{
    private static readonly IReadOnlyDictionary<string, string> FallbackStrings = new Dictionary<string, string>
    {
        ["StatusPending"] = "等待中",
        ["StatusConverting"] = "转换中",
        ["StatusSucceeded"] = "成功",
        ["StatusFailed"] = "失败",
        ["StatusSkipped"] = "已跳过",
        ["StatusCancelled"] = "已取消",
        ["ErrorUnsupportedFormat"] = "文件不是支持的图片格式。",
        ["ErrorFileNotFound"] = "找不到源文件。",
        ["ErrorFileInUse"] = "文件可能正在被其他程序占用。",
        ["ErrorPermission"] = "没有读取或写入权限。",
        ["ErrorPathTooLong"] = "文件路径过长。",
        ["ErrorDiskFull"] = "磁盘空间不足或写入失败。",
        ["ErrorCorruptedImage"] = "图片可能已损坏，或 HEIC 编码暂不支持。",
        ["ErrorEngineMissing"] = "转换引擎不可用，请确认依赖文件完整。",
        ["ErrorCancelled"] = "用户取消了转换。",
        ["ErrorUnknown"] = "转换失败，请查看日志了解详情。",
        ["ErrorOutputPathMissing"] = "输出路径不存在，已无法写入文件。",
        ["ErrorOutputFileExists"] = "输出文件已存在，且无法自动生成新文件名。",
        ["FormatJpeg"] = "JPG",
        ["FormatPng"] = "PNG",
        ["FormatWebp"] = "WebP",
        ["FormatBmp"] = "BMP",
        ["FormatTiff"] = "TIFF",
        ["NamingOriginal"] = "保留原文件名",
        ["NamingDateTimeOriginal"] = "拍摄日期时间 + 原文件名",
        ["NamingDateOriginal"] = "日期 + 原文件名",
        ["InitialMessage"] = "拖入 JPG/PNG/WebP/HEIC/HEIF/BMP/TIFF 文件或文件夹开始。",
        ["NoNewFilesMessage"] = "没有发现新的受支持图片文件。",
        ["AddedFilesMessageFormat"] = "已添加 {0} 个文件。",
        ["AddedFilesWithIgnoredMessageFormat"] = "已添加 {0} 个文件，已忽略 {1} 个不支持的文件。",
        ["NoNewFilesWithIgnoredMessageFormat"] = "没有发现新的受支持图片文件，已忽略 {0} 个不支持的文件。",
        ["CompletedMessageFormat"] = "完成：总数 {0}，成功 {1}，失败 {2}，跳过/取消 {3}。",
        ["ConvertingFileMessageFormat"] = "正在转换：{0}",
        ["ProcessedMessageFormat"] = "已处理 {0}/{1}",
        ["CancellingMessage"] = "正在取消，请稍候。",
        ["ListClearedMessage"] = "列表已清空。",
        ["SettingsSaveFailedMessage"] = "设置暂时无法保存，请检查应用数据目录权限。",
        ["LanguageChangedMessage"] = "语言已切换。",
        ["SameFormatHint"] = "同格式转换可用于压缩或重新编码；JPG/WebP 会使用质量设置。",
        ["DialogSelectOutputTitle"] = "选择输出目录",
        ["DialogSelectInputTitle"] = "选择要添加的图片",
        ["ImageFilesFilter"] = "图片文件|*.jpg;*.jpeg;*.png;*.webp;*.heic;*.heif;*.bmp;*.tif;*.tiff|所有文件|*.*",
        ["BrowseInputButtonText"] = "浏览...",
        ["HelpTooltip"] = "帮助",
        ["SectionOutput"] = "输出",
        ["SectionNaming"] = "命名与位置",
        ["SectionMetadata"] = "元数据",
        ["SectionAppearance"] = "外观与语言",
        ["SectionAbout"] = "关于",
        ["OutputToLabel"] = "输出到",
        ["ThemeLabel"] = "主题",
        ["ThemeLight"] = "浅色",
        ["ThemeDark"] = "深色",
        ["ThemeSystem"] = "跟随系统",
        ["StatCompleted"] = "已完成",
        ["StatSpaceSaved"] = "已节省空间",
        ["SizePendingSuffix"] = "待转换",
        ["SizeUnreadable"] = "无法读取",
        ["RowOpen"] = "打开",
        ["RowReveal"] = "在文件夹中显示",
        ["RowCopyPath"] = "复制输出路径",
        ["RowOpenSource"] = "打开源文件位置",
        ["RowRetry"] = "重试",
        ["AllDone"] = "全部完成",
        ["LinkGitHub"] = "GitHub",
        ["LinkCheckUpdate"] = "检查更新",
        ["LinkLicenses"] = "开源许可",
        ["StartButtonWithCountFmt"] = "开始转换 {0} 张 ▶",
        ["ReductionFmt"] = "−{0}%",
        ["AboutMetaSuffix"] = "MIT · 离线运行",
        ["OutputPathCopiedMessage"] = "输出路径已复制。",
        ["RetryingFileMessageFormat"] = "正在重试：{0}",
        ["EmptyStateTitle"] = "拖拽图片或文件夹到这里",
        ["EmptyStateHint"] = "支持 JPG · PNG · WebP · HEIC · HEIF · BMP · TIFF，或整个文件夹",
        ["PreserveFileDateText"] = "保留原始文件日期",
        ["ClearListButtonText"] = "清空"
    };

    public static string Get(string key)
    {
        if (Application.Current?.TryFindResource(key) is string localized)
        {
            return localized;
        }

        return FallbackStrings.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    public static string GetStatusText(ConversionStatus status)
    {
        return status switch
        {
            ConversionStatus.Pending => Get("StatusPending"),
            ConversionStatus.Converting => Get("StatusConverting"),
            ConversionStatus.Succeeded => Get("StatusSucceeded"),
            ConversionStatus.Failed => Get("StatusFailed"),
            ConversionStatus.Skipped => Get("StatusSkipped"),
            ConversionStatus.Cancelled => Get("StatusCancelled"),
            _ => Get("StatusPending")
        };
    }
}
