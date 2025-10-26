namespace PiSnoreMonitor.Core.Services
{
    public interface ISystemMonitor
    {
        public event EventHandler<SystemMonitorStatusEventArgs> OnSystemStatusUpdate;

        public void StartMonitoring();

        public void StopMonitoring();
    }
}
