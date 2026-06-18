using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface INamingService
{
    string CreateBaseFileName(string sourcePath, PhotoMetadata metadata, NamingRule namingRule);
}
