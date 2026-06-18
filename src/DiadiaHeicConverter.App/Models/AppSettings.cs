namespace DiadiaHeicConverter.App.Models;

public sealed class AppSettings
{
    public string OutputDirectory { get; set; } = GetDefaultOutputDirectory();

    public int JpegQuality { get; set; } = 92;

    public OutputFormat OutputFormat { get; set; } = OutputFormat.Jpeg;

    public NamingRule NamingRule { get; set; } = NamingRule.OriginalFileName;

    public bool PreserveExif { get; set; } = true;

    public bool PreserveGps { get; set; }

    public bool PreserveDirectoryStructure { get; set; }

    public bool OverwriteExistingFiles { get; set; }

    public string Theme { get; set; } = "Light";

    public string LanguageCode { get; set; } = "zh-Hans";

    public AppSettings Normalized()
    {
        return new AppSettings
        {
            OutputDirectory = string.IsNullOrWhiteSpace(OutputDirectory) ? GetDefaultOutputDirectory() : OutputDirectory,
            JpegQuality = Math.Clamp(JpegQuality, 1, 100),
            OutputFormat = Enum.IsDefined(OutputFormat) ? OutputFormat : OutputFormat.Jpeg,
            NamingRule = NamingRule,
            PreserveExif = PreserveExif,
            PreserveGps = PreserveGps,
            PreserveDirectoryStructure = PreserveDirectoryStructure,
            OverwriteExistingFiles = OverwriteExistingFiles,
            Theme = string.IsNullOrWhiteSpace(Theme) ? "Light" : Theme,
            LanguageCode = string.IsNullOrWhiteSpace(LanguageCode) ? "zh-Hans" : LanguageCode
        };
    }

    public static string GetDefaultOutputDirectory()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        if (string.IsNullOrWhiteSpace(pictures))
        {
            pictures = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(pictures, "Diadia Image Converter Output");
    }
}
