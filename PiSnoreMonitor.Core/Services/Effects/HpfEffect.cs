using PiSnoreMonitor.Core.Services.Effects.Parameters;

namespace PiSnoreMonitor.Core.Services.Effects
{
    public class HpfEffect : IEffect
    {
        private readonly FloatParameter cutoffFrequencyParameter;

        private readonly IntParameter sampleRate;

        // Filter state per channel (supports up to 2 channels)
        private readonly float[] x1 = new float[2]; // Previous input samples per channel
        private readonly float[] x2 = new float[2]; // Previous input samples per channel
        private readonly float[] y1 = new float[2]; // Previous output samples per channel
        private readonly float[] y2 = new float[2]; // Previous output samples per channel
        private float b0;
        private float b1;
        private float b2;
        private float a1;
        private float a2; // Biquad coefficients

        public HpfEffect()
        {
            cutoffFrequencyParameter = new FloatParameter("CutoffFrequency", 100.0f);
            sampleRate = new IntParameter("SampleRate", 44100);

            CalculateFilterCoefficient();
        }

        public List<IEffectsParameter> GetParameters()
        {
            return new List<IEffectsParameter>([cutoffFrequencyParameter]);
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

            if (resetFilterState)
            {
                ResetFilterState();
            }
        }

        public byte[] Process(byte[] block, int length, int channels)
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

                    if (channels == 1)
                    {
                        // Mono processing - use channel 0 filter state
                        for (int i = 0; i < sampleCount; i++)
                        {
                            outputSamples[i] = ProcessSample(inputSamples[i], 0);
                        }
                    }
                    else if (channels == 2)
                    {
                        // Stereo processing - interleaved L, R, L, R, ...
                        for (int i = 0; i < sampleCount; i += 2)
                        {
                            // Left channel (even indices) - use channel 0 filter state
                            if (i < sampleCount)
                            {
                                outputSamples[i] = ProcessSample(inputSamples[i], 0);
                            }

                            // Right channel (odd indices) - use channel 1 filter state
                            if (i + 1 < sampleCount)
                            {
                                outputSamples[i + 1] = ProcessSample(inputSamples[i + 1], 1);
                            }
                        }
                    }
                    else
                    {
                        // Fallback for unsupported channel counts - use channel 0 filter state
                        for (int i = 0; i < sampleCount; i++)
                        {
                            outputSamples[i] = ProcessSample(inputSamples[i], 0);
                        }
                    }
                }
            }

            return output;
        }

        private short ProcessSample(short inputSample, int channel)
        {
            // Convert to float for processing (-1.0 to 1.0 range)
            float inputFloat = inputSample / 32768.0f;

            // Apply biquad high-pass filter
            // y[n] = b0*x[n] + b1*x[n-1] + b2*x[n-2] - a1*y[n-1] - a2*y[n-2]
            float filteredSample = (b0 * inputFloat) + (b1 * x1[channel]) + (b2 * x2[channel]) - (a1 * y1[channel]) - (a2 * y2[channel]);

            // Update filter state for this channel
            x2[channel] = x1[channel];
            x1[channel] = inputFloat;
            y2[channel] = y1[channel];
            y1[channel] = filteredSample;

            // Convert back to 16-bit integer with proper clamping
            float scaledSample = filteredSample * 32768.0f;
            if (scaledSample > 32767.0f)
            {
                scaledSample = 32767.0f;
            }
            else if (scaledSample < -32768.0f)
            {
                scaledSample = -32768.0f;
            }

            return (short)Math.Round(scaledSample);
        }

        private void CalculateFilterCoefficient()
        {
            // Biquad high-pass filter coefficient calculation
            float cutoffFreq = cutoffFrequencyParameter.Value;
            int sampleRate = this.sampleRate.Value;

            if (cutoffFreq <= 0 || sampleRate <= 0)
            {
                // Bypass filter if invalid parameters
                b0 = 1.0f;
                b1 = 0.0f;
                b2 = 0.0f;
                a1 = 0.0f;
                a2 = 0.0f;
                return;
            }

            // Calculate normalized frequency (0 to π)
            float omega = 2.0f * MathF.PI * cutoffFreq / sampleRate;
            float cosOmega = MathF.Cos(omega);
            float sinOmega = MathF.Sin(omega);

            // Q factor for high-pass filter (0.707 for Butterworth response)
            float q = 0.707f;
            float alpha = sinOmega / (2.0f * q);

            // Calculate biquad coefficients for high-pass filter
            float norm = 1.0f + alpha;

            b0 = (1.0f + cosOmega) / 2.0f / norm;
            b1 = -(1.0f + cosOmega) / norm;
            b2 = (1.0f + cosOmega) / 2.0f / norm;
            a1 = -2.0f * cosOmega / norm;
            a2 = (1.0f - alpha) / norm;
        }

        // Reset filter state to prevent artifacts
        private void ResetFilterState()
        {
            for (int i = 0; i < x1.Length; i++)
            {
                x1[i] = x2[i] = 0.0f;
                y1[i] = y2[i] = 0.0f;
            }
        }
    }
}