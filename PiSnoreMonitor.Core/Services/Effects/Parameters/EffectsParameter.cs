namespace PiSnoreMonitor.Core.Services.Effects.Parameters
{
    public class EffectsParameter<T> : IEffectsParameter
    {
        public string Name { get; set; } = string.Empty;

        public T? Value { get; set; } = default;

        public FloatParameter? AsFloatParameter() => this as FloatParameter;
    }
}
