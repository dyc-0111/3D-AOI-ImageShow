using HyImageShow.ImageShowWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 多邊形ROI繪製服務實作
    /// </summary>
    public class PolygonRoiDrawingService : BaseRoiDrawingService<PolygonRoiItem>
    {
        private Canvas mainCanvas;
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict =
            new Dictionary<string, (Color, Color, Color, Color)>
        {
            {"polygon", (Color.FromRgb(220, 180, 40), Color.FromRgb(191, 160, 0), Color.FromArgb(220, 240, 220, 120), Color.FromRgb(106, 74, 20))}, // 黃色系
        };

        public override void DrawRois(Canvas canvas, List<PolygonRoiItem> rois, PolygonRoiItem currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois);
            foreach (var polygonRoi in rois)
            {
                DrawSingleRoi(polygonRoi, canvas, showLabels);
            }
            if (currentRoi != null && !rois.Contains(currentRoi))
            {
                DrawPreviewRoi(currentRoi, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(PolygonRoiItem polygonRoi, Canvas canvas, bool showLabels = true)
        {
            // 先清除該多邊形ROI的所有舊視覺元素
            if (polygonRoi.Polygon != null && canvas.Children.Contains(polygonRoi.Polygon))
                canvas.Children.Remove(polygonRoi.Polygon);
            
            foreach (var dot in polygonRoi.PointDots)
            {
                if (dot != null && canvas.Children.Contains(dot))
                    canvas.Children.Remove(dot);
            }
            polygonRoi.PointDots.Clear();
            
            if (polygonRoi.RotateDot != null && canvas.Children.Contains(polygonRoi.RotateDot))
                canvas.Children.Remove(polygonRoi.RotateDot);
            
            if (polygonRoi.MainLabelBorder != null && canvas.Children.Contains(polygonRoi.MainLabelBorder))
                canvas.Children.Remove(polygonRoi.MainLabelBorder);
            
            foreach (var label in polygonRoi.VertexLabelBorders)
            {
                if (label != null && canvas.Children.Contains(label))
                    canvas.Children.Remove(label);
            }
            polygonRoi.VertexLabelBorders.Clear();

            if (polygonRoi.Points.Count > 0)
            {
                // 計算旋轉後的點座標
                Point center = polygonRoi.Center;
                List<Point> rotatedPoints = new List<Point>();
                
                foreach (var point in polygonRoi.Points)
                {
                    Point rotatedPoint = RotatePoint(point, center, polygonRoi.Angle);
                    rotatedPoints.Add(rotatedPoint);
                }

                // 繪製多邊形
                var points = new PointCollection(rotatedPoints);
                if (polygonRoi.IsCompleted && rotatedPoints.Count > 0)
                {
                    points.Add(rotatedPoints[0]); // 閉合多邊形
                }

                // 根據選中狀態選擇顏色
                Color strokeColor, fillColor, glowColor;
                double strokeThickness;
                
                strokeColor = styleDict["polygon"].line;
                fillColor = Color.FromArgb(60, 76, 175, 80);
                glowColor = styleDict["polygon"].glow;
                strokeThickness = 2;

                var polyline = new Polyline
                {
                    Points = points,
                    Stroke = new SolidColorBrush(strokeColor),
                    StrokeThickness = strokeThickness,
                    Fill = new SolidColorBrush(fillColor),
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect { Color = glowColor, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
                };
                canvas.Children.Add(polyline);
                Canvas.SetZIndex(polyline, polygonRoi.ZIndex);
                polygonRoi.Polygon = polyline;

                // 繪製頂點
                for (int i = 0; i < rotatedPoints.Count; i++)
                {
                    var dot = new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = Brushes.White,
                        Stroke = new SolidColorBrush(strokeColor),
                        StrokeThickness = 2,
                        Effect = new DropShadowEffect { Color = glowColor, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                        RenderTransform = i < polygonRoi.PointScales.Count ? polygonRoi.PointScales[i] : new ScaleTransform(1, 1),
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(dot, rotatedPoints[i].X - 6);
                    Canvas.SetTop(dot, rotatedPoints[i].Y - 6);
                    canvas.Children.Add(dot);
                    Canvas.SetZIndex(dot, polygonRoi.ZIndex);
                    polygonRoi.PointDots.Add(dot);
                }

                if (polygonRoi.IsCompleted && rotatedPoints.Count > 2)
                {
                    // 繪製旋轉點
                    var rotateDot = new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = new SolidColorBrush(Color.FromRgb(255, 235, 59)),
                        Stroke = new SolidColorBrush(styleDict["polygon"].line),
                        StrokeThickness = 2,
                        Effect = new DropShadowEffect { Color = styleDict["polygon"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                        RenderTransform = polygonRoi.RotateScale,
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        IsHitTestVisible = false
                    };
                    Point rotatePoint = GetPolygonRotatePoint(polygonRoi);
                    Canvas.SetLeft(rotateDot, rotatePoint.X - 6);
                    Canvas.SetTop(rotateDot, rotatePoint.Y - 6);
                    canvas.Children.Add(rotateDot);
                    Canvas.SetZIndex(rotateDot, polygonRoi.ZIndex);
                    polygonRoi.RotateDot = rotateDot;

                    // 繪製標籤
                    if (showLabels)
                    {
                        AddPolygonLabels(polygonRoi, canvas);
                    }
                }
            }
        }

        public override void DrawPreviewRoi(PolygonRoiItem polygonRoi, Canvas canvas, bool showLabels = true)
        {
            if (polygonRoi.Points.Count > 0)
            {
                // 計算旋轉後的點座標
                Point center = polygonRoi.Center;
                List<Point> rotatedPoints = new List<Point>();
                
                foreach (var point in polygonRoi.Points)
                {
                    Point rotatedPoint = RotatePoint(point, center, polygonRoi.Angle);
                    rotatedPoints.Add(rotatedPoint);
                }

                // 繪製多邊形（不閉合）
                var points = new PointCollection(rotatedPoints);

                var polyline = new Polyline
                {
                    Points = points,
                    Stroke = new SolidColorBrush(styleDict["polygon"].line),
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(60, 76, 175, 80)),
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect { Color = styleDict["polygon"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
                };
                canvas.Children.Add(polyline);
                Canvas.SetZIndex(polyline, polygonRoi.ZIndex);
                polygonRoi.Polygon = polyline;

                // 繪製頂點
                for (int i = 0; i < rotatedPoints.Count; i++)
                {
                    var dot = new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Fill = Brushes.White,
                        Stroke = new SolidColorBrush(styleDict["polygon"].line),
                        StrokeThickness = 2,
                        Effect = new DropShadowEffect { Color = styleDict["polygon"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                        RenderTransform = i < polygonRoi.PointScales.Count ? polygonRoi.PointScales[i] : new ScaleTransform(1, 1),
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(dot, rotatedPoints[i].X - 6);
                    Canvas.SetTop(dot, rotatedPoints[i].Y - 6);
                    canvas.Children.Add(dot);
                    Canvas.SetZIndex(dot, polygonRoi.ZIndex);
                    polygonRoi.PointDots.Add(dot);
                }

                // 為創建中的多邊形添加標籤
                if (showLabels)
                {
                    AddPolygonLabels(polygonRoi, canvas);
                }
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<PolygonRoiItem> rois)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Polygon != null && canvas.Children.Contains(roi.Polygon))
                    canvas.Children.Remove(roi.Polygon);
                foreach (var dot in roi.PointDots)
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                roi.PointDots.Clear();
                if (roi.RotateDot != null && canvas.Children.Contains(roi.RotateDot))
                    canvas.Children.Remove(roi.RotateDot);
                roi.RotateDot = null;
                if (roi.MainLabelBorder != null && canvas.Children.Contains(roi.MainLabelBorder))
                    canvas.Children.Remove(roi.MainLabelBorder);
                roi.MainLabelBorder = null;
                foreach (var label in roi.VertexLabelBorders)
                {
                    if (label != null && canvas.Children.Contains(label))
                        canvas.Children.Remove(label);
                }
                roi.VertexLabelBorders.Clear();
            }
        }

        /// <summary>
        /// 添加多邊形ROI標籤
        /// </summary>
        private void AddPolygonLabels(PolygonRoiItem polygonRoi, Canvas canvas)
        {
            Point center = polygonRoi.Center;
            
            // 為每個頂點添加座標標籤
            for (int i = 0; i < polygonRoi.Points.Count; i++)
            {
                Point originalPoint = polygonRoi.Points[i];
                Point rotatedPoint = RotatePoint(originalPoint, center, polygonRoi.Angle);
                
                var vertexLabelText = new TextBlock
                {
                    Text = $"P{i + 1}({rotatedPoint.X:F0},{rotatedPoint.Y:F0})",
                    Foreground = new SolidColorBrush(styleDict["polygon"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Background = new SolidColorBrush(styleDict["polygon"].labelBg),
                    Padding = new Thickness(3),
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 3, ShadowDepth = 1, Opacity = 0.5 },
                    IsHitTestVisible = false
                };
                var vertexLabelBorder = new Border
                {
                    Child = vertexLabelText,
                    CornerRadius = new CornerRadius(3),
                    IsHitTestVisible = false
                };
                
                // 計算頂點標籤的位置偏移
                var (vertexOffsetX, vertexOffsetY) = GetVertexLabelOffset(rotatedPoint, canvas.ActualWidth, canvas.ActualHeight);
                Canvas.SetLeft(vertexLabelBorder, rotatedPoint.X + vertexOffsetX);
                Canvas.SetTop(vertexLabelBorder, rotatedPoint.Y + vertexOffsetY);
                canvas.Children.Add(vertexLabelBorder);
                Canvas.SetZIndex(vertexLabelBorder, polygonRoi.ZIndex);
                polygonRoi.VertexLabelBorders.Add(vertexLabelBorder);
            }
        }

        /// <summary>
        /// 獲取多邊形旋轉點
        /// </summary>
        private Point GetPolygonRotatePoint(PolygonRoiItem polygonRoi)
        {
            if (polygonRoi.Points.Count == 0) return new Point();
            
            Point center = polygonRoi.Center;
            double maxDistance = polygonRoi.Points.Max(p => (p - center).Length);
            Point baseRotatePoint = (Point)(center + new Vector(0, -maxDistance - 20));
            
            // 將旋轉點也進行旋轉變換
            return RotatePoint(baseRotatePoint, center, polygonRoi.Angle);
        }

        /// <summary>
        /// 根據端點位置決定標註偏移
        /// </summary>
        private (double offsetX, double offsetY) GetLabelOffset(Point pt, double canvasWidth, double canvasHeight)
        {
            double offsetX = 25, offsetY = -25;
            // 右下象限
            if (pt.X > canvasWidth * 0.66 && pt.Y > canvasHeight * 0.66) { offsetX = -90; offsetY = -40; }
            // 右上象限
            else if (pt.X > canvasWidth * 0.66 && pt.Y < canvasHeight * 0.33) { offsetX = -90; offsetY = 25; }
            // 左下象限
            else if (pt.X < canvasWidth * 0.33 && pt.Y > canvasHeight * 0.66) { offsetX = 25; offsetY = -40; }
            // 左上象限
            else if (pt.X < canvasWidth * 0.33 && pt.Y < canvasHeight * 0.33) { offsetX = 25; offsetY = 25; }
            // 右側
            else if (pt.X > canvasWidth * 0.66) { offsetX = -90; }
            // 左側
            else if (pt.X < canvasWidth * 0.33) { offsetX = 25; }
            // 下方
            else if (pt.Y > canvasHeight * 0.66) { offsetY = -40; }
            // 上方
            else if (pt.Y < canvasHeight * 0.33) { offsetY = 25; }
            return (offsetX, offsetY);
        }

        /// <summary>
        /// 根據頂點位置決定頂點標籤偏移
        /// </summary>
        private (double offsetX, double offsetY) GetVertexLabelOffset(Point pt, double canvasWidth, double canvasHeight)
        {
            double offsetX = 15, offsetY = -15;
            // 右下象限
            if (pt.X > canvasWidth * 0.66 && pt.Y > canvasHeight * 0.66) { offsetX = -60; offsetY = -25; }
            // 右上象限
            else if (pt.X > canvasWidth * 0.66 && pt.Y < canvasHeight * 0.33) { offsetX = -60; offsetY = 15; }
            // 左下象限
            else if (pt.X < canvasWidth * 0.33 && pt.Y > canvasHeight * 0.66) { offsetX = 15; offsetY = -25; }
            // 左上象限
            else if (pt.X < canvasWidth * 0.33 && pt.Y < canvasHeight * 0.33) { offsetX = 15; offsetY = 15; }
            // 右側
            else if (pt.X > canvasWidth * 0.66) { offsetX = -60; }
            // 左側
            else if (pt.X < canvasWidth * 0.33) { offsetX = 15; }
            // 下方
            else if (pt.Y > canvasHeight * 0.66) { offsetY = -25; }
            // 上方
            else if (pt.Y < canvasHeight * 0.33) { offsetY = 15; }
            return (offsetX, offsetY);
        }

        /// <summary>
        /// 旋轉點座標
        /// </summary>
        private Point RotatePoint(Point point, Point center, double angle)
        {
            double angleRad = angle * Math.PI / 180;
            double cosAngle = Math.Cos(angleRad);
            double sinAngle = Math.Sin(angleRad);

            double dx = point.X - center.X;
            double dy = point.Y - center.Y;

            double rotatedX = center.X + (dx * cosAngle - dy * sinAngle);
            double rotatedY = center.Y + (dx * sinAngle + dy * cosAngle);

            return new Point(rotatedX, rotatedY);
        }

        /// <summary>
        /// 更新多邊形ROI視覺效果
        /// </summary>
        public void UpdatePolygonVisual(PolygonRoiItem polygonRoiItem)
        {
            if (polygonRoiItem == null || mainCanvas == null) return;

            // 重新計算旋轉後的點座標
            Point center = polygonRoiItem.Center;
            List<Point> rotatedPoints = new List<Point>();
            
            foreach (var point in polygonRoiItem.Points)
            {
                Point rotatedPoint = RotatePoint(point, center, polygonRoiItem.Angle);
                rotatedPoints.Add(rotatedPoint);
            }

            // 更新多邊形位置和形狀
            if (polygonRoiItem.Polygon != null)
            {
                var points = new PointCollection(rotatedPoints);
                if (polygonRoiItem.IsCompleted && rotatedPoints.Count > 0)
                {
                    points.Add(rotatedPoints[0]); // 閉合多邊形
                }
                polygonRoiItem.Polygon.Points = points;
            }
            else if (rotatedPoints.Count > 0)
            {
                // 如果多邊形元素不存在，創建它（用於創建中的多邊形）
                var points = new PointCollection(rotatedPoints);
                var polyline = new Polyline
                {
                    Points = points,
                    Stroke = new SolidColorBrush(styleDict["polygon"].line),
                    StrokeThickness = 2,
                    Fill = new SolidColorBrush(Color.FromArgb(60, 76, 175, 80)),
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect { Color = styleDict["polygon"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
                };
                
                mainCanvas.Children.Add(polyline);
                Canvas.SetZIndex(polyline, polygonRoiItem.ZIndex);
                polygonRoiItem.Polygon = polyline;
            }

            // 更新頂點控制點位置
            for (int i = 0; i < rotatedPoints.Count && i < polygonRoiItem.PointDots.Count; i++)
            {
                if (polygonRoiItem.PointDots[i] != null)
                {
                    Canvas.SetLeft(polygonRoiItem.PointDots[i], rotatedPoints[i].X - 6);
                    Canvas.SetTop(polygonRoiItem.PointDots[i], rotatedPoints[i].Y - 6);
                }
            }

            // 添加新的頂點控制點（如果數量不匹配）
            while (polygonRoiItem.PointDots.Count < rotatedPoints.Count)
            {
                int index = polygonRoiItem.PointDots.Count;
                var dot = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(styleDict["polygon"].line),
                    StrokeThickness = 2,
                    Effect = new DropShadowEffect { Color = styleDict["polygon"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = index < polygonRoiItem.PointScales.Count ? polygonRoiItem.PointScales[index] : new ScaleTransform(1, 1),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = false
                };
                
                Canvas.SetLeft(dot, rotatedPoints[index].X - 6);
                Canvas.SetTop(dot, rotatedPoints[index].Y - 6);
                mainCanvas.Children.Add(dot);
                Canvas.SetZIndex(dot, polygonRoiItem.ZIndex);
                polygonRoiItem.PointDots.Add(dot);
            }

            // 更新旋轉點位置
            if (polygonRoiItem.RotateDot != null && polygonRoiItem.IsCompleted)
            {
                Point rotatePoint = GetPolygonRotatePoint(polygonRoiItem);
                Canvas.SetLeft(polygonRoiItem.RotateDot, rotatePoint.X - 6);
                Canvas.SetTop(polygonRoiItem.RotateDot, rotatePoint.Y - 6);
            }

            // 更新頂點標籤位置和內容
            for (int i = 0; i < rotatedPoints.Count && i < polygonRoiItem.VertexLabelBorders.Count; i++)
            {
                if (polygonRoiItem.VertexLabelBorders[i] != null)
                {
                    Point originalPoint = polygonRoiItem.Points[i];
                    Point rotatedPoint = rotatedPoints[i];
                    
                    // 更新標籤內容 - 顯示旋轉後的座標
                    if (polygonRoiItem.VertexLabelBorders[i].Child is TextBlock textBlock)
                    {
                        textBlock.Text = $"P{i + 1}({rotatedPoint.X:F0},{rotatedPoint.Y:F0})";
                    }
                    
                    // 更新標籤位置
                    var (vertexOffsetX, vertexOffsetY) = GetVertexLabelOffset(rotatedPoint, mainCanvas.ActualWidth, mainCanvas.ActualHeight);
                    Canvas.SetLeft(polygonRoiItem.VertexLabelBorders[i], rotatedPoint.X + vertexOffsetX);
                    Canvas.SetTop(polygonRoiItem.VertexLabelBorders[i], rotatedPoint.Y + vertexOffsetY);
                }
            }

            // 添加新的頂點標籤（如果數量不匹配）
            while (polygonRoiItem.VertexLabelBorders.Count < rotatedPoints.Count)
            {
                int index = polygonRoiItem.VertexLabelBorders.Count;
                Point originalPoint = polygonRoiItem.Points[index];
                Point rotatedPoint = rotatedPoints[index];
                
                var vertexLabelText = new TextBlock
                {
                    Text = $"P{index + 1}({rotatedPoint.X:F0},{rotatedPoint.Y:F0})",
                    Foreground = new SolidColorBrush(styleDict["polygon"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 10,
                    Background = new SolidColorBrush(styleDict["polygon"].labelBg),
                    Padding = new Thickness(3),
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 3, ShadowDepth = 1, Opacity = 0.5 },
                    IsHitTestVisible = false
                };
                var vertexLabelBorder = new Border
                {
                    Child = vertexLabelText,
                    CornerRadius = new CornerRadius(3),
                    IsHitTestVisible = false
                };
                
                var (vertexOffsetX, vertexOffsetY) = GetVertexLabelOffset(rotatedPoint, mainCanvas.ActualWidth, mainCanvas.ActualHeight);
                Canvas.SetLeft(vertexLabelBorder, rotatedPoint.X + vertexOffsetX);
                Canvas.SetTop(vertexLabelBorder, rotatedPoint.Y + vertexOffsetY);
                mainCanvas.Children.Add(vertexLabelBorder);
                Canvas.SetZIndex(vertexLabelBorder, polygonRoiItem.ZIndex);
                polygonRoiItem.VertexLabelBorders.Add(vertexLabelBorder);
            }
        }

        public void SetMainCanvas(Canvas canvas)
        {

        }
    }
} 