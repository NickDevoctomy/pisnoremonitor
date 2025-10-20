using PiSnoreMonitor.Data;
using System.Collections.Generic;

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
