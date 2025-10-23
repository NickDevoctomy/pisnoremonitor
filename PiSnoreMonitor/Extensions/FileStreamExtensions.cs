using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Extensions
{
    public static class FileStreamExtensions
    {
        public static async Task WriteInt32Async(
            this FileStream fs,
            int value,
            CancellationToken cancellationToken = default)
        {
            var bytes = BitConverter.GetBytes(value);
            await fs.WriteAsync(bytes.AsMemory(0, 4), cancellationToken);
        }

        public static async Task WriteInt16Async(
            this FileStream fs,
            short value,
            CancellationToken cancellationToken = default)
        {
            var bytes = BitConverter.GetBytes(value);
            await fs.WriteAsync(bytes.AsMemory(0, 2), cancellationToken);
        }

        public static async Task WriteStringAsync(
            this FileStream fs,
            string value,
            CancellationToken cancellationToken = default)
        {
            var bytes = System.Text.Encoding.ASCII.GetBytes(value);
            await fs.WriteAsync(bytes, cancellationToken);
        }
    }
}
