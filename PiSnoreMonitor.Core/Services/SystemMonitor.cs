namespace PiSnoreMonitor.Core.Services
{
    public class SystemMonitor(
        IMemoryUsageSampler memoryUsageSampler,
        ICpuUsageSampler cpuUsageSampler) : ISystemMonitor
    {
        private readonly IMemoryUsageSampler _memoryUsageSampler = memoryUsageSampler;
        private readonly ICpuUsageSampler _cpuUsageSampler = cpuUsageSampler;
        private System.Timers.Timer? _monitorTimer;

        public event EventHandler<SystemMonitorStatusEventArgs>? OnSystemStatusUpdate;

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
