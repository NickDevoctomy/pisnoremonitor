using PiSnoreMonitor.Core.Extensions;

namespace PiSnoreMonitor.Core.UnitTests.Extensions
{
    public class StreamExtensionsTests
    {
        [Fact]
        public async Task GivenSteam_AndInt_WhenWriteInt32Async_ThenCorrectDataWrittenToStream()
        {
            // Arrange
            using var stream = new MemoryStream();
            int value = 16909060; // 0x01020304

            // Act
            await stream.WriteInt32Async(value);

            // Assert
            var expectedBytes = new byte[] { 0x04, 0x03, 0x02, 0x01 }; // Little-endian
            var actualBytes = stream.ToArray();
            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public async Task GivenSteam_AndShort_WhenWriteInt16Async_ThenCorrectDataWrittenToStream()
        {
            // Arrange
            using var stream = new MemoryStream();
            short value = 258; // 0x0102

            // Act
            await stream.WriteInt16Async(value);

            // Assert
            var expectedBytes = new byte[] { 0x02, 0x01 }; // Little-endian
            var actualBytes = stream.ToArray();
            Assert.Equal(expectedBytes, actualBytes);
        }

        [Fact]
        public async Task GivenSteam_AndString_WhenWriteStringAsync_ThenCorrectDataWrittenToStream()
        {
            // Arrange
            using var stream = new MemoryStream();
            string value = "Hello";
            var encoding = System.Text.Encoding.UTF8;

            // Act
            await stream.WriteStringAsync(value, encoding);

            // Assert
            var expectedBytes = encoding.GetBytes(value);
            var actualBytes = stream.ToArray();
            Assert.Equal(expectedBytes, actualBytes);
        }
    }
}
