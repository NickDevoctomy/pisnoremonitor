using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PiSnoreMonitor.Controls;

internal class CloudData
{
    public int SpriteIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
}

internal class DayNightCycleView : Control
{
    private const double SunRadius = 12.0;
    private const double SunMarginFromTop = 8.0;
    private const double MoonRadius = 10.0;
    private const double MoonMarginFromTop = 8.0;
    private const double CloudSpeed = 2.0; // pixels per frame

    public static readonly StyledProperty<TimeSpan> SunriseTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(SunriseTime), new TimeSpan(6, 51, 0));
    public static readonly StyledProperty<TimeSpan> SunsetTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(SunsetTime), new TimeSpan(16, 34, 0));
    public static readonly StyledProperty<TimeSpan> CurrentTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(CurrentTime), new TimeSpan(9, 53, 0));
    public static readonly StyledProperty<double> HorizonHeightProperty = AvaloniaProperty.Register<DayNightCycleView, double>(nameof(HorizonHeight), 0.2);
    public static readonly StyledProperty<int> TargetFpsProperty = AvaloniaProperty.Register<DayNightCycleView, int>(nameof(TargetFps), 30);

    // Sky color properties
    public static readonly StyledProperty<Color> NightSkyColorProperty = AvaloniaProperty.Register<DayNightCycleView, Color>(nameof(NightSkyColor), Colors.Black);
    public static readonly StyledProperty<Color> SunriseSunsetColorProperty = AvaloniaProperty.Register<DayNightCycleView, Color>(nameof(SunriseSunsetColor), Colors.Orange);
    public static readonly StyledProperty<Color> DaySkyColorProperty = AvaloniaProperty.Register<DayNightCycleView, Color>(nameof(DaySkyColor), Colors.SkyBlue);

    // Stars property
    public static readonly StyledProperty<int> NumberOfStarsProperty = AvaloniaProperty.Register<DayNightCycleView, int>(nameof(NumberOfStars), 50);

    private readonly SolidColorBrush _landBrush = new(Colors.SaddleBrown);
    private readonly SolidColorBrush _sunBrush = new(Colors.Yellow);
    private readonly SolidColorBrush _moonBrush = new(Colors.White);

    private List<Point> _starPositions = new();
    private System.Random _random = new(Environment.TickCount);
    private TimeSpan _last;
    private double _accumMs;
    private static List<Bitmap> _cloudSprites = new();
    private List<CloudData> _clouds = new();

    public TimeSpan SunriseTime { get => GetValue(SunriseTimeProperty); set => SetValue(SunriseTimeProperty, value); }

    public TimeSpan SunsetTime { get => GetValue(SunsetTimeProperty); set => SetValue(SunsetTimeProperty, value); }

    public TimeSpan CurrentTime { get => GetValue(CurrentTimeProperty); set => SetValue(CurrentTimeProperty, value); }

    public double HorizonHeight { get => GetValue(HorizonHeightProperty); set => SetValue(HorizonHeightProperty, value); }

    public int TargetFps { get => GetValue(TargetFpsProperty); set => SetValue(TargetFpsProperty, value); }

    public Color NightSkyColor { get => GetValue(NightSkyColorProperty); set => SetValue(NightSkyColorProperty, value); }

    public Color SunriseSunsetColor { get => GetValue(SunriseSunsetColorProperty); set => SetValue(SunriseSunsetColorProperty, value); }

    public Color DaySkyColor { get => GetValue(DaySkyColorProperty); set => SetValue(DaySkyColorProperty, value); }

    public int NumberOfStars { get => GetValue(NumberOfStarsProperty); set => SetValue(NumberOfStarsProperty, value); }

    static DayNightCycleView()
    {
        //AffectsRender<DayNightCycleView>(SunriseTimeProperty, SunsetTimeProperty, CurrentTimeProperty, HorizonHeightProperty,
        //    NightSkyColorProperty, SunriseSunsetColorProperty, DaySkyColorProperty, NumberOfStarsProperty);

        CacheCloudSprites();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(Tick);
    }

    public override void Render(DrawingContext ctx)
    {
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        // Draw sky (background) with dynamic color
        var skyColor = CalculateSkyColor();
        var skyBrush = new SolidColorBrush(skyColor);
        ctx.FillRectangle(skyBrush, bounds);

        // Initialize stars if needed
        InitializeStarsIfNeeded(bounds);

        // Initialize clouds if needed
        if (_clouds.Count == 0)
        {
            RandomlyPositionClouds(bounds);
        }

        // Draw stars with appropriate alpha based on time of day
        DrawStars(ctx, bounds);

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

        // Draw clouds
        DrawClouds(ctx, bounds);

        // Calculate horizon position
        var horizonY = bounds.Height * (1.0 - HorizonHeight);

        // Draw land/horizon last so it covers the sun/moon when below horizon
        var landRect = new Rect(0, horizonY, bounds.Width, bounds.Height - horizonY);
        ctx.FillRectangle(_landBrush, landRect);
    }

    private void Tick(TimeSpan now)
    {
        var bounds = new Rect(Bounds.Size);

        if (_last == default)
        {
            _last = now;
        }

        var dt = (now - _last).TotalMilliseconds;
        _last = now;

        _accumMs += dt;
        var frameMs = 1000.0 / Math.Max(1, TargetFps);

        if (_accumMs >= frameMs)
        {
            _accumMs %= frameMs;          // keep remainder to reduce jitter
            MoveClouds(bounds);           // Update cloud positions
            InvalidateVisual();           // queues for the next render pass
        }

        TopLevel.GetTopLevel(this)?.RequestAnimationFrame(Tick);
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

    private Color CalculateSkyColor()
    {
        // Convert times to minutes since midnight for easier calculation
        var currentMinutes = CurrentTime.Hours * 60 + CurrentTime.Minutes;
        var sunriseMinutes = SunriseTime.Hours * 60 + SunriseTime.Minutes;
        var sunsetMinutes = SunsetTime.Hours * 60 + SunsetTime.Minutes;

        // Define transition durations (in minutes)
        var transitionDuration = 60; // 1 hour transition period
        var sunriseTransitionStart = sunriseMinutes - transitionDuration / 2;
        var sunriseTransitionEnd = sunriseMinutes + transitionDuration / 2;
        var sunsetTransitionStart = sunsetMinutes - transitionDuration / 2;
        var sunsetTransitionEnd = sunsetMinutes + transitionDuration / 2;

        if (currentMinutes >= sunriseTransitionEnd && currentMinutes <= sunsetTransitionStart)
        {
            // Full day time - use day sky color
            return DaySkyColor;
        }
        else if (currentMinutes >= sunriseTransitionStart && currentMinutes <= sunriseTransitionEnd)
        {
            // Sunrise transition: night -> sunrise/sunset -> day
            var progress = (double)(currentMinutes - sunriseTransitionStart) / transitionDuration;
            if (progress <= 0.5)
            {
                // First half: night -> sunrise/sunset color
                var blendFactor = progress * 2; // 0 to 1
                return BlendColors(NightSkyColor, SunriseSunsetColor, blendFactor);
            }
            else
            {
                // Second half: sunrise/sunset -> day color
                var blendFactor = (progress - 0.5) * 2; // 0 to 1
                return BlendColors(SunriseSunsetColor, DaySkyColor, blendFactor);
            }
        }
        else if (currentMinutes >= sunsetTransitionStart && currentMinutes <= sunsetTransitionEnd)
        {
            // Sunset transition: day -> sunrise/sunset -> night
            var progress = (double)(currentMinutes - sunsetTransitionStart) / transitionDuration;
            if (progress <= 0.5)
            {
                // First half: day -> sunrise/sunset color
                var blendFactor = progress * 2; // 0 to 1
                return BlendColors(DaySkyColor, SunriseSunsetColor, blendFactor);
            }
            else
            {
                // Second half: sunrise/sunset -> night color
                var blendFactor = (progress - 0.5) * 2; // 0 to 1
                return BlendColors(SunriseSunsetColor, NightSkyColor, blendFactor);
            }
        }
        else
        {
            // Night time - use night sky color
            return NightSkyColor;
        }
    }

    private Color BlendColors(Color color1, Color color2, double factor)
    {
        // Clamp factor between 0 and 1
        factor = Math.Max(0, Math.Min(1, factor));

        var r = (byte)(color1.R + (color2.R - color1.R) * factor);
        var g = (byte)(color1.G + (color2.G - color1.G) * factor);
        var b = (byte)(color1.B + (color2.B - color1.B) * factor);
        var a = (byte)(color1.A + (color2.A - color1.A) * factor);

        return Color.FromArgb(a, r, g, b);
    }

    private void InitializeStarsIfNeeded(Rect bounds)
    {
        // Only initialize if we don't have the right number of stars or bounds changed significantly
        if (_starPositions.Count != NumberOfStars || bounds.Width <= 0 || bounds.Height <= 0)
        {
            _starPositions.Clear();

            // Generate random star positions in the sky area
            for (int i = 0; i < NumberOfStars; i++)
            {
                var x = _random.NextDouble();
                var y = _random.NextDouble(); // Only in sky area, not below horizon
                _starPositions.Add(new Point(x, y));
            }
        }
    }

    private void DrawStars(DrawingContext ctx, Rect bounds)
    {
        if (_starPositions.Count == 0)
            return;

        // Calculate star alpha based on time of day (visible at night, invisible during day)
        var starAlpha = CalculateStarAlpha();

        if (starAlpha <= 0)
            return; // Don't draw stars if they're completely invisible

        // Create star brush with calculated alpha
        var starColor = Color.FromArgb((byte)(255 * starAlpha), Colors.White.R, Colors.White.G, Colors.White.B);
        var starBrush = new SolidColorBrush(starColor);

        var horizonY = bounds.Height * (1.0 - HorizonHeight);

        // Draw each star as a small rectangle (pixel)
        foreach (var starPos in _starPositions)
        {
            var starRect = new Rect(bounds.Width * starPos.X, horizonY * starPos.Y, 1, 1);
            ctx.FillRectangle(starBrush, starRect);
        }
    }

    private double CalculateStarAlpha()
    {
        // Convert times to minutes since midnight for easier calculation
        var currentMinutes = CurrentTime.Hours * 60 + CurrentTime.Minutes;
        var sunriseMinutes = SunriseTime.Hours * 60 + SunriseTime.Minutes;
        var sunsetMinutes = SunsetTime.Hours * 60 + SunsetTime.Minutes;

        // Define transition durations (in minutes) - same as sky color transitions
        var transitionDuration = 60; // 1 hour transition period
        var sunriseTransitionStart = sunriseMinutes - transitionDuration / 2;
        var sunriseTransitionEnd = sunriseMinutes + transitionDuration / 2;
        var sunsetTransitionStart = sunsetMinutes - transitionDuration / 2;
        var sunsetTransitionEnd = sunsetMinutes + transitionDuration / 2;

        if (currentMinutes >= sunriseTransitionEnd && currentMinutes <= sunsetTransitionStart)
        {
            // Full day time - stars invisible
            return 0.0;
        }
        else if (currentMinutes >= sunriseTransitionStart && currentMinutes <= sunriseTransitionEnd)
        {
            // Sunrise transition: stars fade out
            var progress = (double)(currentMinutes - sunriseTransitionStart) / transitionDuration;
            return 1.0 - progress; // Fade from 1 to 0
        }
        else if (currentMinutes >= sunsetTransitionStart && currentMinutes <= sunsetTransitionEnd)
        {
            // Sunset transition: stars fade in
            var progress = (double)(currentMinutes - sunsetTransitionStart) / transitionDuration;
            return progress; // Fade from 0 to 1
        }
        else
        {
            // Night time - stars fully visible
            return 1.0;
        }
    }

    private double CalculateCloudBrightness()
    {
        // Convert times to minutes since midnight for easier calculation
        var currentMinutes = CurrentTime.Hours * 60 + CurrentTime.Minutes;
        var sunriseMinutes = SunriseTime.Hours * 60 + SunriseTime.Minutes;
        var sunsetMinutes = SunsetTime.Hours * 60 + SunsetTime.Minutes;

        // Define transition durations (in minutes) - same as sky color transitions
        var transitionDuration = 60; // 1 hour transition period
        var sunriseTransitionStart = sunriseMinutes - transitionDuration / 2;
        var sunriseTransitionEnd = sunriseMinutes + transitionDuration / 2;
        var sunsetTransitionStart = sunsetMinutes - transitionDuration / 2;
        var sunsetTransitionEnd = sunsetMinutes + transitionDuration / 2;

        if (currentMinutes >= sunriseTransitionEnd && currentMinutes <= sunsetTransitionStart)
        {
            // Full day time - clouds at full brightness
            return 1.0;
        }
        else if (currentMinutes >= sunriseTransitionStart && currentMinutes <= sunriseTransitionEnd)
        {
            // Sunrise transition: clouds brighten from dark to full brightness
            var progress = (double)(currentMinutes - sunriseTransitionStart) / transitionDuration;
            return 0.1 + (0.9 * progress); // Brighten from 0.1 (very dark) to 1.0 (full brightness)
        }
        else if (currentMinutes >= sunsetTransitionStart && currentMinutes <= sunsetTransitionEnd)
        {
            // Sunset transition: clouds darken from full brightness to dark
            var progress = (double)(currentMinutes - sunsetTransitionStart) / transitionDuration;
            return 1.0 - (0.9 * progress); // Darken from 1.0 (full brightness) to 0.1 (very dark)
        }
        else
        {
            // Night time - clouds very dark (but not completely black so they're still visible)
            return 0.1;
        }
    }

    private void RandomlyPositionClouds(Rect bounds)
    {
        if (_cloudSprites.Count == 0)
            return;

        _clouds.Clear();

        var horizonY = bounds.Height * (1.0 - HorizonHeight);

        for (int i = 0; i < 20; i++)
        {
            var spriteIndex = _random.Next(_cloudSprites.Count);
            var sprite = _cloudSprites[spriteIndex];
            var cloud = new CloudData
            {
                SpriteIndex = spriteIndex, // Random cloud sprite
                X = _random.NextDouble() * (Bounds.Width * 2), // Random X position across screen
                Y = _random.NextDouble() * horizonY - sprite.PixelSize.Height // Random Y in upper sky area only
            };
            _clouds.Add(cloud);
        }
    }

    private void MoveClouds(Rect bounds)
    {
        for (int i = 0; i < _clouds.Count; i++)
        {
            var cloud = _clouds[i];
            var sprite = _cloudSprites[cloud.SpriteIndex];

            // Move cloud left by CloudSpeed pixels
            cloud.X -= CloudSpeed;

            // Check if cloud is completely out of view on the left
            if (cloud.X < -_cloudSprites[cloud.SpriteIndex].PixelSize.Width)
            {
                var horizonY = bounds.Height * (1.0 - HorizonHeight);
                cloud.X = Bounds.Width + (_random.NextDouble() * Bounds.Width);
                cloud.Y = _random.NextDouble() * horizonY - sprite.PixelSize.Height; // Random Y in upper sky area only
            }
        }
    }

    private void DrawClouds(DrawingContext ctx, Rect bounds)
    {
        if (_cloudSprites.Count == 0 || _clouds.Count == 0)
            return;

        // Calculate cloud brightness based on time of day
        var cloudBrightness = CalculateCloudBrightness();

        // Draw each cloud with appropriate brightness using opacity
        using (ctx.PushOpacity(cloudBrightness))
        {
            foreach (var cloud in _clouds)
            {
                var sprite = _cloudSprites[cloud.SpriteIndex];
                var rect = new Rect(cloud.X, cloud.Y, sprite.PixelSize.Width, sprite.PixelSize.Height);
                ctx.DrawImage(sprite, rect);
            }
        }
    }

    private static void CacheCloudSprites()
    {
        var cloudSpriteFiles = Directory.GetFiles("images/sprites/clouds", "*.png");
        foreach (var cloudSpriteFile in cloudSpriteFiles)
        {
            using var stream = File.OpenRead(cloudSpriteFile);
            var bitmap = new Bitmap(stream);
            _cloudSprites.Add(bitmap);
        }
    }
}