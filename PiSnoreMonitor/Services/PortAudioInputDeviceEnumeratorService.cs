using PortAudioSharp;
using System.Collections.Generic;

namespace PiSnoreMonitor.Services
{
    internal class PortAudioInputDeviceEnumeratorService : IAudioInputDeviceEnumeratorService
    {
        public IEnumerable<AudioInputDevice> GetAudioInputDeviceNames()
        {
            var count = PortAudio.DeviceCount;
            for (int i = 0; i < count; i++)
            {
                var deviceInfo = PortAudio.GetDeviceInfo(i);
                if (deviceInfo.maxInputChannels > 0)
                {
                    yield return new AudioInputDevice
                    {
                        Id = i,
                        Name = deviceInfo.name
                    };
                }
            }

            var inputDevice = PortAudio.DefaultInputDevice;
        }
    }
}
