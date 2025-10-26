namespace PiSnoreMonitor.Core.Services
{
    public interface IWavRecorder : IDisposable
    {
        public event EventHandler<WavRecorderRecordingEventArgs>? WavRecorderRecording;

        public Task StartRecordingAsync(
            string filePath,
            CancellationToken cancellationToken);

        public Task StopRecordingAsync(CancellationToken cancellationToken);
    }
}
