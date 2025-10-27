using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PiSnoreMonitor.Core.Data;
using PiSnoreMonitor.Core.Extensions;
using PiSnoreMonitor.Core.Services;
using PiSnoreMonitor.Core.Services.Effects;
using PortAudioSharp;

namespace PiSnoreMonitor.PortAudio.Services
{
    [ExcludeFromCodeCoverage(Justification = "Not going to attempt to abstract out PortAudio.")]
    public class PortAudioRF64WavRecorder(
        int deviceId,
        int sampleRate,
        int channels,
        uint framesPerBuffer,
        IEffectsBus? effectsBus,
        ILogger<PortAudioRF64WavRecorder> logger) : IWavRecorder
    {
        // RF64 constants for unlimited file size support
        private const int RF64HeaderSize = 80;          // Total RF64 header size
        private const int DS64RiffSizeOffset = 20;      // 8 bytes, little-endian
        private const int DS64DataSizeOffset = 28;      // 8 bytes, little-endian

        private readonly TimeSpan headerRefreshInterval = TimeSpan.FromSeconds(5);
        private readonly Channel<PooledBlock> channel =
            Channel.CreateBounded<PooledBlock>(new BoundedChannelOptions(capacity: 32)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
            });

        private PortAudioSharp.Stream? portAudioStream;
        private FileStream? outputFileStream;
        private Task? writerTask;
        private volatile bool running;
        private volatile bool stopping;
        private long dataBytes;
        private DateTime lastHeaderRefreshUtc;
        private bool disposed;

        ~PortAudioRF64WavRecorder()
        {
            Dispose(disposing: false);
        }

        public event EventHandler<WavRecorderRecordingEventArgs>? WavRecorderRecording;

        public async Task StartRecordingAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation("RF64WavRecorder:StartRecordingAsync - {filePath}", filePath);
            ObjectDisposedException.ThrowIf(disposed, nameof(PortAudioRF64WavRecorder));

            if (running)
            {
                throw new InvalidOperationException("Already recording.");
            }

            var inputInfo = PortAudioSharp.PortAudio.GetDeviceInfo(deviceId);

            if (inputInfo.maxInputChannels < channels)
            {
                channels = inputInfo.maxInputChannels;
            }

            var inputParams = new StreamParameters
            {
                device = deviceId,
                channelCount = channels,
                sampleFormat = SampleFormat.Int16,
                suggestedLatency = inputInfo.defaultLowInputLatency,
                hostApiSpecificStreamInfo = nint.Zero
            };

            outputFileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 1 << 16,
                FileOptions.Asynchronous);

            await WriteRF64HeaderAsync(
                outputFileStream,
                sampleRate,
                channels,
                bitsPerSample: 16,
                dataLength: 0,
                cancellationToken);
            await outputFileStream.FlushAsync(cancellationToken);

            dataBytes = 0;
            lastHeaderRefreshUtc = DateTime.UtcNow;
            running = true;
            stopping = false;

            writerTask = Task.Run(
                () => WriterLoop(cancellationToken),
                cancellationToken);

            portAudioStream = new PortAudioSharp.Stream(
                inputParams,
                null,
                sampleRate,
                framesPerBuffer,
                StreamFlags.NoFlag,
                OutputStreamCallback,
                userData: nint.Zero);

            portAudioStream.Start();
        }

        public async Task StopRecordingAsync(CancellationToken cancellationToken = default)
        {
            logger.LogInformation($"RF64WavRecorder:StopRecordingAsync");
            if (!running)
            {
                return;
            }

            // First signal that we're stopping to allow callbacks to finish gracefully
            stopping = true;

            try
            {
                logger.LogInformation($"RF64WavRecorder:StopRecordingAsync - Stopping PortAudio stream.");
                portAudioStream?.Stop();
            }
            catch
            {
                /* ignore */
            }

            // Add a small delay to allow any pending callbacks to complete
            // This ensures we don't lose audio data that's still being processed
            logger.LogInformation($"RF64WavRecorder:StopRecordingAsync - Allowing drain period for pending audio data.");
            await Task.Delay(100, cancellationToken);

            // Now set running to false to prevent new data from being queued
            running = false;

            try
            {
                logger.LogInformation($"RF64WavRecorder:StopRecordingAsync - Closing PortAudio stream.");
                portAudioStream?.Close();
            }
            catch
            {
                /* ignore */
            }

            portAudioStream = null;

            logger.LogInformation($"RF64WavRecorder:StopRecordingAsync - Completing channel writer.");
            channel.Writer.TryComplete();

            if (writerTask != null)
            {
                logger.LogInformation($"RF64WavRecorder:StopRecordingAsync - Waiting for writer task.");
                try
                {
                    await writerTask!;
                }
                catch
                {
                    /* ignore */
                }
            }

            logger.LogInformation($"RF64WavRecorder:StopRecordingAsync - Closing and flushing output stream.");
            if (outputFileStream != null)
            {
                await PatchRF64HeaderAsync(outputFileStream, dataBytes, cancellationToken);
                await outputFileStream.FlushAsync(cancellationToken);
                outputFileStream.Dispose();
            }

            outputFileStream = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (running)
                    {
                        // This should be async proper
                        StopRecordingAsync(CancellationToken.None).GetAwaiter().GetResult();
                    }

                    writerTask = null;
                }

                disposed = true;
            }
        }

        private static async Task WriteRF64HeaderAsync(
            FileStream fileStream,
            int sampleRate,
            int channels,
            int bitsPerSample,
            long dataLength,
            CancellationToken cancellationToken = default)
        {
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);

            // RF64 header (replaces RIFF)
            await fileStream.WriteStringAsync("RF64", System.Text.Encoding.ASCII, cancellationToken);
            await fileStream.WriteUInt32Async(0xFFFFFFFF, cancellationToken);  // RF64 placeholder (-1 as uint32)
            await fileStream.WriteStringAsync("WAVE", System.Text.Encoding.ASCII, cancellationToken);

            // ds64 chunk MUST come immediately after WAVE, before fmt
            await fileStream.WriteStringAsync("ds64", System.Text.Encoding.ASCII, cancellationToken);
            await fileStream.WriteInt32Async(28, cancellationToken);  // ds64 chunk size (28 bytes)
            await fileStream.WriteInt64Async(72 + dataLength, cancellationToken);  // RF64 file size (header - 8 + data)
            await fileStream.WriteInt64Async(dataLength, cancellationToken);       // data chunk size as 64-bit
            await fileStream.WriteInt64Async(0, cancellationToken);                // sample count (0 = not used)
            await fileStream.WriteInt32Async(0, cancellationToken);                // table length (no additional chunks)

            // fmt chunk (standard WAV format chunk)
            await fileStream.WriteStringAsync("fmt ", System.Text.Encoding.ASCII, cancellationToken);
            await fileStream.WriteInt32Async(16, cancellationToken);                       // PCM fmt chunk size
            await fileStream.WriteInt16Async(1, cancellationToken);                        // PCM format
            await fileStream.WriteInt16Async((short)channels, cancellationToken);
            await fileStream.WriteInt32Async(sampleRate, cancellationToken);
            await fileStream.WriteInt32Async(byteRate, cancellationToken);
            await fileStream.WriteInt16Async(blockAlign, cancellationToken);
            await fileStream.WriteInt16Async((short)bitsPerSample, cancellationToken);

            // data chunk header
            await fileStream.WriteStringAsync("data", System.Text.Encoding.ASCII, cancellationToken);
            await fileStream.WriteUInt32Async(0xFFFFFFFF, cancellationToken);  // RF64 placeholder (-1 as uint32)
        }

        private static async Task PatchRF64HeaderAsync(
            FileStream fileStream,
            long dataBytes,
            CancellationToken cancellationToken = default)
        {
            // Update ds64 chunk with 64-bit values
            fileStream.Position = DS64RiffSizeOffset;
            await fileStream.WriteInt64Async(72 + dataBytes, cancellationToken);  // RF64 file size

            fileStream.Position = DS64DataSizeOffset;
            await fileStream.WriteInt64Async(dataBytes, cancellationToken);       // data size

            fileStream.Position = fileStream.Length;
        }

        private StreamCallbackResult OutputStreamCallback(
            nint input,
            nint output,
            uint frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            nint userDataPtr)
        {
            // Allow processing during stopping phase to capture remaining audio data
            if (input == nint.Zero || (!running && !stopping) || channels <= 0)
            {
                return StreamCallbackResult.Continue;
            }

            // If we're stopping and no longer running, signal completion
            if (!running && stopping)
            {
                return StreamCallbackResult.Complete;
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

        private async Task WriterLoop(CancellationToken cancellationToken = default)
        {
            try
            {
                while (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (channel.Reader.TryRead(out var block))
                    {
                        var processedBlock = effectsBus?.Process(block, block.Count, channels) ?? block;
                        WavRecorderRecording?.Invoke(this, new WavRecorderRecordingEventArgs(processedBlock));

                        try
                        {
                            if (outputFileStream == null)
                            {
                                return;
                            }

                            await outputFileStream.WriteAsync(
                                processedBlock.Buffer.AsMemory(0, processedBlock.Count),
                                cancellationToken);
                            dataBytes += processedBlock.Count;

                            var now = DateTime.UtcNow;
                            if (now - lastHeaderRefreshUtc >= headerRefreshInterval)
                            {
                                await PatchRF64HeaderAsync(
                                    outputFileStream,
                                    dataBytes,
                                    cancellationToken);
                                await outputFileStream.FlushAsync(cancellationToken);
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
            catch (Exception ex)
            {
                Console.WriteLine($"WriterLoop exception: {ex}");
            }
        }
    }
}