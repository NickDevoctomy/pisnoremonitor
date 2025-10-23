using System;
using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
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
