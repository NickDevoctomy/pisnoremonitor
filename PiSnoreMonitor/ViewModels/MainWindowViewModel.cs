using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using PiSnoreMonitor.Services;
using Avalonia.Controls;
using System;
using System.IO;
using PiSnoreMonitor.Configuration;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using PiSnoreMonitor.Extensions;

namespace PiSnoreMonitor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ILogger<MainWindowViewModel>? _logger;
        private readonly IAppSettingsLoader? _appSettingsLoader;
        private readonly ISystemMonitor? _systemMonitor;
        private readonly IStorageService? _storageService;
        private readonly IWavRecorderFactory? _wavRecorderFactory;
        private readonly IAudioInputDeviceEnumeratorService? _audioInputDeviceEnumerator;
        private IWavRecorder? _wavRecorder;

        [ObservableProperty]
        private bool isRecording = false;

        [ObservableProperty]
        private IBrush buttonBackground = Brushes.LightGray;

        [ObservableProperty]
        private string currentDateTimeText = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

        [ObservableProperty]
        private string buttonText = "Start Recording";

        [ObservableProperty]
        private GridLength titleRowGridHeight = new(32);

        [ObservableProperty]
        private GridLength statusRowGridHeight = new(0);

        [ObservableProperty]
        private string startedRecordingAtText = string.Empty;

        [ObservableProperty]
        private string elapsedRecordingTimeText = string.Empty;

        [ObservableProperty]
        private bool isErrorVisible = false;

        [ObservableProperty]
        private string errorMessageTitle = string.Empty;

        [ObservableProperty]
        private string errorMessageText = string.Empty;

        [ObservableProperty]
        private double amplitude = 0d;

        [ObservableProperty]
        private string memoryUsageText = "🐏 ?/?";

        [ObservableProperty]
        private string cpuUsageText = "🖥️ ?%";

        [ObservableProperty]
        private AppSettings? appSettings;

        [ObservableProperty]
        private ObservableCollection<AudioInputDevice> audioInputDevices = [];

        [ObservableProperty]
        private int selectedAudioInputDeviceId = 0;

        private DateTime _startedRecordingAt;
        private int _updateCounter = 0;

        public MainWindowViewModel()
        {
        }

        public MainWindowViewModel(
            ILogger<MainWindowViewModel> logger,
            IAppSettingsLoader appSettingsLoader,
            ISystemMonitor systemMonitor,
            IStorageService storageService,
            IWavRecorderFactory wavRecorderFactory,
            IAudioInputDeviceEnumeratorService audioInputDeviceEnumerator)
        {
            _logger = logger;
            _appSettingsLoader = appSettingsLoader;
            _systemMonitor = systemMonitor;
            _storageService = storageService;
            _wavRecorderFactory = wavRecorderFactory;
            _audioInputDeviceEnumerator = audioInputDeviceEnumerator;

            _logger.LogInformation("MainWindowViewModel initialized");

            _systemMonitor.OnSystemStatusUpdate += _systemMonitor_OnSystemStatusUpdate;

            _systemMonitor.StartMonitoring();
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            switch (e.PropertyName)
            {
                case nameof(SelectedAudioInputDeviceId):
                    {
                        var selectedAudioInputDeviceName = AudioInputDevices.Single(x => x.Id == SelectedAudioInputDeviceId);
                        AppSettings!.SelectedAudioInputDeviceName = selectedAudioInputDeviceName.Name;
                        break;
                    }
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Initializing MainWindowViewModel");
            if(_appSettingsLoader == null)
            {
                return;
            }

            AppSettings = await _appSettingsLoader!.LoadAsync(cancellationToken);

            var audioInputDevices = _audioInputDeviceEnumerator!.GetAudioInputDeviceNames();
            foreach (var device in audioInputDevices.DistinctBy(x => x.Name))
            {
                _logger?.LogInformation("Found audio input device: {DeviceName} (ID: {DeviceId})", device.Name, device.Id);
                AudioInputDevices.Add(device);
            }

            var selectedAudioInputDeviceName = AppSettings.SelectedAudioInputDeviceName;
            var selectedDevice = AudioInputDevices.SingleOrDefault(x => x.Name == selectedAudioInputDeviceName);
            SelectedAudioInputDeviceId = selectedDevice?.Id ?? PortAudioSharp.PortAudio.DefaultInputDevice;

            _logger?.LogInformation("MainWindowViewModel initialization completed");
        }

        private void _systemMonitor_OnSystemStatusUpdate(object? sender, SystemMonitorStatusEventArgs e)
        {
            var usedGB = Math.Round((e.TotalMemoryBytes - e.FreeMemoryBytes) / 1073741824.0, 2);
            var totalGB = Math.Round(e.TotalMemoryBytes / 1073741824.0, 2);
            MemoryUsageText = $"🐏 {usedGB}/{totalGB} GB";
            CpuUsageText = $"🖥️ {Math.Round(e.CpuUsagePercentage,2)}%";
        }

        private void WavRecorder_WavRecorderRecording(object? sender, WavRecorderRecordingEventArgs e)
        {
            UpdateElapsedRecordingTime();
            _updateCounter++;
            if (_updateCounter % 10 == 0)
            {
                Amplitude = e.CurrentBlock.CalculateAmplitude(0) * 100;
                UpdateElapsedRecordingTime();
            }
        }

        private void UpdateStartedRecordingTime()
        {
            if (IsRecording)
            {
                StartedRecordingAtText = $"{_startedRecordingAt:dd-MM-yyyy HH:mm:ss}";
            }
            else
            {
                ElapsedRecordingTimeText = "-";
            }
            
        }

        private void UpdateElapsedRecordingTime()
        {
            if (IsRecording)
            {
                var elapsed = DateTime.Now - _startedRecordingAt;
                ElapsedRecordingTimeText = elapsed.ToString(@"hh\:mm\:ss");
            }
            else
            {
                ElapsedRecordingTimeText = "-";
            }
        }

        private string GetOutputFilePath()
        {
            var removableDrives = _storageService!.GetRemovableStorageDrivePaths();
            if (removableDrives.Count > 0)
            {
                var prefix = "recording";
                var offset = 0;
                var path = Path.Combine(removableDrives[0], $"{prefix}_{offset}.wav");
                while(File.Exists(path))
                {
                    offset++;
                    path = Path.Combine(removableDrives[0], $"{prefix}_{offset}.wav");
                }
                return path;
            }
            else
            {
                throw new InvalidOperationException("No removable storage drives found.");
            }
        }

        private void DisplayErrorMessage(
            string title,
            string message)
        {
            ErrorMessageTitle = title;
            ErrorMessageText = message;
            IsErrorVisible = true;
        }

        [RelayCommand]
        private async Task Close()
        {
            if(IsRecording)
            {
                _logger?.LogInformation("Closing application while recording is in progress. Stopping recording first.");
                await ToggleRecording();
            }

            _logger?.LogInformation("Saving application settings.");
            await _appSettingsLoader!.SaveAsync(AppSettings!, CancellationToken.None);

            _logger?.LogInformation("Exiting.");
            Environment.Exit(0);
        }

        [RelayCommand]
        private async Task ToggleRecording()
        {
            if (!IsRecording)
            {
                _logger?.LogInformation("Starting recording");

                _wavRecorder = await _wavRecorderFactory!.CreateAsync(SelectedAudioInputDeviceId, AppSettings!.EnableStereoRecording, CancellationToken.None);
                _wavRecorder.WavRecorderRecording += WavRecorder_WavRecorderRecording;

                var outputFilePath = string.Empty;
                try
                {
                    outputFilePath = GetOutputFilePath();
                    _logger?.LogInformation("Recording output file path: {OutputFilePath}", outputFilePath);
                }
                catch (InvalidOperationException ex)
                {
                    _logger?.LogError(ex, "Failed to get output file path for recording");
                    DisplayErrorMessage("Storage Error", ex.Message);
                    return;
                }
                await _wavRecorder!.StartRecordingAsync(outputFilePath, CancellationToken.None);
                IsRecording = true;
                ButtonBackground = Brushes.Red;
                ButtonText = "Stop Recording";
                StatusRowGridHeight = new GridLength(48);

                _updateCounter = 0;
                _startedRecordingAt = DateTime.Now;
                UpdateStartedRecordingTime();
                UpdateElapsedRecordingTime();
                _logger?.LogInformation("Recording started successfully");
            }
            else
            {
                _logger?.LogInformation("Stopping recording");
                await _wavRecorder!.StopRecordingAsync(CancellationToken.None);
                IsRecording = false;
                ButtonBackground = Brushes.LightGray;
                ButtonText = "Start Recording";
                StatusRowGridHeight = new GridLength(0);

                UpdateStartedRecordingTime();
                UpdateElapsedRecordingTime();
                Amplitude = 0;

                _wavRecorder.WavRecorderRecording -= WavRecorder_WavRecorderRecording;
                _wavRecorder.Dispose();
                _wavRecorder = null;
                _logger?.LogInformation("Recording stopped successfully");
            }
        }

        [RelayCommand]
        private void DismissError()
        {
            IsErrorVisible = false;
            ErrorMessageText = string.Empty;
        }
    }
}