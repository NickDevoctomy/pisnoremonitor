using Hardware.Info;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.HardwareInfo.Services
{
    public class HardwareInfoCpuUsageSampler : ICpuUsageSampler
    {
        private IHardwareInfo _hardwareInfo = new Hardware.Info.HardwareInfo();

        public double GetProcessCpuUsagePercent()
        {
            _hardwareInfo.RefreshCPUList(true, 500, true);

            var allCpus = _hardwareInfo.CpuList;
            if(allCpus.Any())
            {
                return allCpus
                    .Select(x => (long)x.PercentProcessorTime)
                    .Average();
            }

            return 0;
        }
    }
}
