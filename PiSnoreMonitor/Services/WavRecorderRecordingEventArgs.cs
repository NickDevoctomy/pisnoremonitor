using PiSnoreMonitor.Core.Data;
using System;

namespace PiSnoreMonitor.Services
{
    public class WavRecorderRecordingEventArgs : EventArgs
    {
        public PooledBlock CurrentBlock { get; set; }
    }
}
