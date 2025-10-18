using System;

namespace PiSnoreMonitor.Services
{
    public class WavRecorderRecordingEventArgs : EventArgs
    {
        public float Amplitude { get; set; }
    }
}
