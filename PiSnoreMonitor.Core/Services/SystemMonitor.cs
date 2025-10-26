
namespace PiSnoreMonitor.Core.Services
{
    public class SystemMonitor : ISystemMonitor
    {
        public event EventHandler<SystemMonitorStatusEventArgs>? OnSystemStatusUpdate;

        private readonly IMemoryUsageSampler _memoryUsageSampler;
        private readonly ICpuUsageSampler _cpuUsageSampler;
        private System.Timers.Timer? _monitorTimer;

        public SystemMonitor(
            IMemoryUsageSampler memoryUsageSampler,
            ICpuUsageSampler cpuUsageSampler)
        {
            _memoryUsageSampler = memoryUsageSampler;
            _cpuUsageSampler = cpuUsageSampler;
        }

        public void StartMonitoring()
        {
            _monitorTimer = new System.Timers.Timer(1000);
            _monitorTimer.Elapsed += MonitorTimer_Elapsed;
            _monitorTimer.Start();
        }

        public void StopMonitoring()
        {
            _monitorTimer!.Stop();
        }

        private void MonitorTimer_Elapsed(
            object? sender,
            System.Timers.ElapsedEventArgs e)
        {
            _monitorTimer!.Stop();
            var (totalBytes, freeBytes) = _memoryUsageSampler.GetSystemMemory();
            OnSystemStatusUpdate?.Invoke(this, new SystemMonitorStatusEventArgs
            {
                CpuUsagePercentage = _cpuUsageSampler.GetProcessCpuUsagePercent(),
                TotalMemoryBytes = totalBytes,
                FreeMemoryBytes = freeBytes
            });
            _monitorTimer.Start();
        }
    }
}
