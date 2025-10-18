using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PiSnoreMonitor.Services;
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
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IWavRecorder>(provider => new WavRecorder(44100, 1, 1024));
            
            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();
            
            // Register Views
            services.AddTransient<MainWindowView>();
        }
    }
}