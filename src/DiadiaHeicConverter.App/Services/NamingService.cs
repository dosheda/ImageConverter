using System.Globalization;
using System.Text;
using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public sealed class NamingService : INamingService
{
    public string CreateBaseFileName(string sourcePath, PhotoMetadata metadata, NamingRule namingRule)
    {
        var originalName = Path.GetFileNameWithoutExtension(sourcePath);
        originalName = SanitizeFileNamePart(originalName);

        var date = metadata.BestDate;
        return namingRule switch
        {
            NamingRule.DateTimeOriginalAndFileName =>
                $"{date:yyyy-MM-dd_HH-mm-ss}_{originalName}",
            NamingRule.DateAndOriginalFileName =>
                $"{date:yyyy-MM-dd}_{originalName}",
            _ => originalName
        };
    }

    private static string SanitizeFileNamePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Normalize(NormalizationForm.FormC))
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        var sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized)
            ? "converted"
            : sanitized;
    }
}
