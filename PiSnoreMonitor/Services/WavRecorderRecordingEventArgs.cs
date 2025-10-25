using PiSnoreMonitor.Core.Data;
using System;

namespace PiSnoreMonitor.Services
{
    public class WavRecorderRecordingEventArgs(PooledBlock currentBlock) : EventArgs
    {
        public PooledBlock CurrentBlock { get; init; } = currentBlock;
    }
}
