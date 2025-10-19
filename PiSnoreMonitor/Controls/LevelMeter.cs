using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace PiSnoreMonitor.Controls
{
    public sealed class LevelMeter : Control
    {
        public static readonly StyledProperty<double> MinimumProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Minimum), 0);
        public static readonly StyledProperty<double> MaximumProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Maximum), 100);
        public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Value), 0);
        public static readonly StyledProperty<IBrush?> TrackBrushProperty = AvaloniaProperty.Register<LevelMeter, IBrush?>(nameof(TrackBrush));
        public static readonly StyledProperty<double> LevelMarkerThicknessProperty = AvaloniaProperty.Register<LevelMeter, double>(nameof(Minimum), 2);

        public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
        public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
        public double Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public IBrush? TrackBrush { get => GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
        public double LevelMarkerThickness { get => GetValue(LevelMarkerThicknessProperty); set => SetValue(LevelMarkerThicknessProperty, value); }

        static LevelMeter()
        {
            AffectsRender<LevelMeter>(MinimumProperty, MaximumProperty, ValueProperty, TrackBrushProperty);
        }

        public override void Render(DrawingContext ctx)
        {
            var rect = new Rect(Bounds.Size);
            if (TrackBrush is not null)
                ctx.FillRectangle(TrackBrush, rect);

            var min = Minimum;
            var max = Maximum > min ? Maximum : min + 1;
            var v = Math.Clamp(Value, min, max);
            var frac = (v - min) / (max - min);

            // y from bottom (0) to top (1)
            var y = rect.Bottom - frac * rect.Height;

            // 2 px white line (tweak thickness as needed)
            var pen = new Pen(Brushes.White, LevelMarkerThickness);
            ctx.DrawLine(pen, new Point(rect.Left, y), new Point(rect.Right, y));
        }
    }
}
