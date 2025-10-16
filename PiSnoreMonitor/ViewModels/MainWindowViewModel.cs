using System.Windows.Input;
using Avalonia.Media;
using PiSnoreMonitor.Services;

namespace PiSnoreMonitor.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IWavRecorder _wavRecorder;
        private bool _isRecording = false;
        private IBrush _buttonBackground = Brushes.LightGray;
        private string _buttonText = "Start Recording";

        public MainWindowViewModel(IWavRecorder wavRecorder)
        {
            _wavRecorder = wavRecorder;
            ToggleRecordingCommand = new RelayCommand(ToggleRecording);
        }

        public bool IsRecording
        {
            get => _isRecording;
            set => SetField(ref _isRecording, value);
        }

        public IBrush ButtonBackground
        {
            get => _buttonBackground;
            set => SetField(ref _buttonBackground, value);
        }

        public string ButtonText
        {
            get => _buttonText;
            set => SetField(ref _buttonText, value);
        }

        public ICommand ToggleRecordingCommand { get; }

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