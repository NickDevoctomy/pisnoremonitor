namespace PiSnoreMonitor.Core.Services
{
    public interface IWavRecorderFactory
    {
        public Task<IWavRecorder> CreateAsync(int deviceId, bool stereo, CancellationToken cancellationToken);
    }
}
