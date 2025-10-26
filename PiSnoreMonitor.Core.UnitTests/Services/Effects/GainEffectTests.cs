using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Core.Services.Effects.Parameters;

namespace PiSnoreMonitor.Core.UnitTests.Services.Effects
{
    public class GainEffectTests
    {
        [Fact]
        public void GivenNewGainEffect_WhenConstructorCalled_ThenDefaultParametersAreSet()
        {
            // Arrange & Act
            var effect = new GainEffect();
            var parameters = effect.GetParameters();

            // Assert
            Assert.Single(parameters);
            Assert.Equal("Gain", parameters[0].Name);
            Assert.Equal(1.0f, parameters[0].AsFloatParameter()!.Value);
        }

        [Fact]
        public void GivenGainEffect_AndNewGainParameter_WhenSetParametersCalled_ThenGainIsUpdated()
        {
            // Arrange
            var effect = new GainEffect();
            var newGainParam = new FloatParameter("Gain", 2.5f);

            // Act
            effect.SetParameters(newGainParam);
            var parameters = effect.GetParameters();

            // Assert
            Assert.Equal(2.5f, parameters[0].AsFloatParameter()!.Value);
        }

        [Fact]
        public void GivenGainEffect_AndInvalidParameterName_WhenSetParametersCalled_ThenGainRemainsUnchanged()
        {
            // Arrange
            var effect = new GainEffect();
            var invalidParam = new FloatParameter("InvalidParam", 5.0f);
            var originalParameters = effect.GetParameters();
            var originalValue = originalParameters[0].AsFloatParameter()!.Value;

            // Act
            effect.SetParameters(invalidParam);
            var updatedParameters = effect.GetParameters();

            // Assert
            Assert.Equal(originalValue, updatedParameters[0].AsFloatParameter()!.Value);
        }

        [Fact]
        public void GivenGainEffect_AndInputWithZeroLength_WhenProcessCalled_ThenOriginalBlockIsReturned()
        {
            // Arrange
            var effect = new GainEffect();
            var input = new byte[10];

            // Act
            var result = effect.Process(input, 0, 1);

            // Assert
            Assert.Same(input, result);
        }

        [Fact]
        public void GivenGainEffect_AndUnityGain_AndSineWaveInput_WhenProcessCalled_ThenOutputEqualsInput()
        {
            // Arrange
            var effect = new GainEffect();
            var unityGainParam = new FloatParameter("Gain", 1.0f);
            effect.SetParameters(unityGainParam);

            var input = CreateSineWaveSignal(1000, 44100, 440.0, 0.5);

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            Assert.Equal(input.Length, result.Length);
            Assert.Equal(input, result);
        }

        [Fact]
        public void GivenGainEffect_AndDoubleGain_AndSineWaveInput_WhenProcessCalled_ThenOutputIsDoubledAmplitude()
        {
            // Arrange
            var effect = new GainEffect();
            var doubleGainParam = new FloatParameter("Gain", 2.0f);
            effect.SetParameters(doubleGainParam);

            var input = CreateSineWaveSignal(1000, 44100, 440.0, 0.25); // Quarter amplitude to avoid clipping

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            Assert.Equal(input.Length, result.Length);

            unsafe
            {
                fixed (byte* inputPtr = input)
                fixed (byte* resultPtr = result)
                {
                    short* inputSamples = (short*)inputPtr;
                    short* resultSamples = (short*)resultPtr;
                    int sampleCount = input.Length / 2;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.Equal(inputSamples[i] * 2, resultSamples[i]);
                    }
                }
            }
        }

        [Fact]
        public void GivenGainEffect_AndHalfGain_AndSineWaveInput_WhenProcessCalled_ThenOutputIsHalvedAmplitude()
        {
            // Arrange
            var effect = new GainEffect();
            var halfGainParam = new FloatParameter("Gain", 0.5f);
            effect.SetParameters(halfGainParam);

            var input = CreateSineWaveSignal(1000, 44100, 440.0, 1.0);

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            Assert.Equal(input.Length, result.Length);

            unsafe
            {
                fixed (byte* inputPtr = input)
                fixed (byte* resultPtr = result)
                {
                    short* inputSamples = (short*)inputPtr;
                    short* resultSamples = (short*)resultPtr;
                    int sampleCount = input.Length / 2;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.Equal((short)(inputSamples[i] * 0.5f), resultSamples[i]);
                    }
                }
            }
        }

        [Fact]
        public void GivenGainEffect_AndZeroGain_AndSineWaveInput_WhenProcessCalled_ThenOutputIsSilence()
        {
            // Arrange
            var effect = new GainEffect();
            var zeroGainParam = new FloatParameter("Gain", 0.0f);
            effect.SetParameters(zeroGainParam);

            var input = CreateSineWaveSignal(1000, 44100, 440.0, 1.0);

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            Assert.Equal(input.Length, result.Length);

            unsafe
            {
                fixed (byte* resultPtr = result)
                {
                    short* resultSamples = (short*)resultPtr;
                    int sampleCount = result.Length / 2;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.Equal(0, resultSamples[i]);
                    }
                }
            }
        }

        [Fact]
        public void GivenGainEffect_AndNegativeGain_AndSineWaveInput_WhenProcessCalled_ThenOutputIsInvertedAndScaled()
        {
            // Arrange
            var effect = new GainEffect();
            var negativeGainParam = new FloatParameter("Gain", -1.0f);
            effect.SetParameters(negativeGainParam);

            var input = CreateSineWaveSignal(1000, 44100, 440.0, 0.5);

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            Assert.Equal(input.Length, result.Length);

            unsafe
            {
                fixed (byte* inputPtr = input)
                fixed (byte* resultPtr = result)
                {
                    short* inputSamples = (short*)inputPtr;
                    short* resultSamples = (short*)resultPtr;
                    int sampleCount = input.Length / 2;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.Equal(-inputSamples[i], resultSamples[i]);
                    }
                }
            }
        }

        [Fact]
        public void GivenGainEffect_AndHighGain_AndMaximumInput_WhenProcessCalled_ThenOutputIsClampedToMaximum()
        {
            // Arrange
            var effect = new GainEffect();
            var highGainParam = new FloatParameter("Gain", 10.0f);
            effect.SetParameters(highGainParam);

            // Create signal that will cause positive overflow
            var input = new byte[100 * 2];
            unsafe
            {
                fixed (byte* inputPtr = input)
                {
                    short* samples = (short*)inputPtr;
                    for (int i = 0; i < 100; i++)
                    {
                        samples[i] = 5000; // Will overflow when multiplied by 10
                    }
                }
            }

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            unsafe
            {
                fixed (byte* resultPtr = result)
                {
                    short* resultSamples = (short*)resultPtr;
                    int sampleCount = result.Length / 2;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.Equal(32767, resultSamples[i]); // Clamped to maximum
                    }
                }
            }
        }

        [Fact]
        public void GivenGainEffect_AndHighNegativeGain_AndMinimumInput_WhenProcessCalled_ThenOutputIsClampedToMinimum()
        {
            // Arrange
            var effect = new GainEffect();
            var highNegativeGainParam = new FloatParameter("Gain", -10.0f);
            effect.SetParameters(highNegativeGainParam);

            // Create signal that will cause negative overflow
            var input = new byte[100 * 2];
            unsafe
            {
                fixed (byte* inputPtr = input)
                {
                    short* samples = (short*)inputPtr;
                    for (int i = 0; i < 100; i++)
                    {
                        samples[i] = 5000; // Will underflow when multiplied by -10
                    }
                }
            }

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            unsafe
            {
                fixed (byte* resultPtr = result)
                {
                    short* resultSamples = (short*)resultPtr;
                    int sampleCount = result.Length / 2;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.Equal(-32768, resultSamples[i]); // Clamped to minimum
                    }
                }
            }
        }

        [Fact]
        public void GivenGainEffect_AndMultipleGainChanges_WhenSetParametersCalledMultipleTimes_ThenGainIsUpdatedEachTime()
        {
            // Arrange
            var effect = new GainEffect();
            var input = CreateSineWaveSignal(100, 44100, 440.0, 0.1);

            // Act & Assert - First gain
            var gain1Param = new FloatParameter("Gain", 2.0f);
            effect.SetParameters(gain1Param);
            var result1 = effect.Process(input, input.Length, 1);

            Assert.Equal(2.0f, effect.GetParameters()[0].AsFloatParameter()!.Value);

            // Act & Assert - Second gain
            var gain2Param = new FloatParameter("Gain", 0.5f);
            effect.SetParameters(gain2Param);
            var result2 = effect.Process(input, input.Length, 1);

            Assert.Equal(0.5f, effect.GetParameters()[0].AsFloatParameter()!.Value);

            // Results should be different due to different gains
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void GivenGainEffect_AndVerySmallGain_AndLargeInput_WhenProcessCalled_ThenPrecisionIsPreserved()
        {
            // Arrange
            var effect = new GainEffect();
            var smallGainParam = new FloatParameter("Gain", 0.001f);
            effect.SetParameters(smallGainParam);

            var input = new byte[10 * 2];
            unsafe
            {
                fixed (byte* inputPtr = input)
                {
                    short* samples = (short*)inputPtr;
                    for (int i = 0; i < 10; i++)
                    {
                        samples[i] = 10000; // Large value
                    }
                }
            }

            // Act
            var result = effect.Process(input, input.Length, 1);

            // Assert
            unsafe
            {
                fixed (byte* resultPtr = result)
                {
                    short* resultSamples = (short*)resultPtr;
                    int sampleCount = result.Length / 2;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        Assert.Equal(10, resultSamples[i]); // 10000 * 0.001 = 10
                    }
                }
            }
        }

        private byte[] CreateSineWaveSignal(int sampleCount, int sampleRate, double frequency, double amplitude)
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
                        double value = Math.Sin(2 * Math.PI * frequency * t) * amplitude * 32767;
                        samples[i] = (short)value;
                    }
                }
            }

            return signal;
        }
    }
}