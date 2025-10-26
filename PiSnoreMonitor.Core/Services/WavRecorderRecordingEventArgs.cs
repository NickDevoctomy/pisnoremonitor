using PiSnoreMonitor.Core.Data;

namespace PiSnoreMonitor.Core.Services
{
    public class WavRecorderRecordingEventArgs(PooledBlock currentBlock) : EventArgs
    {
        public PooledBlock CurrentBlock { get; init; } = currentBlock;
    }
}
