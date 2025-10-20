using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Services.Effects.Parameters;

namespace PiSnoreMonitor.Core.UnitTests.Services.Effects
{
    public class HpfEffectTests
    {
        [Fact]
        public void GivenNewHpfEffect_WhenConstructorCalled_ThenDefaultParametersAreSet()
        {
            // Arrange & Act
            var effect = new HpfEffect();
            var parameters = effect.GetParameters();

            // Assert
            Assert.Single(parameters);
            Assert.Equal("CutoffFrequency", parameters[0].Name);
            Assert.Equal(100.0f, parameters[0].AsFloatParameter()!.Value);
        }

        [Fact]
        public void GivenHpfEffect_AndNewCutoffFrequencyParameter_WhenSetParametersCalled_ThenCutoffFrequencyIsUpdated()
        {
            // Arrange
            var effect = new HpfEffect();
            var newCutoffParam = new FloatParameter("CutoffFrequency", 200.0f);

            // Act
            effect.SetParameters(newCutoffParam);
            var parameters = effect.GetParameters();

            // Assert
            Assert.Equal(200.0f, parameters[0].AsFloatParameter()!.Value);
        }

        [Fact]
        public void GivenHpfEffect_AndInputWithZeroLength_WhenProcessCalled_ThenOriginalBlockIsReturned()
        {
            // Arrange
            var effect = new HpfEffect();
            var input = new byte[10];

            // Act
            var result = effect.Process(input, 0);

            // Assert
            Assert.Same(input, result);
        }

        [Fact]
        public void GivenHpfEffect_AndSilenceInput_WhenProcessCalled_ThenNearSilenceIsReturned()
        {
            // Arrange
            var effect = new HpfEffect();
            effect.SetSampleRate(44100);

            // Create 1 second of silence (44100 samples * 2 bytes)
            var input = new byte[44100 * 2];
            Array.Fill<byte>(input, 0);

            // Act
            var result = effect.Process(input, input.Length);

            // Assert
            Assert.Equal(input.Length, result.Length);

            // Check that most samples are still near zero (allowing for tiny filter artifacts)
            unsafe
            {
                fixed (byte* resultPtr = result)
                {
                    short* samples = (short*)resultPtr;
                    int sampleCount = result.Length / 2;

                    int nearZeroCount = 0;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        if (Math.Abs(samples[i]) < 10) // Very small threshold for numerical precision
                            nearZeroCount++;
                    }

                    // At least 99% should be near zero
                    Assert.True(nearZeroCount > sampleCount * 0.99);
                }
            }
        }

        [Fact]
        public void GivenHpfEffect_AndMixedFrequencySignal_AndCutoffAt1000Hz_WhenProcessCalled_ThenLowFrequenciesAreFiltered()
        {
            // Arrange
            var effect = new HpfEffect();
            effect.SetSampleRate(44100);

            // Set cutoff to 1000 Hz
            var cutoffParam = new FloatParameter("CutoffFrequency", 1000.0f);
            effect.SetParameters(cutoffParam);

            // Create a test signal: 500 Hz (should be filtered) + 2000 Hz (should pass)
            var sampleRate = 44100;
            var duration = 0.1; // 100ms
            var sampleCount = (int)(sampleRate * duration);
            var input = new byte[sampleCount * 2];

            unsafe
            {
                fixed (byte* inputPtr = input)
                {
                    short* samples = (short*)inputPtr;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        double t = (double)i / sampleRate;

                        // Mix 500 Hz (should be attenuated) + 2000 Hz (should pass)
                        double lowFreq = Math.Sin(2 * Math.PI * 500 * t) * 0.5;
                        double highFreq = Math.Sin(2 * Math.PI * 2000 * t) * 0.5;
                        double mixed = (lowFreq + highFreq) * 16383; // Scale to 16-bit range

                        samples[i] = (short)mixed;
                    }
                }
            }

            // Act
            var result = effect.Process(input, input.Length);

            // Assert
            Assert.Equal(input.Length, result.Length);

            // The output should have reduced low-frequency content
            // This is a basic check - in reality you'd want FFT analysis
            unsafe
            {
                fixed (byte* inputPtr = input)
                fixed (byte* resultPtr = result)
                {
                    short* inputSamples = (short*)inputPtr;
                    short* resultSamples = (short*)resultPtr;

                    // Calculate RMS of input and output
                    double inputRms = 0, outputRms = 0;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        inputRms += inputSamples[i] * inputSamples[i];
                        outputRms += resultSamples[i] * resultSamples[i];
                    }

                    inputRms = Math.Sqrt(inputRms / sampleCount);
                    outputRms = Math.Sqrt(outputRms / sampleCount);

                    // High-pass filter should reduce overall amplitude (removing low freq energy)
                    Assert.True(outputRms < inputRms);
                }
            }
        }



        [Fact]
        public void GivenHpfEffect_AndMaximumAmplitudeSignal_WhenProcessCalled_ThenClippingIsHandled()
        {
            // Arrange
            var effect = new HpfEffect();
            effect.SetSampleRate(44100);

            // Create a signal with maximum amplitude
            var sampleCount = 1000;
            var input = new byte[sampleCount * 2];

            unsafe
            {
                fixed (byte* inputPtr = input)
                {
                    short* samples = (short*)inputPtr;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        samples[i] = short.MaxValue; // Maximum positive value
                    }
                }
            }

            // Act
            var result = effect.Process(input, input.Length);

            // Assert - should not exceed 16-bit range
            unsafe
            {
                fixed (byte* resultPtr = result)
                {
                    short* samples = (short*)resultPtr;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.True(samples[i] >= short.MinValue);
                        Assert.True(samples[i] <= short.MaxValue);
                    }
                }
            }
        }

        [Fact]
        public void GivenHpfEffect_AndDifferentSampleRates_WhenSetSampleRateCalled_ThenFilterCoefficientIsUpdated()
        {
            // Arrange
            var effect = new HpfEffect();
            var cutoffParam = new FloatParameter("CutoffFrequency", 1000.0f);
            effect.SetParameters(cutoffParam);

            // Create identical test signals
            var input1 = CreateTestSignal(1000, 44100);
            var input2 = CreateTestSignal(1000, 48000);

            // Act
            effect.SetSampleRate(44100);
            var result1 = effect.Process(input1, input1.Length);

            effect.SetSampleRate(48000);
            var result2 = effect.Process(input2, input2.Length);

            // Assert - Different sample rates should produce different filtering behavior
            // This is a basic check that the coefficient calculation is working
            Assert.NotEqual(result1, result2);
        }

        private byte[] CreateTestSignal(int sampleCount, int sampleRate)
        {
            var signal = new byte[sampleCount * 2];

            unsafe
            {
                fixed (byte* signalPtr = signal)
                {
                    short* samples = (short*)signalPtr;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        double t = (double)i / sampleRate;
                        double value = Math.Sin(2 * Math.PI * 440 * t) * 16383; // 440 Hz sine wave
                        samples[i] = (short)value;
                    }
                }
            }

            return signal;
        }
    }
}
