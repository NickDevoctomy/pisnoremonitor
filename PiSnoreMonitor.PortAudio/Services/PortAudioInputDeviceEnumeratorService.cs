using PiSnoreMonitor.Core.Services;
using System.Diagnostics.CodeAnalysis;

namespace PiSnoreMonitor.Services
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
                    yield return new AudioInputDevice
                    {
                        Id = i,
                        Name = deviceInfo.name
                    };
                }
            }

            var inputDevice = PortAudioSharp.PortAudio.DefaultInputDevice;
        }
    }
}
