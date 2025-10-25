using Microsoft.Extensions.Logging;
using PiSnoreMonitor.Core.Data;
using PiSnoreMonitor.Core.Services.Effects;
using PiSnoreMonitor.Extensions;
using PortAudioSharp;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
{

    public class PortAudioWavRecorder(
        int deviceId,
        int sampleRate,
        int channels,
        uint framesPerBuffer,
        IEffectsBus? effectsBus,
        ILogger<PortAudioWavRecorder> logger) : IWavRecorder
    {
        public event EventHandler<WavRecorderRecordingEventArgs>? WavRecorderRecording;

        private PortAudioSharp.Stream? portAudioStream;
        private FileStream? outputFileStream;
        private Task? writerTask;
        private volatile bool running;
        private volatile bool stopping;
        private long dataBytes;
        private readonly TimeSpan headerRefreshInterval = TimeSpan.FromSeconds(5);
        private DateTime lastHeaderRefreshUtc;
        private bool disposed;
        private readonly Channel<PooledBlock> channel =
            Channel.CreateBounded<PooledBlock>(new BoundedChannelOptions(capacity: 32)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
            });

        // WAV constants for simple PCM header positions (no extra chunks)
        private const int RiffSizeOffset = 4;   // 4 bytes, little-endian
        private const int DataSizeOffset = 40;  // 4 bytes, little-endian

        ~PortAudioWavRecorder()
        {
            Dispose(disposing: false);
        }

        public async Task StartRecordingAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation($"WavRecorder:StartRecordingAsync - {filePath}");
            ObjectDisposedException.ThrowIf(disposed, nameof(PortAudioWavRecorder));

            if (running)
            {
                throw new InvalidOperationException("Already recording.");
            }

            var inputInfo = PortAudio.GetDeviceInfo(deviceId);

            if(inputInfo.maxInputChannels < channels)
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
            
            await WriteWavHeaderAsync(
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
            logger.LogInformation($"WavRecorder:StopRecordingAsync");
            if (!running) return;

            // First signal that we're stopping to allow callbacks to finish gracefully
            stopping = true;
            
            try
            {
                logger.LogInformation($"WavRecorder:StopRecordingAsync - Stopping PortAudio stream.");
                portAudioStream?.Stop();
            }
            catch { /* ignore */ }

            // Add a small delay to allow any pending callbacks to complete
            // This ensures we don't lose audio data that's still being processed
            logger.LogInformation($"WavRecorder:StopRecordingAsync - Allowing drain period for pending audio data.");
            await Task.Delay(100, cancellationToken);

            // Now set running to false to prevent new data from being queued
            running = false;

            try
            {
                logger.LogInformation($"WavRecorder:StopRecordingAsync - Closing PortAudio stream.");
                portAudioStream?.Close();
            }
            catch { /* ignore */ }

            portAudioStream = null;

            logger.LogInformation($"WavRecorder:StopRecordingAsync - Completing channel writer.");
            channel.Writer.TryComplete();
            
            if(writerTask != null)
            {
                logger.LogInformation($"WavRecorder:StopRecordingAsync - Waiting for writer task.");
                try { await writerTask!; } catch { /* ignore */ }
            }

            logger.LogInformation($"WavRecorder:StopRecordingAsync - Closing and flushing output stream.");
            if (outputFileStream != null)
            {
                await PatchHeaderAsync(outputFileStream, dataBytes, cancellationToken);
                await outputFileStream.FlushAsync(cancellationToken);
                outputFileStream.Dispose();
            }

            outputFileStream = null;

            // We do not terminate PortAudio here, as it may be used elsewhere in the app.
            ////if (portAudioIsInitialised)
            ////{
            ////    logger.LogInformation($"WavRecorder:StopRecordingAsync - Shutting down PortAudio.");
            ////    PortAudio.Terminate();
            ////    portAudioIsInitialised = false;
            ////}
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
                            if (outputFileStream == null) return;

                            await outputFileStream.WriteAsync(
                                processedBlock.Buffer.AsMemory(0, processedBlock.Count),
                                cancellationToken);
                            dataBytes += processedBlock.Count;

                            var now = DateTime.UtcNow;
                            if (now - lastHeaderRefreshUtc >= headerRefreshInterval)
                            {
                                await PatchHeaderAsync(
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
            catch(Exception ex)
            {
                Console.WriteLine($"WriterLoop exception: {ex}");
            }
        }

        private static async Task WriteWavHeaderAsync(
            FileStream fileStream,
            int sampleRate,
            int channels,
            int bitsPerSample,
            long dataLength,
            CancellationToken cancellationToken = default)
        {
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);

            await fileStream.WriteStringAsync("RIFF", cancellationToken);
            await fileStream.WriteInt32Async((int)(36 + dataLength), cancellationToken);   // will be patched
            await fileStream.WriteStringAsync("WAVE", cancellationToken);
            await fileStream.WriteStringAsync("fmt ", cancellationToken);
            await fileStream.WriteInt32Async(16, cancellationToken);                       // PCM fmt chunk size
            await fileStream.WriteInt16Async(1, cancellationToken);                        // PCM
            await fileStream.WriteInt16Async((short)channels, cancellationToken);
            await fileStream.WriteInt32Async(sampleRate, cancellationToken);
            await fileStream.WriteInt32Async(byteRate, cancellationToken);
            await fileStream.WriteInt16Async(blockAlign, cancellationToken);
            await fileStream.WriteInt16Async((short)bitsPerSample, cancellationToken);
            await fileStream.WriteStringAsync("data", cancellationToken);
            await fileStream.WriteInt32Async((int)dataLength, cancellationToken);
        }

        private static async Task PatchHeaderAsync(
            FileStream fileStream,
            long dataBytes,
            CancellationToken cancellationToken = default)
        {
            fileStream.Position = DataSizeOffset;
            await fileStream.WriteInt32Async((int)dataBytes, cancellationToken);

            // RIFF size = 36 + dataBytes (for 44-byte header)
            fileStream.Position = RiffSizeOffset;
            await fileStream.WriteInt32Async((int)(36 + dataBytes), cancellationToken);

            fileStream.Position = fileStream.Length;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if(running)
                    {
                        // This should be async proper
                        StopRecordingAsync(CancellationToken.None).GetAwaiter().GetResult();
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