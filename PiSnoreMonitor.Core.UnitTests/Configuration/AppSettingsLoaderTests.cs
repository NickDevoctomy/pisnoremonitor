using Moq;
using PiSnoreMonitor.Core.Configuration;

namespace PiSnoreMonitor.Core.UnitTests.Configuration
{
    public class AppSettingsLoaderTests
    {
        [Fact]
        public async Task GivenNoSettingsExist_WhenLoadAsync_ThenDefaultSettingsReturned()
        {
            // Arrange
            var mockIoService = new Mock<Core.Services.IIoService>();
            var sut = new AppSettingsLoader<AppSettings>(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();

            mockIoService.Setup(x => x.GetSpecialPath(
                Enums.SpecialPaths.AppUserStorage))
                .Returns("C:\\NonExistentPath");

            mockIoService.Setup(x => x.CombinePaths(
                It.Is<string[]>(y => y[0] == "C:\\NonExistentPath" && y[1] == "config.json")))
                .Returns("C:\\NonExistentPath\\config.json");

            mockIoService.Setup(x => x.Exists(
                It.Is<string>(y => y == "C:\\NonExistentPath\\config.json")))
                .Returns(false);

            // Act
            var result = await sut.LoadAsync(cancellationTokenSource.Token);

            // Assert
            Assert.Equal(new AppSettings(), result);
            mockIoService.VerifyAll();
        }
    }
}
