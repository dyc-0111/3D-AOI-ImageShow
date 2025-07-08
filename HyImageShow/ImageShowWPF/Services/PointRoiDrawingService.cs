using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using HyImageShow.ImageShowWPF.Models;

namespace HyImageShow.ImageShowWPF.Services
{
    public class PointRoiDrawingService : BaseRoiDrawingService<PointItem>
    {
        private Canvas mainCanvas;
        private readonly Dictionary<string, (Color dot, Color glow, Color labelBg, Color labelFg)> styleDict;

        public PointRoiDrawingService()
        {
            styleDict = new Dictionary<string, (Color, Color, Color, Color)>
            {
                {"point", (Color.FromRgb(229, 57, 53), Color.FromRgb(183, 28, 28), Color.FromArgb(220, 255, 205, 210), Color.FromRgb(97, 0, 0))}, // 紅
            };
        }

        public override void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        public override void DrawSingleRoi(PointItem roi, Canvas canvas, bool showLabels = true)
        {
            RemovePointVisual(roi, canvas);
            // 畫一個有陰影的小圓點
            var ellipse = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(styleDict["point"].dot),
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["point"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                IsHitTestVisible = false,
                RenderTransform = roi.PointScale,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            Canvas.SetLeft(ellipse, roi.Position.X - 6);
            Canvas.SetTop(ellipse, roi.Position.Y - 6);
            canvas.Children.Add(ellipse);
            Canvas.SetZIndex(ellipse, roi.ZIndex);
            roi.EllipseElement = ellipse;

            // 標籤
            if (showLabels)
            {
                AddPointLabel(roi, canvas);
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<PointItem> rois, PointItem currentRoi = null)
        {
            if (canvas == null) return;
            foreach (var roi in rois)
            {
                RemovePointVisual(roi, canvas);
            }
            if (currentRoi != null)
            {
                RemovePointVisual(currentRoi, canvas);
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<PointItem> rois)
        {
            if (canvas == null) return;
            foreach (var roi in rois)
            {
                RemovePointVisual(roi, canvas);
            }
        }

        public override void UpdateRoiVisual(PointItem roi)
        {
            if (roi.EllipseElement is Ellipse ellipse)
            {
                Canvas.SetLeft(ellipse, roi.Position.X - 6);
                Canvas.SetTop(ellipse, roi.Position.Y - 6);
            }
            if (roi.LabelBorder is Border label)
            {
                Canvas.SetLeft(label, roi.Position.X + 12);
                Canvas.SetTop(label, roi.Position.Y - 8);
                if (label.Child is TextBlock tb)
                {
                    tb.Text = roi.DisplayText;
                }
            }
        }

        private void AddPointLabel(PointItem roi, Canvas canvas)
        {
            if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                canvas.Children.Remove(roi.LabelBorder);
            roi.LabelBorder = null;
            var labelText = new TextBlock
            {
                Text = roi.DisplayText,
                Foreground = new SolidColorBrush(styleDict["point"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Background = new SolidColorBrush(styleDict["point"].labelBg),
                Padding = new Thickness(4),
                Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 4, ShadowDepth = 2, Opacity = 0.5 },
                IsHitTestVisible = false
            };
            var labelBorder = new Border
            {
                Child = labelText,
                CornerRadius = new CornerRadius(4),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(labelBorder, roi.Position.X + 12);
            Canvas.SetTop(labelBorder, roi.Position.Y - 8);
            canvas.Children.Add(labelBorder);
            Canvas.SetZIndex(labelBorder, roi.ZIndex);
            roi.LabelBorder = labelBorder;
        }

        private void RemovePointVisual(PointItem roi, Canvas canvas)
        {
            if (roi.EllipseElement is Ellipse ellipse2 && canvas.Children.Contains(ellipse2))
                canvas.Children.Remove(ellipse2);
            roi.EllipseElement = null;
            if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                canvas.Children.Remove(roi.LabelBorder);
            roi.LabelBorder = null;
        }
    }
} 