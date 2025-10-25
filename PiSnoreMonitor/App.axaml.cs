using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PiSnoreMonitor.Configuration;
using PiSnoreMonitor.Services;
using PiSnoreMonitor.ViewModels;
using PiSnoreMonitor.Views;
using PortAudioSharp;
using Serilog;
using System;
using System.IO;

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
                desktop.Exit += (sender, e) =>
                {
                    Log.CloseAndFlush();
                    _serviceProvider?.Dispose();
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure Serilog
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiSnoreMonitor", "Logs");
            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(logDirectory, "pisnoremonitor-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            services.AddSingleton<ICpuUsageSampler, HardwareInfoCpuUsageSampler>();
            services.AddSingleton<IMemoryUsageSampler, HardwareInfoMemoryUsageSampler>();
            services.AddSingleton<ISystemMonitor, SystemMonitor>();
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IWavRecorderFactory, PortAudioWavRecorderFactory>();
            services.AddSingleton<IAudioInputDeviceEnumeratorService, PortAudioInputDeviceEnumeratorService>();
            services.AddSingleton<IIoService, IoService>();
            services.AddSingleton<IAppSettingsLoader, AppSettingsLoader>();

            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            
            // Register Views
            services.AddTransient<MainWindowView>();

            // Configure logging with Serilog
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger);
            });

            PortAudio.Initialize(); // Initialize PortAudio library
        }
    }
}