using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using PiSnoreMonitor.Services;
using Avalonia.Controls;
using System;
using System.IO;
using Avalonia;

namespace PiSnoreMonitor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly ISystemMonitor? _systemMonitor;
        private readonly IStorageService? _storageService;
        private readonly IWavRecorder? _wavRecorder;

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
        private string errorMessageText = string.Empty;

        [ObservableProperty]
        private double amplitude = 0d;

        [ObservableProperty]
        private string memoryUsageText = "🐏 ?/?";

        [ObservableProperty]
        private string cpuUsageText = "🖥️ ?%";

        private DateTime _startedRecordingAt;
        private int _updateCounter = 0;

        public MainWindowViewModel()
        {
        }

        public MainWindowViewModel(
            ISystemMonitor systemMonitor,
            IStorageService storageService,
            IWavRecorder wavRecorder)
        {
            _systemMonitor = systemMonitor;
            _storageService = storageService;
            _wavRecorder = wavRecorder;

            _systemMonitor.OnSystemStatusUpdate += _systemMonitor_OnSystemStatusUpdate;
            _wavRecorder.WavRecorderRecording += WavRecorder_WavRecorderRecording;

            _systemMonitor.StartMonitoring();
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
                Amplitude = e.Amplitude * 100;
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

        private void DisplayErrorMessage(string message)
        {
            ErrorMessageText = message;
            IsErrorVisible = true;
        }

        [RelayCommand]
        private void Close()
        {
            Environment.Exit(0);
        }

        [RelayCommand]
        private void ToggleRecording()
        {
            if (!IsRecording)
            {
                var outputFilePath = string.Empty;
                try
                {
                    outputFilePath = GetOutputFilePath();
                }
                catch (InvalidOperationException ex)
                {
                    DisplayErrorMessage(ex.Message);
                    return;
                }
                _wavRecorder!.StartRecording(outputFilePath);
                IsRecording = true;
                ButtonBackground = Brushes.Red;
                ButtonText = "Stop Recording";
                StatusRowGridHeight = new GridLength(48);

                _updateCounter = 0;
                _startedRecordingAt = DateTime.Now;
                UpdateStartedRecordingTime();
                UpdateElapsedRecordingTime();
            }
            else
            {
                _wavRecorder!.StopRecording();
                IsRecording = false;
                ButtonBackground = Brushes.LightGray;
                ButtonText = "Start Recording";
                StatusRowGridHeight = new GridLength(0);

                UpdateStartedRecordingTime();
                UpdateElapsedRecordingTime();
                Amplitude = 0;
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