using Hardware.Info;

namespace PiSnoreMonitor.Services
{
    public class HardwareInfoMemoryUsageSampler : IMemoryUsageSampler
    {
        private IHardwareInfo _hardwareInfo = new HardwareInfo();

        public (ulong totalBytes, ulong freeBytes) GetSystemMemory()
        {
            _hardwareInfo.RefreshMemoryStatus();
            return (_hardwareInfo.MemoryStatus.TotalPhysical, _hardwareInfo.MemoryStatus.AvailablePhysical);
        }
    }
}
