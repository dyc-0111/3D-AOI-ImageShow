using HyImageShow.ImageShowWPF;
using HyImageShow.ImageShowWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 貝塞爾弧線ROI繪製服務實現
    /// </summary>
    public class BezierArcRoiDrawingService : BaseRoiDrawingService<BezierArcRoiItem>
    {
        private Canvas mainCanvas;
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;
        // 新增：管理每個 ROI 的 UI 元素集合
        private readonly Dictionary<BezierArcRoiItem, List<UIElement>> bezierArcRoiElements = new Dictionary<BezierArcRoiItem, List<UIElement>>();
        private readonly SolidColorBrush arcStroke = new SolidColorBrush(Color.FromRgb(40, 170, 110));
        private readonly SolidColorBrush arcFill = new SolidColorBrush(Color.FromArgb(60, 40, 170, 110));
        private readonly Color arcGlow = Color.FromRgb(30, 122, 74);

        public BezierArcRoiDrawingService()
        {
            styleDict = new Dictionary<string, (Color, Color, Color, Color)>
            {
                {"arc", (Color.FromRgb(40, 170, 110), Color.FromRgb(30, 122, 74), Color.FromArgb(220, 180, 240, 200), Color.FromRgb(30, 122, 74))}, // 綠色
            };
        }

        public void DrawRois(Canvas canvas, ObservableCollection<BezierArcRoiItem> rois, BezierArcRoiItem currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois, currentRoi);
            foreach (var roi in rois)
            {
                if (roi.IsCompleted)
                {
                    DrawSingleRoi(roi, canvas, showLabels);
                }
            }
            if (currentRoi != null && !rois.Contains(currentRoi))
            {
                DrawPreviewRoi(currentRoi, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(BezierArcRoiItem roi, Canvas canvas, bool showLabels = true)
        {
            if (roi == null || !roi.IsCompleted) return;
            // 清除舊視覺元素
            if (roi.Path != null && canvas.Children.Contains(roi.Path))
                canvas.Children.Remove(roi.Path);
            foreach (var dot in roi.ControlDots)
            {
                if (dot != null && canvas.Children.Contains(dot))
                    canvas.Children.Remove(dot);
            }
            // 畫弧線
            PathFigure pathFigure = new PathFigure { StartPoint = roi.StartPoint };
            pathFigure.Segments.Add(new QuadraticBezierSegment(roi.MiddlePoint, roi.EndPoint, true));
            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            var path = new Path
            {
                Data = pathGeometry,
                Stroke = arcStroke,
                StrokeThickness = 2,
                Fill = arcFill,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = arcGlow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 },
                Tag = roi
            };
            canvas.Children.Add(path);
            Canvas.SetZIndex(path, roi.ZIndex);
            roi.Path = path;
            // 畫控制點
            Point[] points = { roi.StartPoint, roi.EndPoint, roi.MiddlePoint };
            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = (i == 2) ? new SolidColorBrush(Color.FromRgb(255, 235, 59)) : Brushes.White,
                    Stroke = arcStroke,
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = arcGlow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = new ScaleTransform(1, 1),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = true,
                    Tag = roi
                };
                Canvas.SetLeft(dot, points[i].X - 9);
                Canvas.SetTop(dot, points[i].Y - 9);
                canvas.Children.Add(dot);
                Canvas.SetZIndex(dot, roi.ZIndex);
                roi.ControlDots[i] = dot;
            }
            // 標籤
            if (showLabels)
            {
                var label = new TextBlock
                {
                    Text = roi.Point1Text + "\n" + roi.Point2Text + "\n" + roi.Point3Text + "\n" + roi.SizeText,
                    Foreground = arcStroke,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Background = Brushes.Transparent,
                    Opacity = 1.0,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var labelBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(220, 180, 240, 200)),
                    CornerRadius = new CornerRadius(8),
                    Child = label,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2),
                    IsHitTestVisible = false,
                    Tag = roi
                };
                // 標籤位置
                var mid = GetQuadraticBezierPoint(roi.StartPoint, roi.MiddlePoint, roi.EndPoint, 0.5);
                var tangent = 2 * (1 - 0.5) * (roi.MiddlePoint - roi.StartPoint) + 2 * 0.5 * (roi.EndPoint - roi.MiddlePoint);
                var normal = new Vector(-tangent.Y, tangent.X);
                if (normal.Length > 0.1) normal.Normalize();
                double offset = 40;
                var labelPos = mid + normal * offset;
                canvas.Children.Add(labelBorder);
                Canvas.SetZIndex(labelBorder, roi.ZIndex);
                labelBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var size = labelBorder.DesiredSize;
                Canvas.SetLeft(labelBorder, labelPos.X - size.Width / 2);
                Canvas.SetTop(labelBorder, labelPos.Y - size.Height / 2);
                roi.LabelBorder = labelBorder;
            }
        }

        public override void DrawPreviewRoi(BezierArcRoiItem roi, Canvas canvas, bool showLabels = true)
        {
            if (roi == null) return;
            // 清除舊視覺元素
            if (roi.Path != null && canvas.Children.Contains(roi.Path))
                canvas.Children.Remove(roi.Path);
            foreach (var dot in roi.ControlDots)
            {
                if (dot != null && canvas.Children.Contains(dot))
                    canvas.Children.Remove(dot);
            }
            int clickCount = 0;
            if (roi.StartPoint != new Point(0, 0)) clickCount++;
            if (roi.EndPoint != new Point(0, 0)) clickCount++;
            if (roi.MiddlePoint != new Point(0, 0)) clickCount++;
            if (clickCount < 1) return;
            if (clickCount == 1)
            {
                var dot = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.White,
                    Stroke = arcStroke,
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = arcGlow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = new ScaleTransform(1, 1),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = true,
                    Tag = roi
                };
                Canvas.SetLeft(dot, roi.StartPoint.X - 9);
                Canvas.SetTop(dot, roi.StartPoint.Y - 9);
                canvas.Children.Add(dot);
                Canvas.SetZIndex(dot, roi.ZIndex);
                roi.ControlDots[0] = dot;
                return;
            }
            // 畫預覽線或弧
            PathFigure pathFigure = new PathFigure { StartPoint = roi.StartPoint };
            if (clickCount == 2)
            {
                pathFigure.Segments.Add(new LineSegment(roi.EndPoint, true));
            }
            else if (clickCount == 3)
            {
                pathFigure.Segments.Add(new QuadraticBezierSegment(roi.MiddlePoint, roi.EndPoint, true));
            }
            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            var path = new Path
            {
                Data = pathGeometry,
                Stroke = arcStroke,
                StrokeThickness = 2,
                Fill = arcFill,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = arcGlow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            canvas.Children.Add(path);
            Canvas.SetZIndex(path, roi.ZIndex);
            roi.Path = path;
            Point[] points = { roi.StartPoint, roi.EndPoint, roi.MiddlePoint };
            for (int i = 0; i < clickCount; i++)
            {
                var dot = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = (i == 2) ? new SolidColorBrush(Color.FromRgb(255, 235, 59)) : Brushes.White,
                    Stroke = arcStroke,
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = arcGlow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = new ScaleTransform(1, 1),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = true,
                    Tag = roi
                };
                Canvas.SetLeft(dot, points[i].X - 9);
                Canvas.SetTop(dot, points[i].Y - 9);
                canvas.Children.Add(dot);
                Canvas.SetZIndex(dot, roi.ZIndex);
                roi.ControlDots[i] = dot;
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<BezierArcRoiItem> rois, BezierArcRoiItem currentRoi = null)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Path != null && canvas.Children.Contains(roi.Path))
                    canvas.Children.Remove(roi.Path);
                foreach (var dot in roi.ControlDots)
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                    canvas.Children.Remove(roi.LabelBorder);
                roi.Path = null;
                roi.LabelBorder = null;
                for (int i = 0; i < roi.ControlDots.Length; i++)
                    roi.ControlDots[i] = null;
            }
            if (currentRoi != null)
            {
                if (currentRoi.Path != null && canvas.Children.Contains(currentRoi.Path))
                    canvas.Children.Remove(currentRoi.Path);
                foreach (var dot in currentRoi.ControlDots)
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                if (currentRoi.LabelBorder != null && canvas.Children.Contains(currentRoi.LabelBorder))
                    canvas.Children.Remove(currentRoi.LabelBorder);
                currentRoi.Path = null;
                currentRoi.LabelBorder = null;
                for (int i = 0; i < currentRoi.ControlDots.Length; i++)
                    currentRoi.ControlDots[i] = null;
            }
        }

        private Point GetQuadraticBezierPoint(Point p0, Point p1, Point p2, double t)
        {
            double x = (1 - t) * (1 - t) * p0.X + 2 * (1 - t) * t * p1.X + t * t * p2.X;
            double y = (1 - t) * (1 - t) * p0.Y + 2 * (1 - t) * t * p1.Y + t * t * p2.Y;
            return new Point(x, y);
        }

        /// <summary>
        /// 更新貝塞爾弧線ROI視覺效果
        /// </summary>
        public void UpdateBezierArcVisual(BezierArcRoiItem bezierArcRoiItem)
        {
            if (bezierArcRoiItem == null || mainCanvas == null) return;

            // 清除舊的視覺元素
            if (bezierArcRoiElements.ContainsKey(bezierArcRoiItem))
            {
                foreach (var elem in bezierArcRoiElements[bezierArcRoiItem])
                {
                    if (mainCanvas.Children.Contains(elem))
                        mainCanvas.Children.Remove(elem);
                }
                bezierArcRoiElements[bezierArcRoiItem].Clear();
                bezierArcRoiElements.Remove(bezierArcRoiItem);
            }
            if (bezierArcRoiItem.Path != null && mainCanvas.Children.Contains(bezierArcRoiItem.Path))
                mainCanvas.Children.Remove(bezierArcRoiItem.Path);
            foreach (var dot in bezierArcRoiItem.ControlDots)
            {
                if (dot != null && mainCanvas.Children.Contains(dot))
                    mainCanvas.Children.Remove(dot);
            }
            if (bezierArcRoiItem.LabelBorder != null && mainCanvas.Children.Contains(bezierArcRoiItem.LabelBorder))
                mainCanvas.Children.Remove(bezierArcRoiItem.LabelBorder);

            // 重新繪製
            if (bezierArcRoiItem.IsCompleted)
            {
                DrawSingleRoi(bezierArcRoiItem, mainCanvas, true);
            }
            else
            {
                DrawPreviewRoi(bezierArcRoiItem, mainCanvas, true);
            }
        }

        public void SetMainCanvas(Canvas canvas)
        {

        }
    }
} 