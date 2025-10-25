using PiSnoreMonitor.Core.Data;
using System;

namespace PiSnoreMonitor.Extensions
{
    public static class PooledBlockExtensions
    {
        public static (float Left, float Right) CalculateAmplitude(this PooledBlock block, double maximumDbLevel, int channels)
        {
            if (block.Buffer == null || block.Count == 0)
            {
                return (0.0f, 0.0f);
            }

            int sampleCount = block.Count / 2;
            if (sampleCount == 0) return (0.0f, 0.0f);

            if (channels == 1)
            {
                // Mono - calculate single amplitude and return it for both left and right
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

                float amplitude = CalculateAmplitudeFromRms(sum, sampleCount, maximumDbLevel);
                return (amplitude, amplitude);
            }
            else if (channels == 2)
            {
                // Stereo - calculate separate amplitudes for left and right channels
                double leftSum = 0;
                double rightSum = 0;
                int leftSamples = 0;
                int rightSamples = 0;

                unsafe
                {
                    fixed (byte* bufferPtr = block.Buffer)
                    {
                        short* samples = (short*)bufferPtr;

                        for (int i = 0; i < sampleCount; i += 2)
                        {
                            // Left channel (even indices)
                            if (i < sampleCount)
                            {
                                short leftSample = samples[i];
                                leftSum += (double)leftSample * leftSample;
                                leftSamples++;
                            }
                            
                            // Right channel (odd indices)
                            if (i + 1 < sampleCount)
                            {
                                short rightSample = samples[i + 1];
                                rightSum += (double)rightSample * rightSample;
                                rightSamples++;
                            }
                        }
                    }
                }

                float leftAmplitude = leftSamples > 0 ? CalculateAmplitudeFromRms(leftSum, leftSamples, maximumDbLevel) : 0.0f;
                float rightAmplitude = rightSamples > 0 ? CalculateAmplitudeFromRms(rightSum, rightSamples, maximumDbLevel) : 0.0f;
                
                return (leftAmplitude, rightAmplitude);
            }
            else
            {
                // Fallback for unsupported channel counts - treat as mono
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

                float amplitude = CalculateAmplitudeFromRms(sum, sampleCount, maximumDbLevel);
                return (amplitude, amplitude);
            }
        }

        private static float CalculateAmplitudeFromRms(double sumOfSquares, int sampleCount, double maximumDbLevel)
        {
            double rms = Math.Sqrt(sumOfSquares / sampleCount);
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
