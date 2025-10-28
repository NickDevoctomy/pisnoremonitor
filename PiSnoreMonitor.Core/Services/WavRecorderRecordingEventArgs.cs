using PiSnoreMonitor.Core.Data;

namespace PiSnoreMonitor.Core.Services
{
    public class WavRecorderRecordingEventArgs(
        PooledBlock currentBlock,
        long totalSamples,
        int sampleRate,
        int channels,
        int channelMaxBlocks,
        int channelCurBlocks) : EventArgs
    {
        public PooledBlock CurrentBlock { get; init; } = currentBlock;

        /// <summary>
        /// Total number of samples recorded so far (per channel)
        /// </summary>
        public long TotalSamples { get; init; } = totalSamples;

        /// <summary>
        /// Sample rate in Hz
        /// </summary>
        public int SampleRate { get; init; } = sampleRate;

        /// <summary>
        /// Number of channels
        /// </summary>
        public int Channels { get; init; } = channels;

        /// <summary>
        /// Total duration of recording so far
        /// </summary>
        public TimeSpan TotalDuration => TimeSpan.FromSeconds((double)TotalSamples / SampleRate);

        /// <summary>
        /// Total bytes of audio data recorded so far
        /// </summary>
        public long TotalBytes => TotalSamples * Channels * 2; // 2 bytes per sample (16-bit)

        /// <summary>
        /// Maximum number of blocks allocated for writing channel
        /// </summary>
        public int ChannelMaxBlocks { get; set; } = channelMaxBlocks;

        /// <summary>
        /// Current number of blocks allocated for writing channel
        /// </summary>
        public int ChannelCurBlocks { get; set; } = channelCurBlocks;
    }
}
