namespace PiSnoreMonitor.Configuration
{
    public class AppSettings
    {
        public bool EnableHpfEffect { get; set; } = true;
        public float HpfEffectCutoffFrequency { get; set; } = 60.0f;
        public bool EnableGainEffect { get; set; } = true;
        public float GainEffectGain { get; set; } = 2.0f;
    }
}
