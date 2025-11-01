namespace PiSnoreMonitor.Core.Services
{
    public interface IWeatherService
    {
        public Task<WeatherData?> FetchAsync(CancellationToken cancellationToken);
    }
}
