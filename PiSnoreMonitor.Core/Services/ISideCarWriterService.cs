namespace PiSnoreMonitor.Core.Services
{
    public interface ISideCarWriterService
    {
        public Task<SideCarInfo> StartRecordingAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        public Task StopRecordingAsync(
            SideCarInfo SideCarInfo,
            CancellationToken cancellationToken = default);
    }
}
