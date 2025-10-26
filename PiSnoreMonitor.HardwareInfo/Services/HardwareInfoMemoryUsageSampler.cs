using Hardware.Info;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.HardwareInfo.Services
{
    public class HardwareInfoMemoryUsageSampler : IMemoryUsageSampler
    {
        private IHardwareInfo _hardwareInfo = new Hardware.Info.HardwareInfo();

        public (ulong totalBytes, ulong freeBytes) GetSystemMemory()
        {
            _hardwareInfo.RefreshMemoryStatus();
            return (_hardwareInfo.MemoryStatus.TotalPhysical, _hardwareInfo.MemoryStatus.AvailablePhysical);
        }
    }
}
