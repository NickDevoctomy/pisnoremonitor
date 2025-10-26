namespace PiSnoreMonitor.Core.Services.Effects.Parameters
{
    public interface IEffectsParameter
    {
        public string Name { get; set; }

        public FloatParameter? AsFloatParameter();
    }
}
