namespace PiSnoreMonitor.Core.Services
{
    public interface ICpuUsageSampler
    {
        public double GetProcessCpuUsagePercent();
    }
}
