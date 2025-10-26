using System.Diagnostics.CodeAnalysis;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.PortAudio.Services
{
    [ExcludeFromCodeCoverage(Justification = "Not going to attempt to abstract out PortAudio.")]
    public class PortAudioInputDeviceEnumeratorService : IAudioInputDeviceEnumeratorService
    {
        public IEnumerable<AudioInputDevice> GetAudioInputDeviceNames()
        {
            var count = PortAudioSharp.PortAudio.DeviceCount;
            for (int i = 0; i < count; i++)
            {
                var deviceInfo = PortAudioSharp.PortAudio.GetDeviceInfo(i);
                if (deviceInfo.maxInputChannels > 0)
                {
                    yield return new AudioInputDevice(i, deviceInfo.name);
                }
            }
        }
    }
}
