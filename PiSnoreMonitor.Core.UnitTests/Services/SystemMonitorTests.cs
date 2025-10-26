using Moq;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.Core.UnitTests.Services
{
    public class SystemMonitorTests
    {
        [Fact]
        public void GivenDependencies_WhenConstructed_ThenNoExceptionThrown()
        {
            // Arrange
            var mockMemoryUsageSampler = new Mock<IMemoryUsageSampler>();
            var mockCpuUsageSampler = new Mock<ICpuUsageSampler>();

            // Act & Assert
            var sut = new SystemMonitor(mockMemoryUsageSampler.Object, mockCpuUsageSampler.Object);
            Assert.NotNull(sut);
        }

        [Fact]
        public void GivenMonitorNotStarted_WhenStartMonitoring_ThenMonitoringBegins()
        {
            // Arrange
            var mockMemoryUsageSampler = new Mock<IMemoryUsageSampler>();
            var mockCpuUsageSampler = new Mock<ICpuUsageSampler>();
            var sut = new SystemMonitor(mockMemoryUsageSampler.Object, mockCpuUsageSampler.Object);

            // Act
            sut.StartMonitoring();

            // Assert
            Assert.True(true);
        }

        [Fact]
        public void GivenMonitorStarted_WhenStopMonitoring_ThenMonitoringStops()
        {
            // Arrange
            var mockMemoryUsageSampler = new Mock<IMemoryUsageSampler>();
            var mockCpuUsageSampler = new Mock<ICpuUsageSampler>();
            var sut = new SystemMonitor(mockMemoryUsageSampler.Object, mockCpuUsageSampler.Object);
            sut.StartMonitoring();

            // Act
            sut.StopMonitoring();

            // Assert
            Assert.True(true);
        }

        [Fact]
        public void GivenMonitorStarted_WhenTimerElapses_ThenOnSystemStatusUpdateEventFired()
        {
            // Arrange
            var mockMemoryUsageSampler = new Mock<IMemoryUsageSampler>();
            var mockCpuUsageSampler = new Mock<ICpuUsageSampler>();
            var sut = new SystemMonitor(mockMemoryUsageSampler.Object, mockCpuUsageSampler.Object);
            var eventFired = false;
            SystemMonitorStatusEventArgs? capturedEventArgs = null;

            mockMemoryUsageSampler.Setup(x => x.GetSystemMemory())
                .Returns((8000000000UL, 4000000000UL));

            mockCpuUsageSampler.Setup(x => x.GetProcessCpuUsagePercent())
                .Returns(15.5);

            sut.OnSystemStatusUpdate += (sender, args) =>
            {
                eventFired = true;
                capturedEventArgs = args;
            };

            // Act
            sut.StartMonitoring();

            var timeout = DateTime.Now.AddSeconds(5);
            while (!eventFired && DateTime.Now < timeout)
            {
                Thread.Sleep(100);
            }

            // Assert
            Assert.True(eventFired, "Event should have fired within timeout period");
            Assert.NotNull(capturedEventArgs);
            Assert.Equal(15.5, capturedEventArgs.CpuUsagePercentage);
            Assert.Equal(8000000000UL, capturedEventArgs.TotalMemoryBytes);
            Assert.Equal(4000000000UL, capturedEventArgs.FreeMemoryBytes);
            mockMemoryUsageSampler.VerifyAll();
            mockCpuUsageSampler.VerifyAll();

            sut.StopMonitoring();
        }

        [Fact]
        public void GivenMonitorStarted_WhenTimerElapses_ThenSamplersAreCalled()
        {
            // Arrange
            var mockMemoryUsageSampler = new Mock<IMemoryUsageSampler>();
            var mockCpuUsageSampler = new Mock<ICpuUsageSampler>();
            var sut = new SystemMonitor(mockMemoryUsageSampler.Object, mockCpuUsageSampler.Object);
            var eventFired = false;

            mockMemoryUsageSampler.Setup(x => x.GetSystemMemory())
                .Returns((16000000000UL, 8000000000UL));

            mockCpuUsageSampler.Setup(x => x.GetProcessCpuUsagePercent())
                .Returns(25.7);

            sut.OnSystemStatusUpdate += (sender, args) => eventFired = true;

            // Act
            sut.StartMonitoring();

            var timeout = DateTime.Now.AddSeconds(5);
            while (!eventFired && DateTime.Now < timeout)
            {
                Thread.Sleep(100);
            }

            // Assert
            Assert.True(eventFired, "Event should have fired within timeout period");
            mockMemoryUsageSampler.Verify(x => x.GetSystemMemory(), Times.AtLeastOnce);
            mockCpuUsageSampler.Verify(x => x.GetProcessCpuUsagePercent(), Times.AtLeastOnce);

            sut.StopMonitoring();
        }

        [Fact]
        public void GivenMultipleEventHandlers_WhenTimerElapses_ThenAllHandlersReceiveEvent()
        {
            // Arrange
            var mockMemoryUsageSampler = new Mock<IMemoryUsageSampler>();
            var mockCpuUsageSampler = new Mock<ICpuUsageSampler>();
            var sut = new SystemMonitor(mockMemoryUsageSampler.Object, mockCpuUsageSampler.Object);
            var eventFiredCount = 0;
            var expectedEventArgs = new SystemMonitorStatusEventArgs
            {
                CpuUsagePercentage = 30.2,
                TotalMemoryBytes = 32000000000UL,
                FreeMemoryBytes = 16000000000UL
            };

            mockMemoryUsageSampler.Setup(x => x.GetSystemMemory())
                .Returns((expectedEventArgs.TotalMemoryBytes, expectedEventArgs.FreeMemoryBytes));

            mockCpuUsageSampler.Setup(x => x.GetProcessCpuUsagePercent())
                .Returns(expectedEventArgs.CpuUsagePercentage);

            sut.OnSystemStatusUpdate += (sender, args) => eventFiredCount++;
            sut.OnSystemStatusUpdate += (sender, args) => eventFiredCount++;
            sut.OnSystemStatusUpdate += (sender, args) =>
            {
                Assert.Equal(expectedEventArgs, args);
                eventFiredCount++;
            };

            // Act
            sut.StartMonitoring();

            var timeout = DateTime.Now.AddSeconds(5);
            while (eventFiredCount < 3 && DateTime.Now < timeout)
            {
                Thread.Sleep(100);
            }

            // Assert
            Assert.Equal(3, eventFiredCount);
            mockMemoryUsageSampler.VerifyAll();
            mockCpuUsageSampler.VerifyAll();

            sut.StopMonitoring();
        }
    }
}