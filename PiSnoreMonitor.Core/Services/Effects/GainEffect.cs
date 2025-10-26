using PiSnoreMonitor.Core.Services.Effects.Parameters;

namespace PiSnoreMonitor.Core.Services.Effects
{
    public class GainEffect : IEffect
    {
        private readonly FloatParameter gainParameter;

        public GainEffect()
        {
            gainParameter = new FloatParameter("Gain", 1.0f);
        }

        public List<IEffectsParameter> GetParameters()
        {
            return new List<IEffectsParameter>([gainParameter]);
        }

        public void SetParameters(params IEffectsParameter[] parameters)
        {
            foreach (var param in parameters)
            {
                if (param is FloatParameter floatParam && floatParam.Name == "Gain")
                {
                    gainParameter.AsFloatParameter()!.Value = floatParam.Value;
                }
            }
        }

        public byte[] Process(
            byte[] block,
            int length,
            int channels)
        {
            if (block == null || length == 0)
            {
                return block ?? Array.Empty<byte>();
            }

            byte[] output = new byte[length];

            unsafe
            {
                fixed (byte* inputPtr = block)
                fixed (byte* outputPtr = output)
                {
                    short* inputSamples = (short*)inputPtr;
                    short* outputSamples = (short*)outputPtr;

                    int sampleCount = length / 2; // 16-bit = 2 bytes per sample
                    float gain = gainParameter.AsFloatParameter()!.Value;

                    // Apply the same gain to all samples regardless of channel count
                    // For stereo, this means both left and right channels get the same gain
                    for (int i = 0; i < sampleCount; i++)
                    {
                        float processedSample = inputSamples[i] * gain;
                        outputSamples[i] = ClampToInt16(processedSample);
                    }
                }
            }

            return output;
        }

        private static short ClampToInt16(float value)
        {
            if (value > 32767.0f)
            {
                return 32767;
            }
            else if (value < -32768.0f)
            {
                return -32768;
            }
            else
            {
                return (short)value;
            }
        }
    }
}