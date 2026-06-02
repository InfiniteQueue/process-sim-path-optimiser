using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using PathOptimiser.OptimisationSystem;

namespace PathOptimiser.DisplayGrid.DebugEnvelopeDisplay
{
    public partial class CollisionTimelineControl : UserControl
    {
        private EnvelopeCollection _collection;

        public CollisionTimelineControl()
        {
            InitializeComponent();
            SizeChanged += (_, __) => Render();
        }


        public void Update(EnvelopeCollection collection)
        {
            _collection = collection;
            Render();
        }


        private void Render()
        {
            timelineCanvas.Children.Clear();
            if (_collection == null) return;

            var frames = _collection.Envelopes
                .SelectMany(e => e.FrameDataList)
                .OrderBy(f => f.Time)
                .ToList();
            if (frames.Count == 0) return;

            double minTime = frames.Min(f => f.Time);
            double maxTime = frames.Max(f => f.Time + f.Duration);
            double timeSpan = maxTime - minTime;

            var maxFramePixelWidth = 50;
            timelineCanvas.Width = double.NaN;
            timelineCanvas.MaxWidth = Math.Min(this.ActualWidth, (timeSpan/(frames.FirstOrDefault()?.Duration ?? 1.0)) * maxFramePixelWidth);
            timelineCanvas.UpdateLayout();
            double canvasWidth = timelineCanvas.ActualWidth;
            double canvasHeight = timelineCanvas.ActualHeight;
            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double maxPermissable = frames.Max(f => f.MinIgnoredClearance);
            if (maxPermissable <= 0) return;

            //var barBrush = new SolidColorBrush(Color.FromArgb(153, 70, 130, 180)); // SteelBlue @ 0.6

            foreach (var frame in frames)
            {
                double x, w;
                if (timeSpan > 0)
                {
                    x = (frame.Time - minTime) / timeSpan * canvasWidth;
                    w = Math.Max(1.0, frame.Duration / timeSpan * canvasWidth);
                }
                else
                {
                    x = 0;
                    w = canvasWidth;
                }

                double heightFactor = Math.Max(0, 0.1 + 0.90 * (maxPermissable - frame.Clearance) / maxPermissable);
                double h = heightFactor * canvasHeight;

                var barBrush = new SolidColorBrush(frame.CollisionState == CollisionEnvelope.FrameData.State.Intersects ? Colors.Red : Colors.Orange);

                var rect = new Rectangle
                {
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.25,
                    Width = w,
                    Height = h,
                    Fill = barBrush,
                };

                var overlay = new Rectangle
                {
                    Fill = Brushes.Transparent,
                    Width = w,
                    Height = canvasHeight,
                    ToolTip = new TextBlock
                    {
                        Text =
                    $"Time: {frame.Time}\n" +
                    $"Closest Via: {frame.ViaName}\n" +
                    $"Clearance: {frame.Clearance:0.##}mm\n" +
                    $"Speed: {frame.Speed:0}mm/s"
                    }
                }; 

                ToolTipService.SetInitialShowDelay(overlay, 0);

                Canvas.SetLeft(rect, x);
                Canvas.SetLeft(overlay, x);
                Canvas.SetTop(rect, canvasHeight - h);
                Canvas.SetTop(overlay, 0);
                timelineCanvas.Children.Add(rect);
                timelineCanvas.Children.Add(overlay);
            }
        }
    }
}
