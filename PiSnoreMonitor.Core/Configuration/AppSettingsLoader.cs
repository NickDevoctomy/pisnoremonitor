using System.Text.Json;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.Core.Configuration
{
    public class AppSettingsLoader<T>(IIoService ioService) : IAppSettingsLoader<T>
    {
        private static T? _appSettings;

        private readonly JsonSerializerOptions defaultJsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public async Task<T> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (_appSettings != null)
            {
                return _appSettings;
            }

            var path = ioService.GetSpecialPath(Enums.SpecialPaths.AppUserStorage);
            path = ioService.CombinePaths(path, "config.json");

            if (ioService.Exists(path))
            {
                try
                {
                    var json = await ioService.ReadAllTextAsync(path, cancellationToken);
                    var appSettings = JsonSerializer.Deserialize<T>(json);
                    _appSettings = appSettings;
                    return appSettings ?? Activator.CreateInstance<T>();
                }
                catch (JsonException)
                {
                    // Invalid JSON, fall through to return default settings
                }
            }

            _appSettings = Activator.CreateInstance<T>();
            return _appSettings;
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            var path = ioService.GetSpecialPath(Enums.SpecialPaths.AppUserStorage);
            path = ioService.CombinePaths(path, "config.json");

            var fileInfo = new FileInfo(path);
            ioService.CreateDirectory(fileInfo.Directory!.FullName);

            var json = JsonSerializer.Serialize(_appSettings, defaultJsonSerializerOptions);
            await ioService.WriteAllTextAsync(path, json, cancellationToken);
        }
    }
}
