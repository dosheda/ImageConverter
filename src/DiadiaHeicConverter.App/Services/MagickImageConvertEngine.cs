using DiadiaHeicConverter.App.Models;
using ImageMagick;

namespace DiadiaHeicConverter.App.Services;

public sealed class MagickImageConvertEngine(IOutputPathService outputPathService) : IImageConvertEngine
{
    public string EngineName => "Magick.NET";

    public async Task<ConversionResult> ConvertAsync(
        ConversionTaskItem item,
        AppSettings settings,
        PhotoMetadata metadata,
        CancellationToken cancellationToken)
    {
        var finalPath = item.OutputPath;
        var temporaryPath = outputPathService.CreateTemporaryPath(finalPath);

        try
        {
            await Task.Run(
                () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputDirectory = Path.GetDirectoryName(finalPath);
                    if (!string.IsNullOrWhiteSpace(outputDirectory))
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }

                    using var image = new MagickImage(item.SourcePath);
                    cancellationToken.ThrowIfCancellationRequested();

                    var outputFormat = ToMagickFormat(settings.OutputFormat);
                    image.AutoOrient();
                    image.Format = outputFormat;
                    if (settings.OutputFormat is OutputFormat.Jpeg or OutputFormat.Webp)
                    {
                        image.Quality = (uint)Math.Clamp(settings.JpegQuality, 1, 100);
                    }

                    ApplyMetadataPolicy(image, settings);
                    image.Write(temporaryPath, outputFormat);

                    cancellationToken.ThrowIfCancellationRequested();
                    File.Move(temporaryPath, finalPath, overwrite: settings.OverwriteExistingFiles);
                },
                cancellationToken);

            return ConversionResult.Success(finalPath);
        }
        catch (OperationCanceledException)
        {
            TryDeleteTemporaryFile(temporaryPath);
            return ConversionResult.Cancelled();
        }
        catch (Exception exception)
        {
            TryDeleteTemporaryFile(temporaryPath);
            return ConversionResult.Failure(UserFriendlyErrorMapper.ToMessage(exception));
        }
    }

    private static void ApplyMetadataPolicy(MagickImage image, AppSettings settings)
    {
        if (!settings.PreserveExif)
        {
            image.Strip();
            return;
        }

        if (settings.PreserveGps)
        {
            return;
        }

        var exif = image.GetExifProfile();
        if (exif is null)
        {
            return;
        }

        RemoveGpsValues(exif);
        image.SetProfile(exif);
    }

    private static MagickFormat ToMagickFormat(OutputFormat outputFormat)
    {
        return outputFormat switch
        {
            OutputFormat.Png => MagickFormat.Png,
            OutputFormat.Webp => MagickFormat.WebP,
            OutputFormat.Bmp => MagickFormat.Bmp,
            OutputFormat.Tiff => MagickFormat.Tiff,
            _ => MagickFormat.Jpeg
        };
    }

    private static void RemoveGpsValues(IExifProfile exif)
    {
        exif.RemoveValue(ExifTag.GPSAltitudeRef);
        exif.RemoveValue(ExifTag.GPSVersionID);
        exif.RemoveValue(ExifTag.GPSIFDOffset);
        exif.RemoveValue(ExifTag.GPSAltitude);
        exif.RemoveValue(ExifTag.GPSDestBearing);
        exif.RemoveValue(ExifTag.GPSDestDistance);
        exif.RemoveValue(ExifTag.GPSImgDirection);
        exif.RemoveValue(ExifTag.GPSDOP);
        exif.RemoveValue(ExifTag.GPSSpeed);
        exif.RemoveValue(ExifTag.GPSTrack);
        exif.RemoveValue(ExifTag.GPSDestLatitude);
        exif.RemoveValue(ExifTag.GPSDestLongitude);
        exif.RemoveValue(ExifTag.GPSLatitude);
        exif.RemoveValue(ExifTag.GPSLongitude);
        exif.RemoveValue(ExifTag.GPSTimestamp);
        exif.RemoveValue(ExifTag.GPSDifferential);
        exif.RemoveValue(ExifTag.GPSDateStamp);
        exif.RemoveValue(ExifTag.GPSDestBearingRef);
        exif.RemoveValue(ExifTag.GPSDestDistanceRef);
        exif.RemoveValue(ExifTag.GPSDestLatitudeRef);
        exif.RemoveValue(ExifTag.GPSDestLongitudeRef);
        exif.RemoveValue(ExifTag.GPSImgDirectionRef);
        exif.RemoveValue(ExifTag.GPSLatitudeRef);
        exif.RemoveValue(ExifTag.GPSLongitudeRef);
        exif.RemoveValue(ExifTag.GPSMapDatum);
        exif.RemoveValue(ExifTag.GPSMeasureMode);
        exif.RemoveValue(ExifTag.GPSSatellites);
        exif.RemoveValue(ExifTag.GPSSpeedRef);
        exif.RemoveValue(ExifTag.GPSStatus);
        exif.RemoveValue(ExifTag.GPSTrackRef);
        exif.RemoveValue(ExifTag.GPSProcessingMethod);
        exif.RemoveValue(ExifTag.GPSAreaInformation);
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch
        {
            // Cleanup should never mask the real conversion error.
        }
    }
}
