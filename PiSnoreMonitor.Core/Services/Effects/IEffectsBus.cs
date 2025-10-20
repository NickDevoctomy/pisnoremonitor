using PiSnoreMonitor.Core.Data;

namespace PiSnoreMonitor.Core.Services.Effects
{
    public interface IEffectsBus
    {
        public List<IEffect> Effects { get; }

        public PooledBlock Process(
            PooledBlock block,
            int length);
    }
}
