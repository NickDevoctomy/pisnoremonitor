using PiSnoreMonitor.Core.Data;
using System;

namespace PiSnoreMonitor.Extensions
{
    public static class PooledBlockExtensions
    {
        public static float CalculateAmplitude(this PooledBlock block, double maximumDbLevel)
        {
            if (block.Buffer == null || block.Count == 0)
            {
                return 0.0f;
            }

            int sampleCount = block.Count / 2;
            if (sampleCount == 0) return 0.0f;

            double sum = 0;

            unsafe
            {
                fixed (byte* bufferPtr = block.Buffer)
                {
                    short* samples = (short*)bufferPtr;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        short sample = samples[i];
                        sum += (double)sample * sample;
                    }
                }
            }

            double rms = Math.Sqrt(sum / sampleCount);
            double rmsNormalized = rms / 32767.0;

            // Convert to dB: 20 * log10(rmsNormalized)
            // For very small values, clamp to prevent log(0)
            if (rmsNormalized < 1e-10) return 0.0f;

            double dB = 20.0 * Math.Log10(rmsNormalized);
            const double minDb = -60.0;
            double normalizedDb = Math.Max(0.0, (dB - minDb) / (maximumDbLevel - minDb));
            return (float)Math.Min(1.0, Math.Max(0.0, normalizedDb));
        }
    }
}
