using PiSnoreMonitor.Services.Effects.Parameters;

namespace PiSnoreMonitor.Core.Services.Effects
{
    public class GainEffect : IEffect
    {
        private readonly IEffectsParameter gainParameter;

        public GainEffect()
        {
            gainParameter = new FloatParameter("Gain", 1.0f);
        }

        public List<IEffectsParameter> GetParameters()
        {
            return [gainParameter];
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
                return block ?? [];

            byte[] output = new byte[length];
            
            unsafe
            {
                fixed (byte* inputPtr = block)
                fixed (byte* outputPtr = output)
                {
                    short* inputSamples = (short*)inputPtr;
                    short* outputSamples = (short*)outputPtr;
                    
                    int sampleCount = length / 2; // 16-bit = 2 bytes per sample
                    
                    for (int i = 0; i < sampleCount; i++)
                    {
                        float processedSample = inputSamples[i] * gainParameter.AsFloatParameter()!.Value;
                        
                        // Clamp to 16-bit range to prevent overflow
                        if (processedSample > 32767.0f)
                            processedSample = 32767.0f;
                        else if (processedSample < -32768.0f)
                            processedSample = -32768.0f;
                        
                        outputSamples[i] = (short)processedSample;
                    }
                }
            }
            
            return output;
        }
    }
}