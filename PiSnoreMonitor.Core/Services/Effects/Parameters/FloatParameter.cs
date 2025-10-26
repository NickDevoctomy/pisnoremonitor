namespace PiSnoreMonitor.Core.Services.Effects.Parameters
{
    public class FloatParameter : EffectsParameter<float>, IEffectsParameter
    {
        public FloatParameter(
            string name,
            float value)
        {
            Name = name;
            Value = value;
        }
    }
}
