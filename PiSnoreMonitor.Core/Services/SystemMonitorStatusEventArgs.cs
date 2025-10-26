namespace PiSnoreMonitor.Core.Services
{
    public class SystemMonitorStatusEventArgs : IEquatable<SystemMonitorStatusEventArgs>
    {
        public double CpuUsagePercentage { get; set; }
        public ulong TotalMemoryBytes { get; set; }
        public ulong FreeMemoryBytes { get; set; }

        public bool Equals(SystemMonitorStatusEventArgs? other)
        {
            return
                other != null &&
                CpuUsagePercentage.Equals(other.CpuUsagePercentage) &&
                TotalMemoryBytes.Equals(other.TotalMemoryBytes) &&
                FreeMemoryBytes.Equals(other.FreeMemoryBytes);
        }
    }
}
