namespace PiSnoreMonitor.Core.Extensions
{
    public static class StreamExtensions
    {
        public static async Task WriteInt32Async(
            this Stream stream,
            int value,
            CancellationToken cancellationToken = default)
        {
            var bytes = BitConverter.GetBytes(value);
            await stream.WriteAsync(bytes.AsMemory(0, 4), cancellationToken);
        }

        public static async Task WriteInt16Async(
            this Stream stream,
            short value,
            CancellationToken cancellationToken = default)
        {
            var bytes = BitConverter.GetBytes(value);
            await stream.WriteAsync(bytes.AsMemory(0, 2), cancellationToken);
        }

        public static async Task WriteStringAsync(
            this Stream stream,
            string value,
            System.Text.Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            var bytes = encoding.GetBytes(value);
            await stream.WriteAsync(bytes, cancellationToken);
        }
    }
}
