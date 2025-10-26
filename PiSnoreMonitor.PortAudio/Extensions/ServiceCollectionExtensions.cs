using Microsoft.Extensions.DependencyInjection;
using PiSnoreMonitor.Core.Services;
using PiSnoreMonitor.Services;

namespace PiSnoreMonitor.PortAudio.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPortAudio(this IServiceCollection services)
        {
            services.AddSingleton<IWavRecorderFactory, PortAudioWavRecorderFactory>();
            services.AddSingleton<IAudioInputDeviceEnumeratorService, PortAudioInputDeviceEnumeratorService>();
            return services;
        }
    }
}
