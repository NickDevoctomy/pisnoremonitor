using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PiSnoreMonitor.Controls
{
    public sealed class LevelMeter : Control
    {
        public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Minimum), 0);
        public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Maximum), 100);
        public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Value), 0);
        public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<LevelMeter, IBrush?>(nameof(TrackBrush));
        public static readonly StyledProperty<double> LevelMarkerThicknessProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Minimum), 2);
        public static readonly StyledProperty<bool> ShowMaxProperty = AvaloniaProperty.Register<LevelMeter, bool>(nameof(ShowMax), true);

        public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
        public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        public bool ShowMax { get => GetValue(ShowMaxProperty); set => SetValue(ShowMaxProperty, value); }
        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
        public double LevelMarkerThickness { get => GetValue(LevelMarkerThicknessProperty); set => SetValue(LevelMarkerThicknessProperty, value); }

        private double _maximumReached = 0;

        static LevelMeter()
        {
            AffectsRender<LevelMeter>(MinimumProperty, MaximumProperty, ValueProperty, TrackBrushProperty);
        }

        public override void Render(DrawingContext ctx)
        {
            var rect = new Rect(Bounds.Size);

            if (TrackBrush is not null)
            {
                ctx.FillRectangle(TrackBrush, rect);
            }

            if (Value > _maximumReached)
            {
                _maximumReached = Value;
            }

            if (ShowMax)
            {
                DrawLine(ctx, rect, Brushes.Yellow, _maximumReached);
            }

            DrawLine(ctx, rect, Brushes.White, Value);
        }

        private void DrawLine(DrawingContext ctx, Rect rect, IImmutableSolidColorBrush brush, double value)
        {
            var min = Minimum;
            var max = Maximum > min ? Maximum : min + 1;
            var v = Math.Clamp(value, min, max);
            var frac = (v - min) / (max - min);
            var y = rect.Bottom - frac * rect.Height;
            var pen = new Pen(brush, LevelMarkerThickness);
            ctx.DrawLine(pen, new Point(rect.Left, y), new Point(rect.Right, y));
        }
    }
}
