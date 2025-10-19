namespace PiSnoreMonitor.Services
{
    public interface ICpuUsageSampler
    {
        public double GetProcessCpuUsagePercent();
    }
}
