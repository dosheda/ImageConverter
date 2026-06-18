using System.IO;
using DiadiaHeicConverter.App.Resources;
using ImageMagick;

namespace DiadiaHeicConverter.App.Services;

public static class UserFriendlyErrorMapper
{
    public static string ToMessage(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => AppStrings.Get("ErrorCancelled"),
            FileNotFoundException => AppStrings.Get("ErrorFileNotFound"),
            DirectoryNotFoundException => AppStrings.Get("ErrorOutputPathMissing"),
            UnauthorizedAccessException => AppStrings.Get("ErrorPermission"),
            PathTooLongException => AppStrings.Get("ErrorPathTooLong"),
            DllNotFoundException => AppStrings.Get("ErrorEngineMissing"),
            TypeInitializationException => AppStrings.Get("ErrorEngineMissing"),
            MagickMissingDelegateErrorException => AppStrings.Get("ErrorEngineMissing"),
            MagickCoderErrorException => AppStrings.Get("ErrorCorruptedImage"),
            MagickException => AppStrings.Get("ErrorCorruptedImage"),
            IOException ioException => MapIoException(ioException),
            _ => AppStrings.Get("ErrorUnknown")
        };
    }

    private static string MapIoException(IOException exception)
    {
        var code = exception.HResult & 0xFFFF;
        return code switch
        {
            32 => AppStrings.Get("ErrorFileInUse"),
            33 => AppStrings.Get("ErrorFileInUse"),
            80 => AppStrings.Get("ErrorOutputFileExists"),
            112 => AppStrings.Get("ErrorDiskFull"),
            _ => AppStrings.Get("ErrorDiskFull")
        };
    }
}
