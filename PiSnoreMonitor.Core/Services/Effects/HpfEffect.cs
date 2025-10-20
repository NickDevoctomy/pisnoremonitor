using PiSnoreMonitor.Core.Services.Effects.Parameters;
using PiSnoreMonitor.Services.Effects.Parameters;

namespace PiSnoreMonitor.Core.Services.Effects
{
    public class HpfEffect : IEffect
    {
        private readonly FloatParameter cutoffFrequencyParameter;
        private readonly IntParameter sampleRate;
        
        private float x1 = 0.0f, x2 = 0.0f; // Previous input samples
        private float y1 = 0.0f, y2 = 0.0f; // Previous output samples
        private float b0, b1, b2, a1, a2; // Biquad coefficients

        public HpfEffect()
        {
            cutoffFrequencyParameter = new FloatParameter("CutoffFrequency", 100.0f);
            sampleRate = new IntParameter("SampleRate", 44100);

            CalculateFilterCoefficient();
        }

        public List<IEffectsParameter> GetParameters()
        {
            return [cutoffFrequencyParameter];
        }

        public void SetParameters(params IEffectsParameter[] parameters)
        {
            bool recalculateCoeff = false;
            bool resetFilterState = false;
            
            foreach (var param in parameters)
            {
                if (param is FloatParameter floatParam)
                {
                    switch (floatParam.Name)
                    {
                        case "CutoffFrequency":
                            cutoffFrequencyParameter.Value = floatParam.Value;
                            recalculateCoeff = true;
                            break;
                        case "SampleRate":
                            sampleRate.Value = (int)floatParam.Value;
                            recalculateCoeff = true;
                            resetFilterState = true;
                            break;
                    }
                }
            }
            
            if (recalculateCoeff)
            {
                CalculateFilterCoefficient();
            }

            if(resetFilterState)
            {
                ResetFilterState();
            }
        }

        public byte[] Process(byte[] block, int length)
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
                        // Convert to float for processing (-1.0 to 1.0 range)
                        float inputSample = inputSamples[i] / 32768.0f;
                        float inputAbs = Math.Abs(inputSample);
                        
                        
                        // Apply biquad high-pass filter
                        // y[n] = b0*x[n] + b1*x[n-1] + b2*x[n-2] - a1*y[n-1] - a2*y[n-2]
                        float filteredSample = b0 * inputSample + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                        
                        // Update filter state
                        x2 = x1;
                        x1 = inputSample;
                        y2 = y1;
                        y1 = filteredSample;
                        
                        // Convert back to 16-bit integer with proper clamping
                        float scaledSample = filteredSample * 32768.0f;
                        
                        if (scaledSample > 32767.0f)
                            scaledSample = 32767.0f;
                        else if (scaledSample < -32768.0f)
                            scaledSample = -32768.0f;
                        
                        outputSamples[i] = (short)Math.Round(scaledSample);
                    }
                }
            }
            
            return output;
        }

        private void CalculateFilterCoefficient()
        {
            // Biquad high-pass filter coefficient calculation
            float cutoffFreq = cutoffFrequencyParameter.Value;
            int sampleRate = this.sampleRate.Value;

            if (cutoffFreq <= 0 || sampleRate <= 0)
            {
                // Bypass filter if invalid parameters
                b0 = 1.0f; b1 = 0.0f; b2 = 0.0f;
                a1 = 0.0f; a2 = 0.0f;
                return;
            }
            
            // Calculate normalized frequency (0 to π)
            float omega = 2.0f * MathF.PI * cutoffFreq / sampleRate;
            float cosOmega = MathF.Cos(omega);
            float sinOmega = MathF.Sin(omega);
            
            // Q factor for high-pass filter (0.707 for Butterworth response)
            float Q = 0.707f;
            float alpha = sinOmega / (2.0f * Q);
            
            // Calculate biquad coefficients for high-pass filter
            float norm = 1.0f + alpha;
            
            b0 = (1.0f + cosOmega) / 2.0f / norm;
            b1 = -(1.0f + cosOmega) / norm;
            b2 = (1.0f + cosOmega) / 2.0f / norm;
            a1 = -2.0f * cosOmega / norm;
            a2 = (1.0f - alpha) / norm;
            
            Console.WriteLine($"HpfEffect: Cutoff={cutoffFreq}Hz, SampleRate={sampleRate}Hz");
            Console.WriteLine($"Coefficients: b0={b0:F6}, b1={b1:F6}, b2={b2:F6}, a1={a1:F6}, a2={a2:F6}");
        }

        // Reset filter state to prevent artifacts
        private void ResetFilterState()
        {
            x1 = x2 = 0.0f;
            y1 = y2 = 0.0f;
        }
    }
}