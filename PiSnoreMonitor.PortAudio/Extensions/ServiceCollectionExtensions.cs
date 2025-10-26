using Microsoft.Extensions.DependencyInjection;
using PiSnoreMonitor.Core.Services;
using PiSnoreMonitor.PortAudio.Services;

namespace PiSnoreMonitor.PortAudio.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPortAudio(this IServiceCollection services)
        {
            services.AddSingleton<IWavRecorderFactory, PortAudioWavRecorderFactory>();
            services.AddSingleton<IAudioInputDeviceEnumeratorService, PortAudioInputDeviceEnumeratorService>();

            PortAudioSharp.PortAudio.Initialize(); // Initialize PortAudio library

            return services;
        }
    }
}
