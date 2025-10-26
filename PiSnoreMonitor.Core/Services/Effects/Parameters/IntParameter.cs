namespace PiSnoreMonitor.Core.Services.Effects.Parameters
{
    public class IntParameter : EffectsParameter<int>, IEffectsParameter
    {
        public IntParameter(
            string name,
            int value)
        {
            Name = name;
            Value = value;
        }
    }
}
