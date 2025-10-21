using PiSnoreMonitor.Services;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Configuration
{
    public class AppSettingsLoader(IIoService ioService) : IAppSettingsLoader
    {
        private readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
        {
            var path = ioService.GetSpecialPath(Enums.SpecialPaths.AppUserStorage);
            path = System.IO.Path.Combine(path, "config.json");

            if(System.IO.Path.Exists(path))
            {
                var json = await ioService.ReadAllTextAsync(path, cancellationToken);
                var appSettings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json);
                return appSettings ?? new AppSettings();
            }

            return new AppSettings();
        }

        public async Task SaveAsync(AppSettings appSettings, CancellationToken cancellationToken)
        {
            var path = ioService.GetSpecialPath(Enums.SpecialPaths.AppUserStorage);
            path = System.IO.Path.Combine(path, "config.json");

            var json = System.Text.Json.JsonSerializer.Serialize(appSettings, DefaultJsonSerializerOptions);
            await ioService.WriteAllTextAsync(path, json, cancellationToken);
        }
    }
}
