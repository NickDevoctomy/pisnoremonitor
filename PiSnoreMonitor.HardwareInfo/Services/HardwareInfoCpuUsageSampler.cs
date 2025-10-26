using Hardware.Info;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.HardwareInfo.Services
{
    public class HardwareInfoCpuUsageSampler(IHardwareInfo hardwareInfo) : ICpuUsageSampler
    {
        public double GetProcessCpuUsagePercent()
        {
            hardwareInfo.RefreshCPUList(true, 500, true);

            var allCpus = hardwareInfo.CpuList;
            if (allCpus.Count != 0)
            {
                return allCpus
                    .Select(x => (long)x.PercentProcessorTime)
                    .Average();
            }

            return 0;
        }
    }
}
