using PiSnoreMonitor.Core.Data;
using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Services.Effects.Parameters;
using System.Text;

namespace PiSnoreMonitor.Core.UnitTests.Services.Effects
{
    public class ManualTests
    {

        [Theory]
        [InlineData("Data/Wavs/Unprocessed1.wav", "Data/Output/HpfEffect-Processed1.wav")]
        public void GivenWavFile_WhenHpfEffectApplied_ThenOutputIsFiltered(
            string inputFileName,
            string outputFileName)
        {
            // Arrange
            var sut = new HpfEffect();
            
            // Set up HpfEffect with specific parameters for testing
            var cutoffParam = new FloatParameter("CutoffFrequency", 12.0f);
            var sampleRateParam = new FloatParameter("SampleRate", 44100);
            sut.SetParameters(cutoffParam, sampleRateParam);

            // Load WAV file
            var wavData = LoadWavFile(inputFileName);
            
            // Act
            // Process the audio data through the HpfEffect
            var processedData = sut.Process(wavData.AudioData, wavData.AudioData.Length);
            
            // Create output WAV with same format but processed audio data
            var outputWavData = new WavData
            {
                SampleRate = wavData.SampleRate,
                Channels = wavData.Channels,
                BitsPerSample = wavData.BitsPerSample,
                AudioData = processedData
            };
            
            // Save processed WAV file
            SaveWavFile(outputFileName, outputWavData);
            
            // Assert
            // Manual verification - check that files exist and have expected properties
            Assert.True(File.Exists(inputFileName), $"Input file {inputFileName} should exist");
            Assert.True(File.Exists(outputFileName), $"Output file {outputFileName} should be created");
            
            // Verify output file is not empty and has reasonable size
            var inputFileInfo = new FileInfo(inputFileName);
            var outputFileInfo = new FileInfo(outputFileName);
            
            Assert.True(outputFileInfo.Length > 44, "Output file should contain WAV header + data");
            //Assert.True(Math.Abs(outputFileInfo.Length - inputFileInfo.Length) < 100, 
            //    "Output file should be similar size to input file");
            
            Console.WriteLine($"Processed {inputFileName} -> {outputFileName}");
            Console.WriteLine($"Input size: {inputFileInfo.Length} bytes");
            Console.WriteLine($"Output size: {outputFileInfo.Length} bytes");
            Console.WriteLine($"WAV Info: {wavData.SampleRate}Hz, {wavData.Channels} channels, {wavData.BitsPerSample} bits");
        }

        [Theory]
        [InlineData("Data/Wavs/Unprocessed2.wav", "Data/Output/EffectsBus-Processed2.wav")]
        public void GivenWavFile_WhenEffectsBusApplied_ThenOutputIsFiltered(
            string inputFileName,
            string outputFileName)
        {
            // Arrange
            var hpfEffect = new HpfEffect();
            var cutoffParam = new FloatParameter("CutoffFrequency", 60.0f);
            var sampleRateParam = new FloatParameter("SampleRate", 44100);
            hpfEffect.SetParameters(cutoffParam, sampleRateParam);

            var gainEffect = new GainEffect();
            var gainParam = new FloatParameter("Gain", 2.0f);
            gainEffect.SetParameters(gainParam);

            var sut = new EffectsBus();
            sut.Effects.Add(hpfEffect);
            sut.Effects.Add(gainEffect);

            // Load WAV file
            var wavData = LoadWavFile(inputFileName);

            // Act
            // Create PooledBlock from WAV data
            var inputBlock = new PooledBlock
            {
                Buffer = wavData.AudioData,
                Count = wavData.AudioData.Length
            };

            // Process the audio data through the EffectsBus
            var processedBlock = sut.Process(inputBlock, wavData.AudioData.Length);

            // Create output WAV with same format but processed audio data
            var outputWavData = new WavData
            {
                SampleRate = wavData.SampleRate,
                Channels = wavData.Channels,
                BitsPerSample = wavData.BitsPerSample,
                AudioData = processedBlock.Buffer
            };

            // Save processed WAV file
            SaveWavFile(outputFileName, outputWavData);

            // Assert
            // Manual verification - check that files exist and have expected properties
            Assert.True(File.Exists(inputFileName), $"Input file {inputFileName} should exist");
            Assert.True(File.Exists(outputFileName), $"Output file {outputFileName} should be created");

            // Verify output file is not empty and has reasonable size
            var inputFileInfo = new FileInfo(inputFileName);
            var outputFileInfo = new FileInfo(outputFileName);

            Assert.True(outputFileInfo.Length > 44, "Output file should contain WAV header + data");

            Console.WriteLine($"Processed {inputFileName} -> {outputFileName}");
            Console.WriteLine($"Input size: {inputFileInfo.Length} bytes");
            Console.WriteLine($"Output size: {outputFileInfo.Length} bytes");
            Console.WriteLine($"WAV Info: {wavData.SampleRate}Hz, {wavData.Channels} channels, {wavData.BitsPerSample} bits");
        }

        private WavData LoadWavFile(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // Read WAV header
            var riff = Encoding.ASCII.GetString(br.ReadBytes(4)); // "RIFF"
            if (riff != "RIFF") throw new InvalidDataException("Not a valid WAV file - missing RIFF header");

            var fileSize = br.ReadInt32(); // File size
            var wave = Encoding.ASCII.GetString(br.ReadBytes(4)); // "WAVE"
            if (wave != "WAVE") throw new InvalidDataException("Not a valid WAV file - missing WAVE header");

            var fmt = Encoding.ASCII.GetString(br.ReadBytes(4)); // "fmt "
            if (fmt != "fmt ") throw new InvalidDataException("Not a valid WAV file - missing fmt chunk");

            var fmtSize = br.ReadInt32(); // Format chunk size
            var audioFormat = br.ReadInt16(); // Audio format (1 = PCM)
            var channels = br.ReadInt16(); // Number of channels
            var sampleRate = br.ReadInt32(); // Sample rate
            var byteRate = br.ReadInt32(); // Byte rate
            var blockAlign = br.ReadInt16(); // Block align
            var bitsPerSample = br.ReadInt16(); // Bits per sample

            // Skip any extra format bytes
            if (fmtSize > 16)
            {
                br.ReadBytes(fmtSize - 16);
            }

            // Find data chunk
            while (fs.Position < fs.Length - 8)
            {
                var chunkId = Encoding.ASCII.GetString(br.ReadBytes(4));
                var chunkSize = br.ReadInt32();

                if (chunkId == "data")
                {
                    // Read audio data
                    var audioData = br.ReadBytes(chunkSize);
                    
                    return new WavData
                    {
                        SampleRate = sampleRate,
                        Channels = channels,
                        BitsPerSample = bitsPerSample,
                        AudioData = audioData
                    };
                }
                else
                {
                    // Skip this chunk
                    fs.Seek(chunkSize, SeekOrigin.Current);
                }
            }

            throw new InvalidDataException("No data chunk found in WAV file");
        }

        private void SaveWavFile(string filePath, WavData wavData)
        {
            // Ensure output directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            // Calculate sizes
            var dataSize = wavData.AudioData.Length;
            var fileSize = 36 + dataSize;
            var byteRate = wavData.SampleRate * wavData.Channels * wavData.BitsPerSample / 8;
            var blockAlign = (short)(wavData.Channels * wavData.BitsPerSample / 8);

            // Write WAV header
            bw.Write(Encoding.ASCII.GetBytes("RIFF")); // ChunkID
            bw.Write(fileSize); // ChunkSize
            bw.Write(Encoding.ASCII.GetBytes("WAVE")); // Format

            // Write fmt sub-chunk
            bw.Write(Encoding.ASCII.GetBytes("fmt ")); // Subchunk1ID
            bw.Write(16); // Subchunk1Size (16 for PCM)
            bw.Write((short)1); // AudioFormat (1 = PCM)
            bw.Write((short)wavData.Channels); // NumChannels
            bw.Write(wavData.SampleRate); // SampleRate
            bw.Write(byteRate); // ByteRate
            bw.Write(blockAlign); // BlockAlign
            bw.Write((short)wavData.BitsPerSample); // BitsPerSample

            // Write data sub-chunk
            bw.Write(Encoding.ASCII.GetBytes("data")); // Subchunk2ID
            bw.Write(dataSize); // Subchunk2Size
            bw.Write(wavData.AudioData); // The actual audio data
        }

        private class WavData
        {
            public int SampleRate { get; set; }
            public int Channels { get; set; }
            public int BitsPerSample { get; set; }
            public byte[] AudioData { get; set; } = [];
        }
    }
}
