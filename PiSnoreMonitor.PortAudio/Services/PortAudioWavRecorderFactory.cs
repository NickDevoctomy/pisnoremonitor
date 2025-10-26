using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiSnoreMonitor.Core.Configuration;
using PiSnoreMonitor.Core.Services;
using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Core.Services.Effects.Parameters;

namespace PiSnoreMonitor.PortAudio.Services
{
    [ExcludeFromCodeCoverage(Justification = "Not going to attempt to abstract out PortAudio.")]
    public class PortAudioWavRecorderFactory(
        IAppSettingsLoader<AppSettings> appSettingsLoader,
        IServiceProvider serviceProvider) : IWavRecorderFactory
    {
        public async Task<IWavRecorder> CreateAsync(int deviceId, bool stereo, CancellationToken cancellationToken = default)
        {
            var appSettings = await appSettingsLoader.LoadAsync(cancellationToken);
            var effectsBus = new EffectsBus();

            if (appSettings.EnableHpfEffect)
            {
                var hpfEffect = new HpfEffect();
                var cutoffParam = new FloatParameter("CutoffFrequency", appSettings.HpfEffectCutoffFrequency);
                var sampleRateParam = new FloatParameter("SampleRate", appSettings.RecordingSampleRate);
                hpfEffect.SetParameters(cutoffParam, sampleRateParam);
                effectsBus.Effects.Add(hpfEffect);
            }

            if (appSettings.EnableGainEffect)
            {
                var gainEffect = new GainEffect();
                var gainParam = new FloatParameter("Gain", appSettings.GainEffectGain);
                gainEffect.SetParameters(gainParam);
                effectsBus.Effects.Add(gainEffect);
            }

            return new PortAudioWavRecorder(
                deviceId,
                appSettings.RecordingSampleRate,
                stereo ? 2 : 1,
                1024,
                effectsBus.Effects.Count > 0 ? effectsBus : null,
                serviceProvider.GetService<ILogger<PortAudioWavRecorder>>()!);
        }
    }
}
