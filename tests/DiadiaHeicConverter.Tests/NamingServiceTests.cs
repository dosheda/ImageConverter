using DiadiaHeicConverter.App.Models;
using DiadiaHeicConverter.App.Services;

namespace DiadiaHeicConverter.Tests;

public sealed class NamingServiceTests
{
    [Fact]
    public void CreateBaseFileName_keeps_original_file_name()
    {
        var service = new NamingService();
        var metadata = CreateMetadata();

        var result = service.CreateBaseFileName(@"C:\photos\IMG_1234.HEIC", metadata, NamingRule.OriginalFileName);

        Assert.Equal("IMG_1234", result);
    }

    [Fact]
    public void CreateBaseFileName_supports_datetime_prefix()
    {
        var service = new NamingService();
        var metadata = CreateMetadata();

        var result = service.CreateBaseFileName(@"C:\photos\IMG_1234.HEIC", metadata, NamingRule.DateTimeOriginalAndFileName);

        Assert.Equal("2024-08-12_15-32-08_IMG_1234", result);
    }

    [Fact]
    public void CreateBaseFileName_supports_date_prefix()
    {
        var service = new NamingService();
        var metadata = CreateMetadata();

        var result = service.CreateBaseFileName(@"C:\photos\IMG_1234.HEIC", metadata, NamingRule.DateAndOriginalFileName);

        Assert.Equal("2024-08-12_IMG_1234", result);
    }

    private static PhotoMetadata CreateMetadata()
    {
        return new PhotoMetadata
        {
            DateTimeOriginal = new DateTime(2024, 8, 12, 15, 32, 8),
            CreateDate = new DateTime(2024, 8, 11, 10, 0, 0),
            FileCreatedAt = new DateTime(2024, 8, 10),
            FileModifiedAt = new DateTime(2024, 8, 9)
        };
    }
}
