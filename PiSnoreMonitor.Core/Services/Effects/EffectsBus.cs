using PiSnoreMonitor.Core.Data;

namespace PiSnoreMonitor.Core.Services.Effects
{
    public class EffectsBus : IEffectsBus
    {
        public List<IEffect> Effects { get; } = [];

        public PooledBlock Process(
            PooledBlock block,
            int length)
        {
            if(Effects.Count == 0)
            {
                return block;
            }

            var processedBuffer = block.Buffer;
            for (var i = 0; i < Effects.Count; i++)
            {
                processedBuffer = Effects[i].Process(processedBuffer, length);
            }

            return new PooledBlock
            {
                Buffer = processedBuffer,
                Count = length
            };
        }
    }
}
