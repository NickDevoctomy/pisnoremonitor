using Hardware.Info;
using Moq;
using PiSnoreMonitor.HardwareInfo.Services;
using Xunit;

namespace PiSnoreMonitor.HardwareInfo.UnitTests.Services
{
    public class HardwareInfoMemoryUsageSamplerTests
    {
        [Fact]
        public void GivenHardwareInfo_WhenConstructed_ThenNoExceptionThrown()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();

            // Act & Assert
            var sut = new HardwareInfoMemoryUsageSampler(mockHardwareInfo.Object);
            Assert.NotNull(sut);
        }

        [Fact]
        public void GivenMemoryStatus_WhenGetSystemMemory_ThenCorrectValuesReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoMemoryUsageSampler(mockHardwareInfo.Object);
            var memoryStatus = new MemoryStatus
            {
                TotalPhysical = 16000000000UL, // 16GB
                AvailablePhysical = 8000000000UL // 8GB
            };

            mockHardwareInfo.Setup(x => x.MemoryStatus)
                .Returns(memoryStatus);

            // Act
            var (totalBytes, freeBytes) = sut.GetSystemMemory();

            // Assert
            Assert.Equal(16000000000UL, totalBytes);
            Assert.Equal(8000000000UL, freeBytes);
            mockHardwareInfo.Verify(x => x.RefreshMemoryStatus(), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenLowMemorySystem_WhenGetSystemMemory_ThenCorrectValuesReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoMemoryUsageSampler(mockHardwareInfo.Object);
            var memoryStatus = new MemoryStatus
            {
                TotalPhysical = 4000000000UL, // 4GB
                AvailablePhysical = 1000000000UL // 1GB
            };

            mockHardwareInfo.Setup(x => x.MemoryStatus)
                .Returns(memoryStatus);

            // Act
            var (totalBytes, freeBytes) = sut.GetSystemMemory();

            // Assert
            Assert.Equal(4000000000UL, totalBytes);
            Assert.Equal(1000000000UL, freeBytes);
            mockHardwareInfo.Verify(x => x.RefreshMemoryStatus(), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenHighMemorySystem_WhenGetSystemMemory_ThenCorrectValuesReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoMemoryUsageSampler(mockHardwareInfo.Object);
            var memoryStatus = new MemoryStatus
            {
                TotalPhysical = 64000000000UL, // 64GB
                AvailablePhysical = 32000000000UL // 32GB
            };

            mockHardwareInfo.Setup(x => x.MemoryStatus)
                .Returns(memoryStatus);

            // Act
            var (totalBytes, freeBytes) = sut.GetSystemMemory();

            // Assert
            Assert.Equal(64000000000UL, totalBytes);
            Assert.Equal(32000000000UL, freeBytes);
            mockHardwareInfo.Verify(x => x.RefreshMemoryStatus(), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenFullyUsedMemory_WhenGetSystemMemory_ThenZeroFreeMemoryReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoMemoryUsageSampler(mockHardwareInfo.Object);
            var memoryStatus = new MemoryStatus
            {
                TotalPhysical = 8000000000UL, // 8GB
                AvailablePhysical = 0UL // 0GB free
            };

            mockHardwareInfo.Setup(x => x.MemoryStatus)
                .Returns(memoryStatus);

            // Act
            var (totalBytes, freeBytes) = sut.GetSystemMemory();

            // Assert
            Assert.Equal(8000000000UL, totalBytes);
            Assert.Equal(0UL, freeBytes);
            mockHardwareInfo.Verify(x => x.RefreshMemoryStatus(), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenCompletelyFreeMemory_WhenGetSystemMemory_ThenAllMemoryFreeReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoMemoryUsageSampler(mockHardwareInfo.Object);
            var memoryStatus = new MemoryStatus
            {
                TotalPhysical = 32000000000UL, // 32GB
                AvailablePhysical = 32000000000UL // 32GB free (unrealistic but valid test case)
            };

            mockHardwareInfo.Setup(x => x.MemoryStatus)
                .Returns(memoryStatus);

            // Act
            var (totalBytes, freeBytes) = sut.GetSystemMemory();

            // Assert
            Assert.Equal(32000000000UL, totalBytes);
            Assert.Equal(32000000000UL, freeBytes);
            mockHardwareInfo.Verify(x => x.RefreshMemoryStatus(), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenHardwareInfo_WhenGetSystemMemory_ThenRefreshMemoryStatusCalled()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoMemoryUsageSampler(mockHardwareInfo.Object);
            var memoryStatus = new MemoryStatus
            {
                TotalPhysical = 16000000000UL,
                AvailablePhysical = 8000000000UL
            };

            mockHardwareInfo.Setup(x => x.MemoryStatus)
                .Returns(memoryStatus);

            // Act
            sut.GetSystemMemory();

            // Assert
            mockHardwareInfo.Verify(x => x.RefreshMemoryStatus(), Times.Once);
        }
    }
}