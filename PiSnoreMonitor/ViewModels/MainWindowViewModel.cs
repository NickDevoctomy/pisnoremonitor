using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using PiSnoreMonitor.Services;
using Avalonia.Controls;
using System;
using System.IO;

namespace PiSnoreMonitor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IStorageService _storageService;
        private readonly IWavRecorder _wavRecorder;

        [ObservableProperty]
        private bool isRecording = false;

        [ObservableProperty]
        private IBrush buttonBackground = Brushes.LightGray;

        [ObservableProperty]
        private string buttonText = "Start Recording";

        [ObservableProperty]
        private GridLength statusRowGridHeight = new(0);

        [ObservableProperty]
        private string startedRecordingAtText = "-";

        [ObservableProperty]
        private string elapsedRecordingTimeText = "-";

        [ObservableProperty]
        private bool isErrorVisible = false;

        [ObservableProperty]
        private string errorMessageText = string.Empty;

        private DateTime _startedRecordingAt;
        private int _updateCounter = 0;

        public MainWindowViewModel(
            IStorageService storageService,
            IWavRecorder wavRecorder)
        {
            _storageService = storageService;
            _wavRecorder = wavRecorder;
            _wavRecorder.WavRecorderRecording += WavRecorder_WavRecorderRecording;
        }

        private void WavRecorder_WavRecorderRecording(object? sender, WavRecorderRecordingEventArgs e)
        {
            _updateCounter++;
            if(_updateCounter % 10 ==0)
            {
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
            var removableDrives = _storageService.GetRemovableStorageDrivePaths();
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
                _wavRecorder.StartRecording(outputFilePath);
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
                _wavRecorder.StopRecording();
                IsRecording = false;
                ButtonBackground = Brushes.LightGray;
                ButtonText = "Start Recording";
                StatusRowGridHeight = new GridLength(0);

                UpdateStartedRecordingTime();
                UpdateElapsedRecordingTime();
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