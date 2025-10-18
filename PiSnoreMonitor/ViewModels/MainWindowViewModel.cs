using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using PiSnoreMonitor.Services;
using Avalonia.Controls;
using System;

namespace PiSnoreMonitor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
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
        private string startedRecordingAtText = string.Empty;

        [ObservableProperty]
        private string elapsedRecordingTimeText = string.Empty;

        private DateTime _startedRecordingAt;

        public MainWindowViewModel(IWavRecorder wavRecorder)
        {
            _wavRecorder = wavRecorder;
            _wavRecorder.WavRecorderRecording += WavRecorder_WavRecorderRecording;
        }

        private void WavRecorder_WavRecorderRecording(object? sender, WavRecorderRecordingEventArgs e)
        {
            UpdateElapsedRecordingTime();
        }

        private void UpdateElapsedRecordingTime()
        {
            if (IsRecording)
            {
                var elapsed = DateTime.Now - _startedRecordingAt;
                ElapsedRecordingTimeText = $"{elapsed}";
            }
            else
            {
                ElapsedRecordingTimeText = "-";
            }
        }

        [RelayCommand]
        private void ToggleRecording()
        {
            if (!IsRecording)
            {
                _wavRecorder.StartRecording("c:/temp/test.wav");
                IsRecording = true;
                ButtonBackground = Brushes.Red;
                ButtonText = "Stop Recording";
                StatusRowGridHeight = new GridLength(48);

                _startedRecordingAt = DateTime.Now;
                StartedRecordingAtText = $"{_startedRecordingAt:dd-MM-yyyy HH:mm:ss}";
                UpdateElapsedRecordingTime();
            }
            else
            {
                _wavRecorder.StopRecording();
                IsRecording = false;
                ButtonBackground = Brushes.LightGray;
                ButtonText = "Start Recording";
                StatusRowGridHeight = new GridLength(0);
            }
        }
    }
}