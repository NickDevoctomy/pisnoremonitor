using Moq;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.Core.UnitTests.Services
{
    public class SideCarWriterServiceTests
    {
        [Fact]
        public async Task GivenValidFilePath_WhenStartRecordingAsync_ThenSideCarInfoCreatedAndSaved()
        {
            // Arrange
            var mockIoService = new Mock<IIoService>();
            var sut = new SideCarWriterService(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var filePath = "C:\\TestPath\\recording.sidecar";

            mockIoService.Setup(x => x.WriteAllTextAsync(
                It.Is<string>(y => y == filePath),
                It.IsAny<string>(),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .Returns(Task.CompletedTask);

            // Act
            var result = await sut.StartRecordingAsync(filePath, cancellationTokenSource.Token);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(filePath, result.FilePath);
            Assert.NotNull(result.StartedRecordingAt);
            Assert.Null(result.StoppedRecordingAt);
            Assert.Equal(TimeSpan.Zero, result.ElapsedRecordingTime);
            mockIoService.VerifyAll();
        }

        [Fact]
        public async Task GivenValidFilePath_WhenStartRecordingAsync_ThenCorrectJsonSavedToFile()
        {
            // Arrange
            var mockIoService = new Mock<IIoService>();
            var sut = new SideCarWriterService(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var filePath = "C:\\TestPath\\recording.sidecar";
            var capturedJson = string.Empty;

            mockIoService.Setup(x => x.WriteAllTextAsync(
                It.Is<string>(y => y == filePath),
                It.IsAny<string>(),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .Callback<string, string, CancellationToken>((path, json, ct) => capturedJson = json)
                .Returns(Task.CompletedTask);

            // Act
            var result = await sut.StartRecordingAsync(filePath, cancellationTokenSource.Token);

            // Assert
            Assert.Contains("StartedRecordingAt", capturedJson);
            Assert.Contains("\"StoppedRecordingAt\":null", capturedJson);
            Assert.DoesNotContain("FilePath", capturedJson); // FilePath should be JsonIgnore
            mockIoService.VerifyAll();
        }

        [Fact]
        public async Task GivenSideCarInfoWithStartTime_WhenStopRecordingAsync_ThenStopTimeSetAndSaved()
        {
            // Arrange
            var mockIoService = new Mock<IIoService>();
            var sut = new SideCarWriterService(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var filePath = "C:\\TestPath\\recording.sidecar";
            var startTime = DateTime.Now.AddMinutes(-5);
            var sideCarInfo = new SideCarInfo(filePath)
            {
                StartedRecordingAt = startTime
            };

            mockIoService.Setup(x => x.WriteAllTextAsync(
                It.Is<string>(y => y == filePath),
                It.IsAny<string>(),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .Returns(Task.CompletedTask);

            // Act
            await sut.StopRecordingAsync(sideCarInfo, cancellationTokenSource.Token);

            // Assert
            Assert.NotNull(sideCarInfo.StoppedRecordingAt);
            Assert.True(sideCarInfo.StoppedRecordingAt > startTime);
            Assert.True(sideCarInfo.ElapsedRecordingTime > TimeSpan.Zero);
            mockIoService.VerifyAll();
        }

        [Fact]
        public async Task GivenSideCarInfoWithStartTime_WhenStopRecordingAsync_ThenCorrectJsonSavedToFile()
        {
            // Arrange
            var mockIoService = new Mock<IIoService>();
            var sut = new SideCarWriterService(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var filePath = "C:\\TestPath\\recording.sidecar";
            var startTime = DateTime.Now.AddMinutes(-5);
            var sideCarInfo = new SideCarInfo(filePath)
            {
                StartedRecordingAt = startTime
            };
            var capturedJson = string.Empty;

            mockIoService.Setup(x => x.WriteAllTextAsync(
                It.Is<string>(y => y == filePath),
                It.IsAny<string>(),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .Callback<string, string, CancellationToken>((path, json, ct) => capturedJson = json)
                .Returns(Task.CompletedTask);

            // Act
            await sut.StopRecordingAsync(sideCarInfo, cancellationTokenSource.Token);

            // Assert
            Assert.Contains("StartedRecordingAt", capturedJson);
            Assert.Contains("StoppedRecordingAt", capturedJson);
            Assert.DoesNotContain("FilePath", capturedJson); // FilePath should be JsonIgnore
            mockIoService.VerifyAll();
        }

        [Fact]
        public async Task GivenSideCarInfoWithoutStartTime_WhenStopRecordingAsync_ThenElapsedTimeIsZero()
        {
            // Arrange
            var mockIoService = new Mock<IIoService>();
            var sut = new SideCarWriterService(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var filePath = "C:\\TestPath\\recording.sidecar";
            var sideCarInfo = new SideCarInfo(filePath)
            {
                StartedRecordingAt = null
            };

            mockIoService.Setup(x => x.WriteAllTextAsync(
                It.Is<string>(y => y == filePath),
                It.IsAny<string>(),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .Returns(Task.CompletedTask);

            // Act
            await sut.StopRecordingAsync(sideCarInfo, cancellationTokenSource.Token);

            // Assert
            Assert.NotNull(sideCarInfo.StoppedRecordingAt);
            Assert.Equal(TimeSpan.Zero, sideCarInfo.ElapsedRecordingTime);
            mockIoService.VerifyAll();
        }
    }
}