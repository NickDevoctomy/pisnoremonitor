using Moq;
using PiSnoreMonitor.Core.Configuration;
using System.Reflection;

namespace PiSnoreMonitor.Core.UnitTests.Configuration
{
    public class AppSettingsLoaderTests
    {
        public AppSettingsLoaderTests()
        {
            // Reset the static _appSettings field before each test
            var field = typeof(AppSettingsLoader<AppSettings>).GetField("_appSettings", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, null);
        }
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

        [Fact]
        public async Task GivenValidSettingsExist_WhenLoadAsync_ThenSettingsLoadedFromFile()
        {
            // Arrange
            var mockIoService = new Mock<Core.Services.IIoService>();
            var sut = new AppSettingsLoader<AppSettings>(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var expectedSettings = new AppSettings
            {
                RecordingSampleRate = 44100,
                EnableStereoRecording = true,
                SelectedAudioInputDeviceName = "Test Device",
                EnableHpfEffect = true,
                HpfEffectCutoffFrequency = 80.0f,
                EnableGainEffect = true,
                GainEffectGain = 3.0f
            };
            var jsonContent = """
                {
                  "RecordingSampleRate": 44100,
                  "EnableStereoRecording": true,
                  "SelectedAudioInputDeviceName": "Test Device",
                  "EnableHpfEffect": true,
                  "HpfEffectCutoffFrequency": 80.0,
                  "EnableGainEffect": true,
                  "GainEffectGain": 3.0
                }
                """;

            mockIoService.Setup(x => x.GetSpecialPath(
                Enums.SpecialPaths.AppUserStorage))
                .Returns("C:\\TestPath");

            mockIoService.Setup(x => x.CombinePaths(
                It.Is<string[]>(y => y[0] == "C:\\TestPath" && y[1] == "config.json")))
                .Returns("C:\\TestPath\\config.json");

            mockIoService.Setup(x => x.Exists(
                It.Is<string>(y => y == "C:\\TestPath\\config.json")))
                .Returns(true);

            mockIoService.Setup(x => x.ReadAllTextAsync(
                It.Is<string>(y => y == "C:\\TestPath\\config.json"),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .ReturnsAsync(jsonContent);

            // Act
            var result = await sut.LoadAsync(cancellationTokenSource.Token);

            // Assert
            Assert.Equal(expectedSettings, result);
            mockIoService.VerifyAll();
        }

        [Fact]
        public async Task GivenInvalidJsonInFile_WhenLoadAsync_ThenDefaultSettingsReturned()
        {
            // Arrange
            var mockIoService = new Mock<Core.Services.IIoService>();
            var sut = new AppSettingsLoader<AppSettings>(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var invalidJsonContent = "{ invalid json content }";

            mockIoService.Setup(x => x.GetSpecialPath(
                Enums.SpecialPaths.AppUserStorage))
                .Returns("C:\\TestPath");

            mockIoService.Setup(x => x.CombinePaths(
                It.Is<string[]>(y => y[0] == "C:\\TestPath" && y[1] == "config.json")))
                .Returns("C:\\TestPath\\config.json");

            mockIoService.Setup(x => x.Exists(
                It.Is<string>(y => y == "C:\\TestPath\\config.json")))
                .Returns(true);

            mockIoService.Setup(x => x.ReadAllTextAsync(
                It.Is<string>(y => y == "C:\\TestPath\\config.json"),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .ReturnsAsync(invalidJsonContent);

            // Act
            var result = await sut.LoadAsync(cancellationTokenSource.Token);

            // Assert
            Assert.Equal(new AppSettings(), result);
            mockIoService.VerifyAll();
        }

        [Fact]
        public async Task GivenSettingsAlreadyLoaded_WhenLoadAsyncCalledAgain_ThenCachedSettingsReturned()
        {
            // Arrange
            var mockIoService = new Mock<Core.Services.IIoService>();
            var sut = new AppSettingsLoader<AppSettings>(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var jsonContent = """
                {
                  "RecordingSampleRate": 44100,
                  "EnableStereoRecording": true
                }
                """;

            mockIoService.Setup(x => x.GetSpecialPath(
                Enums.SpecialPaths.AppUserStorage))
                .Returns("C:\\TestPath");

            mockIoService.Setup(x => x.CombinePaths(
                It.Is<string[]>(y => y[0] == "C:\\TestPath" && y[1] == "config.json")))
                .Returns("C:\\TestPath\\config.json");

            mockIoService.Setup(x => x.Exists(
                It.Is<string>(y => y == "C:\\TestPath\\config.json")))
                .Returns(true);

            mockIoService.Setup(x => x.ReadAllTextAsync(
                It.Is<string>(y => y == "C:\\TestPath\\config.json"),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .ReturnsAsync(jsonContent);

            // Act
            var result1 = await sut.LoadAsync(cancellationTokenSource.Token);
            var result2 = await sut.LoadAsync(cancellationTokenSource.Token);

            // Assert
            Assert.Same(result1, result2);
            mockIoService.Verify(x => x.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            mockIoService.VerifyAll();
        }

        [Fact]
        public async Task GivenSettingsLoaded_WhenSaveAsync_ThenSettingsSavedToFile()
        {
            // Arrange
            var mockIoService = new Mock<Core.Services.IIoService>();
            var sut = new AppSettingsLoader<AppSettings>(mockIoService.Object);
            var cancellationTokenSource = new CancellationTokenSource();
            var expectedJson = """
                {
                  "RecordingSampleRate": 48000,
                  "EnableStereoRecording": false,
                  "SelectedAudioInputDeviceName": "",
                  "EnableHpfEffect": false,
                  "HpfEffectCutoffFrequency": 60,
                  "EnableGainEffect": false,
                  "GainEffectGain": 2
                }
                """;

            mockIoService.Setup(x => x.GetSpecialPath(
                Enums.SpecialPaths.AppUserStorage))
                .Returns("C:\\TestPath");

            mockIoService.Setup(x => x.CombinePaths(
                It.Is<string[]>(y => y[0] == "C:\\TestPath" && y[1] == "config.json")))
                .Returns("C:\\TestPath\\config.json");

            mockIoService.Setup(x => x.Exists(
                It.Is<string>(y => y == "C:\\TestPath\\config.json")))
                .Returns(false);

            mockIoService.Setup(x => x.CreateDirectory(
                It.Is<string>(y => y == "C:\\TestPath")));

            mockIoService.Setup(x => x.WriteAllTextAsync(
                It.Is<string>(y => y == "C:\\TestPath\\config.json"),
                It.Is<string>(y => y == expectedJson),
                It.Is<CancellationToken>(y => y == cancellationTokenSource.Token)))
                .Returns(Task.CompletedTask);

            // Act
            await sut.LoadAsync(cancellationTokenSource.Token); // Load default settings first
            await sut.SaveAsync(cancellationTokenSource.Token);

            // Assert
            mockIoService.VerifyAll();
        }
    }
}
