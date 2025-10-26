namespace PiSnoreMonitor.Core.Services
{
    public interface IMemoryUsageSampler
    {
        public (ulong totalBytes, ulong freeBytes) GetSystemMemory();
    }
}
