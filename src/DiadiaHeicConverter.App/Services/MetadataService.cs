using System.Globalization;
using DiadiaHeicConverter.App.Models;
using ImageMagick;

namespace DiadiaHeicConverter.App.Services;

public sealed class MetadataService : IMetadataService
{
    public Task<PhotoMetadata> ReadAsync(string sourcePath, CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileInfo = new FileInfo(sourcePath);
                var fallback = PhotoMetadata.FromFile(fileInfo);

                try
                {
                    using var image = new MagickImage(sourcePath);
                    cancellationToken.ThrowIfCancellationRequested();

                    var profile = image.GetExifProfile();
                    if (profile is null)
                    {
                        return fallback;
                    }

                    return new PhotoMetadata
                    {
                        DateTimeOriginal = ReadExifDate(profile, ExifTag.DateTimeOriginal),
                        CreateDate = ReadExifDate(profile, ExifTag.DateTimeDigitized)
                            ?? ReadExifDate(profile, ExifTag.DateTime),
                        FileCreatedAt = fallback.FileCreatedAt,
                        FileModifiedAt = fallback.FileModifiedAt,
                        HasGps = HasGps(profile)
                    };
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    return PhotoMetadata.FromFile(fileInfo, UserFriendlyErrorMapper.ToMessage(exception));
                }
            },
            cancellationToken);
    }

    private static DateTime? ReadExifDate(IExifProfile profile, ExifTag<string> tag)
    {
        var value = profile.GetValue(tag)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(
                value,
                "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var parsed))
        {
            return parsed;
        }

        return DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out parsed)
            ? parsed
            : null;
    }

    private static bool HasGps(IExifProfile profile)
    {
        return profile.GetValue(ExifTag.GPSLatitude) is not null
            || profile.GetValue(ExifTag.GPSLongitude) is not null
            || profile.GetValue(ExifTag.GPSLatitudeRef) is not null
            || profile.GetValue(ExifTag.GPSLongitudeRef) is not null;
    }
}
