namespace DiadiaHeicConverter.App.Models;

public static class OutputFormatExtensions
{
    public static string ToFileExtension(this OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Png => ".png",
            OutputFormat.Webp => ".webp",
            OutputFormat.Bmp => ".bmp",
            OutputFormat.Tiff => ".tiff",
            _ => ".jpg"
        };
    }

    public static string ToLogName(this OutputFormat format)
    {
        return format switch
        {
            OutputFormat.Png => "PNG",
            OutputFormat.Webp => "WebP",
            OutputFormat.Bmp => "BMP",
            OutputFormat.Tiff => "TIFF",
            _ => "JPG"
        };
    }
}
