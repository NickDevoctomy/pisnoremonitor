using Hardware.Info;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.HardwareInfo.Services
{
    public class HardwareInfoMemoryUsageSampler(IHardwareInfo hardwareInfo) : IMemoryUsageSampler
    {
        public (ulong totalBytes, ulong freeBytes) GetSystemMemory()
        {
            hardwareInfo.RefreshMemoryStatus();
            return (hardwareInfo.MemoryStatus.TotalPhysical, hardwareInfo.MemoryStatus.AvailablePhysical);
        }
    }
}
