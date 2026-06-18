using DiadiaHeicConverter.App.Models;

namespace DiadiaHeicConverter.App.Services;

public interface ISettingsService
{
    AppSettings Load();

    void Save(AppSettings settings);
}
