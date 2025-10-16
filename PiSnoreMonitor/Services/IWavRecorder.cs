using System;

namespace PiSnoreMonitor.Services
{
    public interface IWavRecorder : IDisposable
    {
        public void StartRecording(string filePath);
        public void StopRecording();
    }
}
