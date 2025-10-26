using Microsoft.Extensions.DependencyInjection;
using PiSnoreMonitor.Core.Services;
using PiSnoreMonitor.HardwareInfo.Services;

namespace PiSnoreMonitor.PortAudio.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHardwareInfo(this IServiceCollection services)
        {
            services.AddSingleton<ICpuUsageSampler, HardwareInfoCpuUsageSampler>();
            services.AddSingleton<IMemoryUsageSampler, HardwareInfoMemoryUsageSampler>();
            return services;
        }
    }
}
