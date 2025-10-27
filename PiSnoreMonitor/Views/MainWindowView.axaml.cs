using Avalonia.Controls;
using Avalonia.Interactivity;
using PiSnoreMonitor.Core.Configuration;
using PiSnoreMonitor.ViewModels;
using System;
using System.Threading;

namespace PiSnoreMonitor.Views
{
    public partial class MainWindowView : Window
    {
        private CancellationTokenSource? _loadCts;

        public MainWindowView()
        {
            InitializeComponent();
            Loaded += OnLoadedAsync;
            Unloaded += OnUnloaded;
        }

        public MainWindowView(MainWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private async void OnLoadedAsync(object? s, RoutedEventArgs e)
        {
            _loadCts = new CancellationTokenSource();

            if (DataContext is MainWindowViewModel vm)
            {
                // Bind the StereoAmplitudeDisplay control to the ViewModel
                var stereoDisplay = this.FindControl<Controls.StereoAmplitudeDisplay>("StereoAmplitudeDisplayControl");
                vm.StereoAmplitudeDisplay = stereoDisplay;

                try
                {
                    await vm.InitializeAsync(_loadCts.Token);
                    if (!vm.AppSettings!.StartInKioskMode)
                    {
                        this.WindowState = WindowState.Normal;
                        this.Topmost = false;
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private void OnUnloaded(object? s, RoutedEventArgs e)
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = null;
        }
    }
}