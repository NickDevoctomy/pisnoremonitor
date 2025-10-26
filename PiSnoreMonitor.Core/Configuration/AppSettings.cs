namespace PiSnoreMonitor.Core.Configuration
{
    public class AppSettings
    {
        public int RecordingSampleRate { get; set; } = 48000;
        public bool EnableStereoRecording { get; set; } = false;
        public string SelectedAudioInputDeviceName { get; set; } = string.Empty;
        public bool EnableHpfEffect { get; set; } = false;
        public float HpfEffectCutoffFrequency { get; set; } = 60.0f;
        public bool EnableGainEffect { get; set; } = false;
        public float GainEffectGain { get; set; } = 2.0f;
    }
}
