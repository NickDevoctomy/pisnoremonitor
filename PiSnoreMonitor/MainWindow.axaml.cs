using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace PiSnoreMonitor
{
    public partial class MainWindow : Window
    {
        private WavRecorder wavRecorder = new(44100, 1, 1024);
        private bool isRecording = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void ClickHandler(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            if (!isRecording)
            {
                wavRecorder.StartRecording("c:/temp/test.wav");
                isRecording = true;
                button!.Tag = button!.Background;
                button!.Background = Avalonia.Media.Brushes.Red;
            }
            else
            {
                wavRecorder.StopRecording();
                button!.Background = (Brush)button!.Tag!;
                isRecording = false;
            }
        }
         
    }
}