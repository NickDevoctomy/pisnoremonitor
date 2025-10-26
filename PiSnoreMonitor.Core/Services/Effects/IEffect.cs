using PiSnoreMonitor.Core.Services.Effects.Parameters;

namespace PiSnoreMonitor.Core.Services.Effects
{
    public interface IEffect
    {
        public List<IEffectsParameter> GetParameters();

        public void SetParameters(params IEffectsParameter[] parameters);

        public byte[] Process(
            byte[] block,
            int length,
            int channels);
    }
}
