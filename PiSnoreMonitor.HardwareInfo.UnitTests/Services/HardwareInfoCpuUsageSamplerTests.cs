using Moq;
using Hardware.Info;
using PiSnoreMonitor.HardwareInfo.Services;
using Xunit;

namespace PiSnoreMonitor.HardwareInfo.UnitTests.Services
{
    public class HardwareInfoCpuUsageSamplerTests
    {
        [Fact]
        public void GivenHardwareInfo_WhenConstructed_ThenNoExceptionThrown()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();

            // Act & Assert
            var sut = new HardwareInfoCpuUsageSampler(mockHardwareInfo.Object);
            Assert.NotNull(sut);
        }

        [Fact]
        public void GivenNoCpus_WhenGetProcessCpuUsagePercent_ThenZeroReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoCpuUsageSampler(mockHardwareInfo.Object);
            var emptyCpuList = new List<CPU>();

            mockHardwareInfo.Setup(x => x.CpuList)
                .Returns(emptyCpuList);

            // Act
            var result = sut.GetProcessCpuUsagePercent();

            // Assert
            Assert.Equal(0, result);
            mockHardwareInfo.Verify(x => x.RefreshCPUList(true, 500, true), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenSingleCpu_WhenGetProcessCpuUsagePercent_ThenCpuUsageReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoCpuUsageSampler(mockHardwareInfo.Object);
            var cpuList = new List<CPU>
            {
                new CPU { PercentProcessorTime = 25UL }
            };

            mockHardwareInfo.Setup(x => x.CpuList)
                .Returns(cpuList);

            // Act
            var result = sut.GetProcessCpuUsagePercent();

            // Assert
            Assert.Equal(25.0, result);
            mockHardwareInfo.Verify(x => x.RefreshCPUList(true, 500, true), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenMultipleCpus_WhenGetProcessCpuUsagePercent_ThenAverageUsageReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoCpuUsageSampler(mockHardwareInfo.Object);
            var cpuList = new List<CPU>
            {
                new CPU { PercentProcessorTime = 10UL },
                new CPU { PercentProcessorTime = 20UL },
                new CPU { PercentProcessorTime = 30UL },
                new CPU { PercentProcessorTime = 40UL }
            };

            mockHardwareInfo.Setup(x => x.CpuList)
                .Returns(cpuList);

            // Act
            var result = sut.GetProcessCpuUsagePercent();

            // Assert
            Assert.Equal(25.0, result); // Average of 10, 20, 30, 40
            mockHardwareInfo.Verify(x => x.RefreshCPUList(true, 500, true), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenCpusWithHighUsage_WhenGetProcessCpuUsagePercent_ThenCorrectAverageReturned()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoCpuUsageSampler(mockHardwareInfo.Object);
            var cpuList = new List<CPU>
            {
                new CPU { PercentProcessorTime = 96UL },
                new CPU { PercentProcessorTime = 87UL }
            };

            mockHardwareInfo.Setup(x => x.CpuList)
                .Returns(cpuList);

            // Act
            var result = sut.GetProcessCpuUsagePercent();

            // Assert
            Assert.Equal(91.5, result); // Average of 96 and 87
            mockHardwareInfo.Verify(x => x.RefreshCPUList(true, 500, true), Times.Once);
            mockHardwareInfo.VerifyAll();
        }

        [Fact]
        public void GivenHardwareInfo_WhenGetProcessCpuUsagePercent_ThenRefreshCalledWithCorrectParameters()
        {
            // Arrange
            var mockHardwareInfo = new Mock<IHardwareInfo>();
            var sut = new HardwareInfoCpuUsageSampler(mockHardwareInfo.Object);
            var cpuList = new List<CPU>
            {
                new CPU { PercentProcessorTime = 15UL }
            };

            mockHardwareInfo.Setup(x => x.CpuList)
                .Returns(cpuList);

            // Act
            sut.GetProcessCpuUsagePercent();

            // Assert
            mockHardwareInfo.Verify(x => x.RefreshCPUList(
                It.Is<bool>(sleepFirst => sleepFirst == true),
                It.Is<int>(millisecondsDelayBetweenTwoMeasurements => millisecondsDelayBetweenTwoMeasurements == 500),
                It.Is<bool>(normalizeTotalCpuUsage => normalizeTotalCpuUsage == true)
            ), Times.Once);
        }
    }
}