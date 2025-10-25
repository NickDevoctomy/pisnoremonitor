using System.Collections.Generic;

namespace PiSnoreMonitor.Services
{
    public interface IAudioInputDeviceEnumeratorService
    {
        public IEnumerable<AudioInputDevice> GetAudioInputDeviceNames();
    }
}
