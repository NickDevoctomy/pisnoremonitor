using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Data;
using PortAudioSharp;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
{

    public class WavRecorder(
        int sampleRate,
        int channels,
        uint framesPerBuffer,
        IEffectsBus? effectsBus) : IWavRecorder
    {
        public event EventHandler<WavRecorderRecordingEventArgs>? WavRecorderRecording;

        private PortAudioSharp.Stream? paStream;
        private FileStream? fs;
        private BinaryWriter? bw;
        private Task? writerTask;
        private bool paInitialized;
        private volatile bool running;
        private long dataBytes;
        private readonly TimeSpan headerRefreshInterval = TimeSpan.FromSeconds(5);
        private DateTime lastHeaderRefreshUtc;
        private bool disposed;
        private readonly Channel<PooledBlock> channel =
            Channel.CreateBounded<PooledBlock>(new BoundedChannelOptions(capacity: 8)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

        // WAV constants for simple PCM header positions (no extra chunks)
        private const int RiffSizeOffset = 4;   // 4 bytes, little-endian
        private const int DataSizeOffset = 40;  // 4 bytes, little-endian

        ~WavRecorder()
        {
            Dispose(disposing: false);
        }

        public void StartRecording(string filePath)
        {
            ObjectDisposedException.ThrowIf(disposed, nameof(WavRecorder));

            if (running)
            {
                throw new InvalidOperationException("Already recording.");
            }

            PortAudio.Initialize();
            paInitialized = true;

            var inputDevice = PortAudio.DefaultInputDevice;
            var inputInfo = PortAudio.GetDeviceInfo(inputDevice);

            var inputParams = new StreamParameters
            {
                device = inputDevice,
                channelCount = channels,
                sampleFormat = SampleFormat.Int16,
                suggestedLatency = inputInfo.defaultLowInputLatency,
                hostApiSpecificStreamInfo = nint.Zero
            };

            fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read,
                                 bufferSize: 1 << 16, FileOptions.None);
            bw = new BinaryWriter(fs);
            WriteWavHeader(bw, sampleRate, channels, bitsPerSample: 16, dataLength: 0);
            bw.Flush();

            dataBytes = 0;
            lastHeaderRefreshUtc = DateTime.UtcNow;
            running = true;

            writerTask = Task.Run(WriterLoop);

            paStream = new PortAudioSharp.Stream(
                inputParams,
                null,
                sampleRate,
                framesPerBuffer,
                StreamFlags.NoFlag,
                OutputStreamCallback,
                userData: nint.Zero);

            paStream.Start();
        }

        public void StopRecording()
        {
            if (!running) return;

            try
            {
                paStream?.Stop();
            }
            catch { /* ignore */ }

            try
            {
                paStream?.Close();
            }
            catch { /* ignore */ }

            paStream = null;

            running = false;
            channel.Writer.TryComplete();
            try { writerTask?.Wait(); } catch { /* ignore */ }

            if (bw != null && fs != null)
            {
                PatchHeader(bw, fs, dataBytes);
                bw.Flush();
                fs.Flush(true);
                bw.Dispose();
                fs.Dispose();
            }

            bw = null;
            fs = null;

            if (paInitialized)
            {
                PortAudio.Terminate();
                paInitialized = false;
            }
        }

        private StreamCallbackResult OutputStreamCallback(
            nint input,
            nint output,
            uint frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            nint userDataPtr)
        {
            if (input == nint.Zero || !running || channels <= 0)
            {
                return StreamCallbackResult.Continue;
            }

            int bytesPerSample = 2;
            int bytesNeeded = checked((int)frameCount * channels * bytesPerSample);

            var block = new PooledBlock
            {
                Buffer = ArrayPool<byte>.Shared.Rent(bytesNeeded),
                Count = bytesNeeded
            };

            // Copy from unmanaged input pointer to managed buffer
            unsafe
            {
                Buffer.MemoryCopy(
                    source: (void*)input,
                    destination: Unsafe.AsPointer(ref block.Buffer[0]),
                    destinationSizeInBytes: block.Buffer.Length,
                    sourceBytesToCopy: bytesNeeded);
            }

            // Try to enqueue. If channel is full, drop oldest (per config) and still enqueue current.
            channel.Writer.TryWrite(block);

            return StreamCallbackResult.Continue;
        }

        private async Task WriterLoop()
        {
            try
            {
                while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out var block))
                    {
                        var processedBlock = effectsBus?.Process(block, block.Count) ?? block;

                        float amplitude = CalculateAmplitude(processedBlock, 0);
                        System.Diagnostics.Debug.WriteLine($"Firing event with amplitude: {amplitude:F3}");
                        WavRecorderRecording?.Invoke(this, new WavRecorderRecordingEventArgs
                        {
                            Amplitude = amplitude
                        });

                        try
                        {
                            if (bw == null || fs == null) return;

                            bw.Write(processedBlock.Buffer, 0, processedBlock.Count);
                            dataBytes += processedBlock.Count;

                            // Periodically patch header & fsync to minimize corruption on power loss
                            var now = DateTime.UtcNow;
                            if (now - lastHeaderRefreshUtc >= headerRefreshInterval)
                            {
                                PatchHeader(bw, fs, dataBytes);
                                bw.Flush();
                                fs.Flush(true);
                                lastHeaderRefreshUtc = now;
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(processedBlock.Buffer);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"WriterLoop exception: {ex}");
            }
        }

        private float CalculateAmplitude(PooledBlock block, double maximumDbLevel)
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

        private static void WriteWavHeader(
            BinaryWriter bw,
            int sampleRate,
            int channels,
            int bitsPerSample,
            long dataLength)
        {
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write((int)(36 + dataLength));   // will be patched
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);                       // PCM fmt chunk size
            bw.Write((short)1);                 // PCM
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write((short)bitsPerSample);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write((int)dataLength);
        }

        private static void PatchHeader(
            BinaryWriter bw,
            FileStream fs,
            long dataBytes)
        {
            fs.Position = DataSizeOffset;
            bw.Write((int)dataBytes);

            // RIFF size = 36 + dataBytes (for 44-byte header)
            fs.Position = RiffSizeOffset;
            bw.Write((int)(36 + dataBytes));

            fs.Position = fs.Length;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if(running)
                    {
                        StopRecording();
                    }

                    writerTask = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}