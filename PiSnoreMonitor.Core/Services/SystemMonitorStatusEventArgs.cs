namespace PiSnoreMonitor.Core.Services
{
    public class SystemMonitorStatusEventArgs
    {
        public double CpuUsagePercentage { get; set; }
        public ulong TotalMemoryBytes { get; set; }
        public ulong FreeMemoryBytes { get; set; }
    }
}
