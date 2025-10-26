namespace PiSnoreMonitor.Core.Data
{
    public class PooledBlock
    {
        public byte[] Buffer { get; set; } = Array.Empty<byte>();

        public int Count { get; set; }
    }
}