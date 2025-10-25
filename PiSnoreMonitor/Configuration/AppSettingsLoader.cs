using PiSnoreMonitor.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Configuration
{
    public class AppSettingsLoader(IIoService ioService) : IAppSettingsLoader
    {
        private static AppSettings? _appSettings;

        private readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
        {
            if (_appSettings != null)
            {
                return _appSettings;
            }

            var path = ioService.GetSpecialPath(Enums.SpecialPaths.AppUserStorage);
            path = System.IO.Path.Combine(path, "config.json");

            if(System.IO.Path.Exists(path))
            {
                var json = await ioService.ReadAllTextAsync(path, cancellationToken);
                var appSettings = JsonSerializer.Deserialize<AppSettings>(json);
                _appSettings = appSettings;
                return appSettings ?? new AppSettings();
            }

            _appSettings = new AppSettings();
            return _appSettings;
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            var path = ioService.GetSpecialPath(Enums.SpecialPaths.AppUserStorage);
            path = System.IO.Path.Combine(path, "config.json");

            var fileInfo = new System.IO.FileInfo(path);
            fileInfo.Directory!.Create();

            var json = JsonSerializer.Serialize(_appSettings, DefaultJsonSerializerOptions);
            await ioService.WriteAllTextAsync(path, json, cancellationToken);
        }
    }
}
