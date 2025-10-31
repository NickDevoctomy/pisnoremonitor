using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PiSnoreMonitor.Controls
{
    internal class DayNightCycleView : Control
    {
        private const double HoursInDay = 24d;
        private const double MinutesInHour = 60d;
        private const double FullCircleDegrees = 360.0;
        private const double HalfCircleDegrees = 180.0;
        private const double DegToRad = Math.PI / 180.0;

        public static readonly StyledProperty<TimeSpan> SunriseTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(TimeSpan), new TimeSpan(6, 51, 0));
        public static readonly StyledProperty<TimeSpan> SunsetTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(TimeSpan), new TimeSpan(16, 34, 0));
        public static readonly StyledProperty<TimeSpan> CurrentTimeProperty = AvaloniaProperty.Register<DayNightCycleView, TimeSpan>(nameof(TimeSpan), new TimeSpan(18, 35, 0));

        public TimeSpan SunriseTime { get => GetValue(SunriseTimeProperty); set => SetValue(SunriseTimeProperty, value); }
        public TimeSpan SunsetTime { get => GetValue(SunsetTimeProperty); set => SetValue(SunsetTimeProperty, value); }
        public TimeSpan CurrentTime { get => GetValue(CurrentTimeProperty); set => SetValue(CurrentTimeProperty, value); }

        public override void Render(DrawingContext ctx)
        {
            var rect = new Rect(Bounds.Size);
            double shortSide = Math.Min(rect.Width, rect.Height);
            var box = new Rect(
                (rect.Width - shortSide) / 2.0,
                (rect.Height - shortSide) / 2.0,
                shortSide,
                shortSide
            );

            double radius = shortSide / 2.0;
            var center = box.Center;

            const double totalMinutes = HoursInDay * MinutesInHour;
            double degPerMinute = FullCircleDegrees / totalMinutes;

            double nowDeg = degPerMinute * CurrentTime.TotalMinutes;
            double sunriseDeg = degPerMinute * SunriseTime.TotalMinutes;
            double sunsetDeg = degPerMinute * SunsetTime.TotalMinutes;

            double dayStartRel = Normalize360(sunriseDeg - nowDeg);
            double dayEndRel = Normalize360(sunsetDeg - nowDeg);

            double nightStartRel = dayEndRel;
            double nightEndRel = dayStartRel + FullCircleDegrees;

            FillSectorTopClockwise(
                ctx,
                dayStartRel,
                dayEndRel,
                radius,
                center,
                new Pen(Brushes.LightBlue, 2),
                new SolidColorBrush(Colors.LightBlue));

            FillSectorTopClockwise(
                ctx,
                nightStartRel,
                nightEndRel,
                radius,
                center,
                new Pen(Brushes.DarkBlue, 2),
                new SolidColorBrush(Colors.DarkBlue));
        }

        private static void FillSectorTopClockwise(
            DrawingContext ctx,
            double startAngle, double endAngle,
            double radius, Point center,
            Pen outlinePen, Brush fillBrush)
        {
            double sweep = Normalize360(endAngle - startAngle);
            bool isLargeArc = sweep > HalfCircleDegrees;

            Point start = PtTop(startAngle, center, radius);
            Point end = PtTop(endAngle, center, radius);

            var geom = new StreamGeometry();
            using (var g = geom.Open())
            {
                g.BeginFigure(center, isFilled: true);
                g.LineTo(start);
                g.ArcTo(
                    end,
                    new Size(radius, radius),
                    rotationAngle: 0,
                    isLargeArc,
                    sweepDirection: SweepDirection.Clockwise,
                    true
                );
                g.LineTo(center);
                g.EndFigure(true);
            }

            ctx.DrawGeometry(fillBrush, outlinePen, geom);
        }

        private static Point PtTop(double deg, Point center, double radius)
        {
            double rad = deg * DegToRad;
            return new Point(
                center.X + radius * Math.Sin(rad),
                center.Y - radius * Math.Cos(rad)
            );
        }

        private static double Normalize360(double a)
        {
            a %= FullCircleDegrees;
            if (a < 0) a += FullCircleDegrees;
            return a;
        }
    }
}
