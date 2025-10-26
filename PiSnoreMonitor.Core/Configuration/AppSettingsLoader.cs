using PiSnoreMonitor.Core.Services;
using System.Text.Json;

namespace PiSnoreMonitor.Core.Configuration
{
    public class AppSettingsLoader<T>(IIoService ioService) : IAppSettingsLoader<T>
    {
        private static T? _appSettings;

        private readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
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
            path = Path.Combine(path, "config.json");

            if(Path.Exists(path))
            {
                var json = await ioService.ReadAllTextAsync(path, cancellationToken);
                var appSettings = JsonSerializer.Deserialize<T>(json);
                _appSettings = appSettings;
                return (appSettings ?? Activator.CreateInstance<T>());
            }

            _appSettings = Activator.CreateInstance<T>();
            return _appSettings;
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            var path = ioService.GetSpecialPath(Enums.SpecialPaths.AppUserStorage);
            path = Path.Combine(path, "config.json");

            var fileInfo = new FileInfo(path);
            fileInfo.Directory!.Create();

            var json = JsonSerializer.Serialize(_appSettings, DefaultJsonSerializerOptions);
            await ioService.WriteAllTextAsync(path, json, cancellationToken);
        }
    }
}
