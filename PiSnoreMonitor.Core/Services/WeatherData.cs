namespace PiSnoreMonitor.Core.Services
{
    public class WeatherData
    {
        public DateTime FetchedAt { get; set; }

        public string? Source { get; set; }

        public DateTime Sunrise { get; set; }

        public DateTime Sunset { get; set; }

        public int CloudinessPercent { get; set; }

        public float TemperatureCelsius { get; set; }

        public float WindSpeedMetersPerSecond { get; set; }

        public float RainMillimetersPerHour { get; set; }

        public float SnowMillimetersPerHour { get; set; }
    }
}
