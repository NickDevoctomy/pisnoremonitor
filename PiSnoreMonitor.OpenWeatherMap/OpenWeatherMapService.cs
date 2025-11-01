using Microsoft.Extensions.Logging;
using OpenWeatherMap.Standard;
using OpenWeatherMap.Standard.Enums;
using PiSnoreMonitor.Core.Services;

namespace PiSnoreMonitor.OpenWeatherMap
{
    public class OpenWeatherMapService(ILogger<OpenWeatherMapService> logger) : IWeatherService
    {
        public async Task<WeatherData?> FetchAsync(CancellationToken cancellationToken)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENWEATHERMAP_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("Api key for OpenWeatherMap has not been configured. Check OPENWEATHERMAP_API_KEY env var.");
                return null;
            }

            try
            {
                var client = new Current(
                    apiKey,
                    WeatherUnits.Metric);
                var result = await client.GetForecastDataByCityNameAsync("London");
                var data = new WeatherData
                {
                    FetchedAt = DateTime.UtcNow,
                    Source = "OpenWeatherMap",
                    Sunrise = result.WeatherData[0].DayInfo.Sunrise,
                    Sunset = result.WeatherData[0].DayInfo.Sunset,
                    CloudinessPercent = result.WeatherData[0].Clouds.All,
                    TemperatureCelsius = result.WeatherData[0].WeatherDayInfo.Temperature,
                    RainMillimetersPerHour = result.WeatherData[0].Rain.LastHour,
                    SnowMillimetersPerHour = result.WeatherData[0].Snow.LastHour,
                    WindSpeedMetersPerSecond = result.WeatherData[0].Wind.Speed
                };
                return data;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch weather data from OpenWeatherMap.");
                return null;
            }
        }
    }
}
