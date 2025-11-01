using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PiSnoreMonitor.Controls
{
    internal class DayNightCycleView : Control
    {
        private const double SunRadius = 12.0;
        private const double SunMarginFromTop = 8.0;
        private const double MoonRadius = 10.0;
        private const double MoonMarginFromTop = 8.0;

        public static readonly StyledProperty<TimeSpan> SunriseTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(SunriseTime), new TimeSpan(6, 51, 0));
        public static readonly StyledProperty<TimeSpan> SunsetTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(SunsetTime), new TimeSpan(16, 34, 0));
        public static readonly StyledProperty<TimeSpan> CurrentTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(CurrentTime), new TimeSpan(9, 32, 0));
        public static readonly StyledProperty<double> HorizonHeightProperty = AvaloniaProperty.Register<DayNightCycleView, double>(nameof(HorizonHeight), 0.5);

        private readonly SolidColorBrush _skyBrush = new(Colors.SkyBlue);
        private readonly SolidColorBrush _landBrush = new(Colors.SaddleBrown);
        private readonly SolidColorBrush _sunBrush = new(Colors.Yellow);
        private readonly SolidColorBrush _moonBrush = new(Colors.White);

        public TimeSpan SunriseTime { get => GetValue(SunriseTimeProperty); set => SetValue(SunriseTimeProperty, value); }
        public TimeSpan SunsetTime { get => GetValue(SunsetTimeProperty); set => SetValue(SunsetTimeProperty, value); }
        public TimeSpan CurrentTime { get => GetValue(CurrentTimeProperty); set => SetValue(CurrentTimeProperty, value); }
        public double HorizonHeight { get => GetValue(HorizonHeightProperty); set => SetValue(HorizonHeightProperty, value); }

        static DayNightCycleView()
        {
            AffectsRender<DayNightCycleView>(SunriseTimeProperty, SunsetTimeProperty, CurrentTimeProperty, HorizonHeightProperty);
        }

        public override void Render(DrawingContext ctx)
        {
            var bounds = new Rect(Bounds.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            // Draw sky (background)
            ctx.FillRectangle(_skyBrush, bounds);

            // Calculate and draw sun (always draw it, even if it will be covered by land)
            var sunPosition = CalculateSunPosition(bounds);
            if (sunPosition.HasValue)
            {
                var sunCenter = new Point(bounds.Width / 2.0, sunPosition.Value);
                ctx.DrawEllipse(_sunBrush, null, sunCenter, SunRadius, SunRadius);
            }

            // Calculate and draw moon (always draw it, even if it will be covered by land)
            var moonPosition = CalculateMoonPosition(bounds);
            if (moonPosition.HasValue)
            {
                var moonCenter = moonPosition.Value;
                ctx.DrawEllipse(_moonBrush, null, moonCenter, MoonRadius, MoonRadius);
            }

            // Calculate horizon position
            var horizonY = bounds.Height * (1.0 - HorizonHeight);

            // Draw land/horizon last so it covers the sun/moon when below horizon
            var landRect = new Rect(0, horizonY, bounds.Width, bounds.Height - horizonY);
            ctx.FillRectangle(_landBrush, landRect);
        }

        private double? CalculateSunPosition(Rect bounds)
        {
            // Convert times to minutes since midnight for easier calculation
            var currentMinutes = CurrentTime.Hours * 60 + CurrentTime.Minutes;
            var sunriseMinutes = SunriseTime.Hours * 60 + SunriseTime.Minutes;
            var sunsetMinutes = SunsetTime.Hours * 60 + SunsetTime.Minutes;

            // Calculate the progress through the entire day (24 hours)
            var dayLength = sunsetMinutes - sunriseMinutes;
            var midDay = sunriseMinutes + (dayLength / 2);

            double progress;
            if (currentMinutes >= sunriseMinutes && currentMinutes <= sunsetMinutes)
            {
                // Daytime: calculate normal progress (0 = sunrise, 1 = sunset)
                progress = (double)(currentMinutes - sunriseMinutes) / dayLength;
            }
            else
            {
                // Nighttime: sun is below horizon
                // Calculate how far through the night we are to position sun underground
                if (currentMinutes < sunriseMinutes)
                {
                    // Early morning before sunrise
                    var nightProgress = (double)(sunriseMinutes - currentMinutes) / (24 * 60 - dayLength);
                    progress = -nightProgress;
                }
                else
                {
                    // Evening after sunset
                    var nightProgress = (double)(currentMinutes - sunsetMinutes) / (24 * 60 - dayLength);
                    progress = 1 + nightProgress;
                }
            }

            // Calculate sun's height using a sine wave (peaks at midday)
            var angle = progress * Math.PI; // Creates the arc
            var heightFactor = Math.Sin(angle); // 0 at sunrise/sunset, 1 at midday, negative underground

            // Calculate the available height for sun movement
            var horizonY = bounds.Height * (1.0 - HorizonHeight);
            var maxSunHeight = SunMarginFromTop + SunRadius;
            var availableHeight = horizonY - maxSunHeight;

            // Position sun: at sunrise/sunset, center should be one radius below horizon (completely hidden)
            // At midday, center should be at the top margin
            var sunY = horizonY + SunRadius - (heightFactor * (availableHeight + SunRadius));

            return sunY;
        }

        private Point? CalculateMoonPosition(Rect bounds)
        {
            // Convert times to minutes since midnight for easier calculation
            var currentMinutes = CurrentTime.Hours * 60 + CurrentTime.Minutes;
            var sunriseMinutes = SunriseTime.Hours * 60 + SunriseTime.Minutes;
            var sunsetMinutes = SunsetTime.Hours * 60 + SunsetTime.Minutes;

            // Check if it's nighttime (moon is only visible during night)
            double nightProgress;

            if (currentMinutes > sunsetMinutes)
            {
                // Evening after sunset until midnight
                var nightLength = (24 * 60) - sunsetMinutes + sunriseMinutes; // Total night duration
                var currentNightMinutes = currentMinutes - sunsetMinutes;
                nightProgress = (double)currentNightMinutes / nightLength;
            }
            else if (currentMinutes < sunriseMinutes)
            {
                // Early morning before sunrise
                var nightLength = (24 * 60) - sunsetMinutes + sunriseMinutes; // Total night duration
                var currentNightMinutes = (24 * 60) - sunsetMinutes + currentMinutes; // Minutes since sunset
                nightProgress = (double)currentNightMinutes / nightLength;
            }
            else
            {
                // Daytime - moon is not visible
                return null;
            }

            // Calculate moon's position during night (left to right arc)
            var angle = nightProgress * Math.PI; // 0 to π radians
            var heightFactor = Math.Sin(angle); // 0 at start/end of night, 1 at midnight

            // Calculate horizontal position (left to right across the sky) with same margin as sun
            var horizontalMargin = SunMarginFromTop; // Use same margin value
            var moonX = horizontalMargin + MoonRadius + (nightProgress * (bounds.Width - 2 * (horizontalMargin + MoonRadius)));

            // Calculate vertical position (arc across the sky)
            var horizonY = bounds.Height * (1.0 - HorizonHeight);
            var maxMoonHeight = MoonMarginFromTop + MoonRadius;
            var availableHeight = horizonY - maxMoonHeight;

            // Position moon: at sunset/sunrise, center should be one radius below horizon (completely hidden)
            // At midnight, center should be at the top margin
            var moonY = horizonY + MoonRadius - (heightFactor * (availableHeight + MoonRadius));

            return new Point(moonX, moonY);
        }
    }
}
