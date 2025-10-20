using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Services;
using PiSnoreMonitor.Services.Effects.Parameters;
using PiSnoreMonitor.ViewModels;
using PiSnoreMonitor.Views;

namespace PiSnoreMonitor
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindowView>();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<ICpuUsageSampler, HardwareInfoCpuUsageSampler>();
            services.AddSingleton<IMemoryUsageSampler, HardwareInfoMemoryUsageSampler>();
            services.AddSingleton<ISystemMonitor, SystemMonitor>();
            services.AddSingleton<IStorageService, StorageService>();


            services.AddSingleton<IWavRecorder>((provider) =>
            {
                // This will be configurable within the UI, at the moment I am testing with hardcoded values
                var hpfEffect = new HpfEffect();
                var cutoffParam = new FloatParameter("CutoffFrequency", 12.0f);
                hpfEffect.SetParameters(cutoffParam);

                var gainEffect = new GainEffect();
                var gainParam = new FloatParameter("Gain", 20.0f);
                gainEffect.SetParameters(gainParam);

                var effectsBus = new EffectsBus();
                effectsBus.Effects.Add(hpfEffect);
                effectsBus.Effects.Add(gainEffect);

                return new WavRecorder(44100, 1, 1024, effectsBus);
            });
            
            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            
            // Register Views
            services.AddTransient<MainWindowView>();
        }
    }
}