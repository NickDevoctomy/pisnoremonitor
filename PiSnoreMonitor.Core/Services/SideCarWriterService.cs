namespace PiSnoreMonitor.Core.Services
{
    public class SideCarWriterService(IIoService ioService) : ISideCarWriterService
    {
        public async Task<SideCarInfo> StartRecordingAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            var sideCarInfo = new SideCarInfo(filePath);
            var sideCarInfoJson = System.Text.Json.JsonSerializer.Serialize(sideCarInfo);
            await ioService.WriteAllTextAsync(filePath, sideCarInfoJson, cancellationToken);
            return sideCarInfo;
        }

        public async Task StopRecordingAsync(
            SideCarInfo sideCarInfo,
            CancellationToken cancellationToken = default)
        {
            sideCarInfo.StoppedRecordingAt = DateTime.Now;
            var sideCarInfoJson = System.Text.Json.JsonSerializer.Serialize(sideCarInfo);
            await ioService.WriteAllTextAsync(sideCarInfo.FilePath, sideCarInfoJson, cancellationToken);
        }
    }
}
