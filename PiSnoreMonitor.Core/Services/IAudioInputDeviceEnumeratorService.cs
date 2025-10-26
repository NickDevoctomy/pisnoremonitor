namespace PiSnoreMonitor.Core.Services
{
    public interface IAudioInputDeviceEnumeratorService
    {
        public IEnumerable<AudioInputDevice> GetAudioInputDeviceNames();
    }
}
