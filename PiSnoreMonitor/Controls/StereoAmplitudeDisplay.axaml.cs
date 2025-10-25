using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PiSnoreMonitor.Controls
{
    public partial class StereoAmplitudeDisplay : UserControl
    {
        private readonly List<(float Left, float Right)> _samples = new();
        private Canvas _displayCanvas;
        private Line _centerLine;
        private readonly object _lock = new object();

        public StereoAmplitudeDisplay()
        {
            InitializeComponent();
            _displayCanvas = this.FindControl<Canvas>("DisplayCanvas")!;
            _centerLine = this.FindControl<Line>("CenterLine")!;
            
            // Subscribe to size changes to update the display
            SizeChanged += OnSizeChanged;
        }

        public static readonly StyledProperty<IBrush> LeftChannelBrushProperty =
            AvaloniaProperty.Register<StereoAmplitudeDisplay, IBrush>(nameof(LeftChannelBrush), Brushes.Blue);

        public IBrush LeftChannelBrush
        {
            get => GetValue(LeftChannelBrushProperty);
            set => SetValue(LeftChannelBrushProperty, value);
        }

        public static readonly StyledProperty<IBrush> RightChannelBrushProperty =
            AvaloniaProperty.Register<StereoAmplitudeDisplay, IBrush>(nameof(RightChannelBrush), Brushes.Green);

        public IBrush RightChannelBrush
        {
            get => GetValue(RightChannelBrushProperty);
            set => SetValue(RightChannelBrushProperty, value);
        }

        public static readonly StyledProperty<double> AmplitudeScaleProperty =
            AvaloniaProperty.Register<StereoAmplitudeDisplay, double>(nameof(AmplitudeScale), 100.0);

        public double AmplitudeScale
        {
            get => GetValue(AmplitudeScaleProperty);
            set => SetValue(AmplitudeScaleProperty, value);
        }

        public static readonly StyledProperty<int> MaxSamplesProperty =
            AvaloniaProperty.Register<StereoAmplitudeDisplay, int>(nameof(MaxSamples), 100, coerce: CoerceMaxSamples);

        public int MaxSamples
        {
            get => GetValue(MaxSamplesProperty);
            set => SetValue(MaxSamplesProperty, value);
        }

        private static int CoerceMaxSamples(AvaloniaObject instance, int value)
        {
            // Ensure MaxSamples is at least 10 and at most 1000
            return Math.Max(10, Math.Min(1000, value));
        }

        public void Clear()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                ClearInternal();
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(ClearInternal);
            }
        }

        public void PushSample(float left, float right)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                PushSampleInternal(left, right);
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() => PushSampleInternal(left, right));
            }
        }

        private void ClearInternal()
        {
            lock (_lock)
            {
                _samples.Clear();
                RedrawDisplay();
            }
        }

        private void PushSampleInternal(float left, float right)
        {
            lock (_lock)
            {
                // Add new sample
                _samples.Add((left, right));
                
                // Remove old samples if we exceed the maximum
                while (_samples.Count > MaxSamples)
                {
                    _samples.RemoveAt(0);
                }
                
                RedrawDisplay();
            }
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Update center line
            var centerY = e.NewSize.Height / 2;
            _centerLine.StartPoint = new Point(0, centerY);
            _centerLine.EndPoint = new Point(e.NewSize.Width, centerY);
        }

        private void RedrawDisplay()
        {
            if (_displayCanvas == null) return;

            // Clear existing amplitude bars (keep the center line)
            var toRemove = _displayCanvas.Children
                .Where(child => child != _centerLine)
                .ToList();
            
            foreach (var child in toRemove)
            {
                _displayCanvas.Children.Remove(child);
            }

            if (_samples.Count == 0) return;

            var width = _displayCanvas.Bounds.Width;
            var height = _displayCanvas.Bounds.Height;
            var centerY = height / 2;
            
            if (width <= 0 || height <= 0) return;

            // Calculate spacing using integer math to avoid sub-pixel issues
            const int gapWidth = 1;
            var totalGapWidth = (MaxSamples - 1) * gapWidth;
            var availableBarWidth = (int)width - totalGapWidth;
            var barWidth = Math.Max(1, availableBarWidth / MaxSamples);
            var barSpacing = barWidth + gapWidth;
            
            for (int i = 0; i < _samples.Count; i++)
            {
                var sample = _samples[i];
                // Position from right to left, with proper spacing using integer math
                var barIndex = _samples.Count - i - 1;
                var x = (int)width - (barIndex * barSpacing) - barWidth;
                
                // Clamp amplitudes to 0-1 range
                var leftAmp = Math.Max(0, Math.Min(1, sample.Left));
                var rightAmp = Math.Max(0, Math.Min(1, sample.Right));
                
                // Calculate bar heights (scale by AmplitudeScale and half the available height)
                var leftHeight = leftAmp * AmplitudeScale * (centerY * 0.9); // Leave some margin
                var rightHeight = rightAmp * AmplitudeScale * (centerY * 0.9);
                
                // Create left channel bar (above center line)
                if (leftHeight > 1)
                {
                    var leftBar = new Rectangle
                    {
                        Width = barWidth,
                        Height = leftHeight,
                        Fill = LeftChannelBrush
                    };
                    Canvas.SetLeft(leftBar, x);
                    Canvas.SetTop(leftBar, centerY - leftHeight);
                    _displayCanvas.Children.Add(leftBar);
                }
                
                // Create right channel bar (below center line)
                if (rightHeight > 1)
                {
                    var rightBar = new Rectangle
                    {
                        Width = barWidth,
                        Height = rightHeight,
                        Fill = RightChannelBrush
                    };
                    Canvas.SetLeft(rightBar, x);
                    Canvas.SetTop(rightBar, centerY);
                    _displayCanvas.Children.Add(rightBar);
                }
            }
        }
    }
}