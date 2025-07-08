using HyImageShow.ImageShowWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 圓弧ROI繪製服務實現
    /// </summary>
    public class CircularArcRoiDrawingService : BaseRoiDrawingService<CircularArcRoiItem>
    {
        private Canvas mainCanvas;
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;
        private Dictionary<CircularArcRoiItem, List<UIElement>> circularArcRoiElements;

        public CircularArcRoiDrawingService()
        {
            styleDict = new Dictionary<string, (Color, Color, Color, Color)>
            {
                {"circulararc", (Color.FromRgb(255, 64, 129), Color.FromRgb(255, 64, 129), Color.FromRgb(255, 64, 129), Colors.White)},
            };
            circularArcRoiElements = new Dictionary<CircularArcRoiItem, List<UIElement>>();
        }

        public override void DrawRois(Canvas canvas, List<CircularArcRoiItem> rois, CircularArcRoiItem currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois, currentRoi);
            foreach (var arc in rois)
            {
                DrawSingleRoi(arc, canvas, showLabels);
            }
            if (currentRoi != null && !rois.Contains(currentRoi))
            {
                DrawPreviewRoi(currentRoi, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(CircularArcRoiItem arc, Canvas canvas, bool showLabels = true)
        {
            DrawCircularArcRoi(canvas, arc, showLabels);
        }

        public override void DrawPreviewRoi(CircularArcRoiItem roi, Canvas canvas, bool showLabels = true)
        {
            if (roi == null) return;

            // 先清除舊的點與輔助線
            var elementsToRemove = new List<UIElement>();
            foreach (var child in canvas.Children)
            {
                if (child is Ellipse ellipse && ellipse.Tag == roi)
                    elementsToRemove.Add(ellipse);
                // 不再移除/處理 Path 或 Line
            }
            foreach (var el in elementsToRemove)
                canvas.Children.Remove(el);

            // 確保 ControlDots 有正確初始化
            if (roi.ControlDots == null || roi.ControlDots.Length != 3)
                roi.ControlDots = new Ellipse[3];

            // 根據目前有幾個點來畫，只畫控制點，不畫預覽線
            int count = 0;
            if (roi.StartPoint != default(Point)) count++;
            if (roi.EndPoint != default(Point)) count++;
            if (roi.UserMidPoint != default(Point)) count++;

            if (count >= 1)
                roi.ControlDots[0] = DrawHelperDot(canvas, roi.StartPoint, roi, 0);
            if (count >= 2)
                roi.ControlDots[1] = DrawHelperDot(canvas, roi.EndPoint, roi, 1);
            if (count == 3)
                roi.ControlDots[2] = DrawHelperDot(canvas, roi.UserMidPoint, roi, 2);
        }

        public override void ClearRois(Canvas canvas, IEnumerable<CircularArcRoiItem> rois, CircularArcRoiItem currentRoi = null)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Path != null && canvas.Children.Contains(roi.Path))
                    canvas.Children.Remove(roi.Path);
                foreach (var dot in roi.ControlDots ?? new Ellipse[0])
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                    canvas.Children.Remove(roi.LabelBorder);
                roi.Path = null;
                roi.LabelBorder = null;
                roi.ControlDots = new Ellipse[3];
            }
            if (currentRoi != null)
            {
                if (currentRoi.Path != null && canvas.Children.Contains(currentRoi.Path))
                    canvas.Children.Remove(currentRoi.Path);
                foreach (var dot in currentRoi.ControlDots ?? new Ellipse[0])
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                if (currentRoi.LabelBorder != null && canvas.Children.Contains(currentRoi.LabelBorder))
                    canvas.Children.Remove(currentRoi.LabelBorder);
                currentRoi.Path = null;
                currentRoi.LabelBorder = null;
                currentRoi.ControlDots = new Ellipse[3];
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<CircularArcRoiItem> rois)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Path != null && canvas.Children.Contains(roi.Path))
                    canvas.Children.Remove(roi.Path);
                foreach (var dot in roi.ControlDots ?? new Ellipse[0])
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                    canvas.Children.Remove(roi.LabelBorder);
                roi.Path = null;
                roi.LabelBorder = null;
                roi.ControlDots = new Ellipse[3];
            }
        }

        public void DrawCircularArcRoi(Canvas canvas, CircularArcRoiItem circularArcRoi, bool showLabels)
        {
            if (circularArcRoi == null || !circularArcRoi.IsCompleted) return;

            // 先清除該圓弧ROI的所有舊視覺元素
            if (circularArcRoi.Path != null && canvas.Children.Contains(circularArcRoi.Path))
                canvas.Children.Remove(circularArcRoi.Path);
            foreach (var dot in circularArcRoi.ControlDots ?? new Ellipse[0])
            {
                if (dot != null && canvas.Children.Contains(dot))
                    canvas.Children.Remove(dot);
            }
            if (circularArcRoi.LabelBorder != null && canvas.Children.Contains(circularArcRoi.LabelBorder))
                canvas.Children.Remove(circularArcRoi.LabelBorder);

            // 建立圓弧路徑
            PathFigure pathFigure = new PathFigure();
            pathFigure.StartPoint = circularArcRoi.StartPoint;
            ArcSegment arcSegment = new ArcSegment
            {
                Point = circularArcRoi.EndPoint,
                Size = new Size(circularArcRoi.Radius, circularArcRoi.Radius),
                IsLargeArc = circularArcRoi.IsLargeArc,
                SweepDirection = circularArcRoi.SweepDirection
            };
            pathFigure.Segments.Add(arcSegment);
            PathGeometry pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            var path = new Path
            {
                Data = pathGeometry,
                Stroke = new SolidColorBrush(styleDict["circulararc"].line),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(60, 255, 64, 129)),
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["circulararc"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 },
                Tag = circularArcRoi
            };
            canvas.Children.Add(path);
            Canvas.SetZIndex(path, circularArcRoi.ZIndex);
            circularArcRoi.Path = path;

            // 畫控制點
            Point[] points = { circularArcRoi.StartPoint, circularArcRoi.EndPoint, circularArcRoi.UserMidPoint };
            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = (i == 2) ? new SolidColorBrush(Color.FromRgb(255, 235, 59)) : Brushes.White, // 第三點黃色，其餘白色
                    Stroke = new SolidColorBrush(styleDict["circulararc"].line),
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = styleDict["circulararc"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = new ScaleTransform(1, 1),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = true,
                    Tag = circularArcRoi
                };
                Canvas.SetLeft(dot, points[i].X - 9);
                Canvas.SetTop(dot, points[i].Y - 9);
                canvas.Children.Add(dot);
                Canvas.SetZIndex(dot, circularArcRoi.ZIndex);
                if (circularArcRoi.ControlDots == null) circularArcRoi.ControlDots = new Ellipse[3];
                circularArcRoi.ControlDots[i] = dot;
            }

            // 只有 showLabels 為 true 時才畫 label
            if (showLabels)
                AddCircularArcLabels(circularArcRoi, canvas);
        }

        private void AddCircularArcLabels(CircularArcRoiItem circularArcRoi, Canvas canvas)
        {
            string start = $"起點: ({circularArcRoi.StartPoint.X:F0}, {circularArcRoi.StartPoint.Y:F0})";
            string mid = $"中點: ({circularArcRoi.UserMidPoint.X:F0}, {circularArcRoi.UserMidPoint.Y:F0})";
            string end = $"終點: ({circularArcRoi.EndPoint.X:F0}, {circularArcRoi.EndPoint.Y:F0})";
            var labelText = new TextBlock
            {
                Text = $"{start}\n{mid}\n{end}\n弧長: {circularArcRoi.ArcLength:F1}\n弦長: {circularArcRoi.ChordLength:F1}",
                Foreground = new SolidColorBrush(styleDict["circulararc"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                Background = Brushes.Transparent,
                Opacity = 1.0,
                Padding = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };
            var labelBorder = new Border
            {
                Background = new SolidColorBrush(styleDict["circulararc"].labelBg),
                CornerRadius = new CornerRadius(8),
                Child = labelText,
                Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                Opacity = 0.98,
                Padding = new Thickness(6, 2, 6, 2),
                IsHitTestVisible = false,
                Tag = circularArcRoi
            };
            // 圓弧中點計算
            double midAngle = (circularArcRoi.StartAngle + circularArcRoi.EndAngle) / 2;
            double x = circularArcRoi.CenterPoint.X + circularArcRoi.Radius * Math.Cos(midAngle * Math.PI / 180);
            double y = circularArcRoi.CenterPoint.Y + circularArcRoi.Radius * Math.Sin(midAngle * Math.PI / 180);
            Point labelPos = new Point(x, y);
            // 先加到Canvas再取寬高
            canvas.Children.Add(labelBorder);
            Canvas.SetZIndex(labelBorder, circularArcRoi.ZIndex);
            labelBorder.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = labelBorder.DesiredSize;
            Canvas.SetLeft(labelBorder, labelPos.X - size.Width / 2);
            Canvas.SetTop(labelBorder, labelPos.Y - size.Height / 2);
            circularArcRoi.LabelBorder = labelBorder;
        }

        private Ellipse DrawHelperDot(Canvas canvas, Point pt, CircularArcRoiItem roi, int idx)
        {
            var dot = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = (idx == 2) ? new SolidColorBrush(Color.FromRgb(255, 235, 59)) : Brushes.White,
                Stroke = new SolidColorBrush(styleDict["circulararc"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["circulararc"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = new ScaleTransform(1, 1),
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = true,
                Tag = roi
            };
            Canvas.SetLeft(dot, pt.X - 9);
            Canvas.SetTop(dot, pt.Y - 9);
            canvas.Children.Add(dot);
            Canvas.SetZIndex(dot, roi.ZIndex);
            return dot;
        }

        public void UpdateCircularArcVisual(CircularArcRoiItem circularArcRoi)
        {
            if (circularArcRoi == null || !circularArcRoi.IsCompleted) return;

            // 移除舊的視覺元素
            if (circularArcRoiElements.ContainsKey(circularArcRoi))
            {
                foreach (var element in circularArcRoiElements[circularArcRoi])
                {
                    var parent = VisualTreeHelper.GetParent(element) as Canvas;
                    if (parent != null)
                    {
                        parent.Children.Remove(element);
                    }
                }
            }

            // 重新繪製 - 需要找到對應的Canvas
            // 這裡我們需要從外部傳入Canvas，或者通過其他方式獲取
            // 暫時跳過重新繪製，因為我們沒有Canvas引用
        }

        public void ClearCircularArcRois(Canvas canvas)
        {
            if (canvas == null) return;

            var elementsToRemove = new List<UIElement>();
            foreach (var child in canvas.Children)
            {
                if (child is Path path && path.Tag is CircularArcRoiItem)
                {
                    elementsToRemove.Add((UIElement)child);
                }
                else if (child is Ellipse ellipse && ellipse.Tag is CircularArcRoiItem)
                {
                    elementsToRemove.Add((UIElement)child);
                }
                else if (child is Line line && line.Tag is CircularArcRoiItem)
                {
                    elementsToRemove.Add((UIElement)child);
                }
            }

            foreach (var element in elementsToRemove)
            {
                canvas.Children.Remove(element);
            }

            circularArcRoiElements.Clear();
        }

        public void RemoveCircularArcRoiVisual(CircularArcRoiItem circularArcRoi, Canvas canvas)
        {
            if (circularArcRoi == null || canvas == null) return;

            if (circularArcRoiElements.ContainsKey(circularArcRoi))
            {
                foreach (var element in circularArcRoiElements[circularArcRoi])
                {
                    canvas.Children.Remove(element);
                }
                circularArcRoiElements.Remove(circularArcRoi);
            }
        }

        private Path CreateCircularArcPath(CircularArcRoiItem circularArcRoi)
        {
            if (!circularArcRoi.IsCompleted) return null;

            var path = new Path
            {
                Tag = circularArcRoi,
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure
            {
                StartPoint = circularArcRoi.StartPoint,
                IsClosed = false
            };

            var arcSegment = new ArcSegment
            {
                Point = circularArcRoi.EndPoint,
                Size = new Size(circularArcRoi.Radius, circularArcRoi.Radius),
                IsLargeArc = circularArcRoi.IsLargeArc,
                SweepDirection = circularArcRoi.SweepDirection
            };

            figure.Segments.Add(arcSegment);
            geometry.Figures.Add(figure);
            path.Data = geometry;

            return path;
        }

        public void SetMainCanvas(Canvas canvas)
        {

        }
    }
}