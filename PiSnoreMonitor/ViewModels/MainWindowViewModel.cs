using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using PiSnoreMonitor.Services;

namespace PiSnoreMonitor.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly IWavRecorder _wavRecorder;

        [ObservableProperty]
        private bool _isRecording = false;

        [ObservableProperty]
        private IBrush _buttonBackground = Brushes.LightGray;

        [ObservableProperty]
        private string _buttonText = "Start Recording";

        public MainWindowViewModel(IWavRecorder wavRecorder)
        {
            _wavRecorder = wavRecorder;
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
            }
            else
            {
                _wavRecorder.StopRecording();
                IsRecording = false;
                ButtonBackground = Brushes.LightGray;
                ButtonText = "Start Recording";
            }
        }
    }
}