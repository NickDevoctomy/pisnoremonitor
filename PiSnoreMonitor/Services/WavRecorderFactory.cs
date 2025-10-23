using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiSnoreMonitor.Configuration;
using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Services.Effects.Parameters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
{
    public class WavRecorderFactory : IWavRecorderFactory
    {
        private readonly IAppSettingsLoader _appSettingsLoader;
        private readonly IServiceProvider _serviceProvider;

        public WavRecorderFactory(
            IAppSettingsLoader appSettingsLoader,
            IServiceProvider serviceProvider)
        {
            _appSettingsLoader = appSettingsLoader;
            _serviceProvider = serviceProvider;
        }

        public async Task<IWavRecorder> CreateAsync(CancellationToken cancellationToken = default)
        {
            const int sampleRate = 44100;
            var appSettings = await _appSettingsLoader.LoadAsync(cancellationToken);

            var effectsBus = new EffectsBus();

            if (appSettings.EnableHpfEffect)
            {
                var hpfEffect = new HpfEffect();
                var cutoffParam = new FloatParameter("CutoffFrequency", appSettings.HpfEffectCutoffFrequency);
                var sampleRateParam = new FloatParameter("SampleRate", sampleRate);
                hpfEffect.SetParameters(cutoffParam, sampleRateParam);
                effectsBus.Effects.Add(hpfEffect);
            }

            if(appSettings.EnableGainEffect)
            {
                var gainEffect = new GainEffect();
                var gainParam = new FloatParameter("Gain", appSettings.GainEffectGain);
                gainEffect.SetParameters(gainParam);
                effectsBus.Effects.Add(gainEffect);
            }

            return new WavRecorder(
                sampleRate,
                1,
                1024,
                effectsBus.Effects.Count > 0 ? effectsBus : null,
                _serviceProvider.GetService<ILogger<WavRecorder>>()!);
        }
    }
}
