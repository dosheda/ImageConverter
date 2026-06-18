namespace DiadiaHeicConverter.App.Models;

public static class InputImageFormatExtensions
{
    public static InputImageFormat FromPath(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" => InputImageFormat.Jpeg,
            ".jpeg" => InputImageFormat.Jpeg,
            ".png" => InputImageFormat.Png,
            ".webp" => InputImageFormat.Webp,
            ".heic" => InputImageFormat.Heic,
            ".heif" => InputImageFormat.Heif,
            ".bmp" => InputImageFormat.Bmp,
            ".tif" => InputImageFormat.Tiff,
            ".tiff" => InputImageFormat.Tiff,
            _ => InputImageFormat.Unknown
        };
    }
}
