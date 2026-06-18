using System.IO;

namespace DiadiaHeicConverter.App.Models;

public sealed class PhotoMetadata
{
    public DateTime? DateTimeOriginal { get; init; }

    public DateTime? CreateDate { get; init; }

    public DateTime FileCreatedAt { get; init; }

    public DateTime FileModifiedAt { get; init; }

    public bool HasGps { get; init; }

    public string? ReadWarning { get; init; }

    public DateTime BestDate
    {
        get
        {
            if (DateTimeOriginal.HasValue)
            {
                return DateTimeOriginal.Value;
            }

            if (CreateDate.HasValue)
            {
                return CreateDate.Value;
            }

            return FileCreatedAt != default ? FileCreatedAt : FileModifiedAt;
        }
    }

    public static PhotoMetadata FromFile(FileInfo fileInfo, string? warning = null)
    {
        return new PhotoMetadata
        {
            FileCreatedAt = fileInfo.Exists ? fileInfo.CreationTime : DateTime.Now,
            FileModifiedAt = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now,
            ReadWarning = warning
        };
    }
}
