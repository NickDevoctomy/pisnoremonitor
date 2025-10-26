using Microsoft.Extensions.DependencyInjection;
using PiSnoreMonitor.Core.Configuration;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.Core.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPiSnoreMonitorCore(this IServiceCollection services)
        {
            services.AddSingleton<IIoService, IoService>();
            services.AddSingleton<IAppSettingsLoader<AppSettings>, AppSettingsLoader<AppSettings>>();
            services.AddSingleton<ISideCarWriterService, SideCarWriterService>();
            services.AddSingleton<ISystemMonitor, SystemMonitor>();
            return services;
        }
    }
}
