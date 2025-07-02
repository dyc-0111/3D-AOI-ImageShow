using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using HyImageShow.ImageShowWPF.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Effects;
using System.Windows.Media.TextFormatting;
using System.Windows.Media.Animation;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 矩形ROI繪製服務實現類
    /// </summary>
    public class RotRectRoiDrawingService : BaseRoiDrawingService<RectRoiItem>
    {
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;

        public RotRectRoiDrawingService()
        {
            // 初始化樣式字典，與主視圖保持一致
            styleDict = new Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)>
            {
               {"rotRect", (Color.FromRgb(33, 150, 243), Color.FromRgb(25, 118, 210), Color.FromArgb(220, 187, 222, 251), Color.FromRgb(13, 71, 161))}, // 藍
            };
        }

        public override void DrawRois(Canvas canvas, List<RectRoiItem> rois, RectRoiItem currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois);
            foreach (var rotRectItem in rois.Where(r => r.IsCompleted))
            {
                DrawSingleRoi(rotRectItem, canvas, showLabels);
            }
            if (currentRoi != null && !currentRoi.IsCompleted)
            {
                DrawCurrentRotRect(currentRoi, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(RectRoiItem rotRectItem, Canvas canvas, bool showLabels = true)
        {
            // 先清除該矩形ROI的所有舊視覺元素
            if (rotRectItem.Polygon != null && canvas.Children.Contains(rotRectItem.Polygon))
                canvas.Children.Remove(rotRectItem.Polygon);
            
            for (int i = 0; i < 4; i++)
            {
                if (rotRectItem.CornerDots[i] != null && canvas.Children.Contains(rotRectItem.CornerDots[i]))
                    canvas.Children.Remove(rotRectItem.CornerDots[i]);
            }
            
            if (rotRectItem.RotateDot != null && canvas.Children.Contains(rotRectItem.RotateDot))
                canvas.Children.Remove(rotRectItem.RotateDot);
            
            if (rotRectItem.LabelBorder != null && canvas.Children.Contains(rotRectItem.LabelBorder))
                canvas.Children.Remove(rotRectItem.LabelBorder);

            // 計算矩形的四個角落點
            Point[] corners = CalculateCorners(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);

            // 繪製矩形
            var polygon = new Polygon
            {
                Points = new PointCollection(corners),
                Stroke = new SolidColorBrush(styleDict["rotRect"].line),
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Opacity = 0.7,
                Effect = new DropShadowEffect { Color = styleDict["rotRect"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 },
                IsHitTestVisible = false
            };
            canvas.Children.Add(polygon);
            Canvas.SetZIndex(polygon, rotRectItem.ZIndex);
            rotRectItem.Polygon = polygon;

            // 繪製角落控制點
            for (int i = 0; i < 4; i++)
            {
                var cornerDot = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(styleDict["rotRect"].line),
                    StrokeThickness = 2,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = styleDict["rotRect"].glow, BlurRadius = 6, ShadowDepth = 0, Opacity = 0.7 },
                    RenderTransform = rotRectItem.CornerScales[i],
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(cornerDot, corners[i].X - 6);
                Canvas.SetTop(cornerDot, corners[i].Y - 6);
                canvas.Children.Add(cornerDot);
                Canvas.SetZIndex(cornerDot, rotRectItem.ZIndex);
                rotRectItem.CornerDots[i] = cornerDot;
            }

            // 繪製旋轉控制點
            Point rotatePoint = CalculateRotatePoint(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);
            var rotateDot = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = new SolidColorBrush(Color.FromRgb(255, 167, 38)), // 亮橘色
                Stroke = new SolidColorBrush(Color.FromRgb(255, 87, 34)), // 深橘色邊框
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = Color.FromRgb(255, 167, 38), BlurRadius = 10, ShadowDepth = 0, Opacity = 0.8 },
                RenderTransform = rotRectItem.RotateScale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rotateDot, rotatePoint.X - 9);
            Canvas.SetTop(rotateDot, rotatePoint.Y - 9);
            canvas.Children.Add(rotateDot);
            Canvas.SetZIndex(rotateDot, rotRectItem.ZIndex);
            rotRectItem.RotateDot = rotateDot;

            // 如果顯示標籤，添加標籤
            if (showLabels)
            {
                AddRotRectLabels(rotRectItem, canvas);
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<RectRoiItem> rois)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Polygon != null && canvas.Children.Contains(roi.Polygon))
                    canvas.Children.Remove(roi.Polygon);
                for (int i = 0; i < 4; i++)
                {
                    if (roi.CornerDots[i] != null && canvas.Children.Contains(roi.CornerDots[i]))
                        canvas.Children.Remove(roi.CornerDots[i]);
                    roi.CornerDots[i] = null;
                }
                if (roi.RotateDot != null && canvas.Children.Contains(roi.RotateDot))
                    canvas.Children.Remove(roi.RotateDot);
                roi.RotateDot = null;
                if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                    canvas.Children.Remove(roi.LabelBorder);
                roi.LabelBorder = null;
                roi.Polygon = null;
            }
        }

        /// <summary>
        /// 繪製當前正在繪製的矩形ROI
        /// </summary>
        private void DrawCurrentRotRect(RectRoiItem rotRectItem, Canvas canvas, bool showLabels = true)
        {
            // 計算矩形的四個角落點
            Point[] corners = CalculateCorners(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);

            // 繪製矩形（不添加控制點）
            var polygon = new Polygon
            {
                Points = new PointCollection(corners),
                Stroke = new SolidColorBrush(styleDict["rotRect"].line),
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Opacity = 0.7,
                Effect = new DropShadowEffect { Color = styleDict["rotRect"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 },
                IsHitTestVisible = false
            };
            canvas.Children.Add(polygon);
            Canvas.SetZIndex(polygon, rotRectItem.ZIndex);
            rotRectItem.Polygon = polygon;

            // 如果顯示標籤，添加標籤
            if (showLabels)
            {
                AddRotRectLabels(rotRectItem, canvas);
            }
        }

        /// <summary>
        /// 更新矩形ROI視覺效果
        /// </summary>
        public void UpdateRotRectVisual(RectRoiItem rectRoiItem)
        {
            if (rectRoiItem.Polygon != null)
            {
                // 重新計算角落點
                Point[] corners = CalculateCorners(rectRoiItem.Center, rectRoiItem.Width, rectRoiItem.Height, rectRoiItem.Angle);
                rectRoiItem.Polygon.Points = new PointCollection(corners);
            }

            // 更新角落控制點位置
            Point[] cornerPoints = CalculateCorners(rectRoiItem.Center, rectRoiItem.Width, rectRoiItem.Height, rectRoiItem.Angle);
            for (int i = 0; i < 4; i++)
            {
                if (rectRoiItem.CornerDots[i] != null)
                {
                    Canvas.SetLeft(rectRoiItem.CornerDots[i], cornerPoints[i].X - 6);
                    Canvas.SetTop(rectRoiItem.CornerDots[i], cornerPoints[i].Y - 6);
                }
            }

            // 更新旋轉控制點位置
            if (rectRoiItem.RotateDot != null)
            {
                Point rotatePoint = CalculateRotatePoint(rectRoiItem.Center, rectRoiItem.Width, rectRoiItem.Height, rectRoiItem.Angle);
                Canvas.SetLeft(rectRoiItem.RotateDot, rotatePoint.X - 9);
                Canvas.SetTop(rectRoiItem.RotateDot, rotatePoint.Y - 9);
            }

            // 更新標籤位置和內容
            if (rectRoiItem.LabelBorder != null && mainCanvas != null)
            {
                // 更新標籤內容
                if (rectRoiItem.LabelBorder.Child is TextBlock labelText)
                {
                    labelText.Text = $"{rectRoiItem.Point1Text}\n{rectRoiItem.SizeText}\n{rectRoiItem.AngleText}";
                }
                // 更新標籤位置
                var (offsetX, offsetY) = GetLabelOffset(rectRoiItem.Center, mainCanvas.ActualWidth, mainCanvas.ActualHeight);
                Canvas.SetLeft(rectRoiItem.LabelBorder, rectRoiItem.Center.X + offsetX);
                Canvas.SetTop(rectRoiItem.LabelBorder, rectRoiItem.Center.Y + offsetY);
            }
        }

        /// <summary>
        /// 移除矩形ROI視覺元素
        /// </summary>
        public void RemoveRotRectVisual(RectRoiItem rectRoiItem, Canvas canvas)
        {
            if (rectRoiItem.Polygon != null)
            {
                canvas.Children.Remove(rectRoiItem.Polygon);
                rectRoiItem.Polygon = null;
            }

            for (int i = 0; i < 4; i++)
            {
                if (rectRoiItem.CornerDots[i] != null)
                {
                    canvas.Children.Remove(rectRoiItem.CornerDots[i]);
                    rectRoiItem.CornerDots[i] = null;
                }
            }

            if (rectRoiItem.RotateDot != null)
            {
                canvas.Children.Remove(rectRoiItem.RotateDot);
                rectRoiItem.RotateDot = null;
            }

            if (rectRoiItem.LabelBorder != null)
            {
                canvas.Children.Remove(rectRoiItem.LabelBorder);
                rectRoiItem.LabelBorder = null;
            }
        }

        /// <summary>
        /// 添加矩形ROI標籤
        /// </summary>
        public void AddRotRectLabels(RectRoiItem rectRoiItem, Canvas canvas)
        {
            // 創建標籤
            var labelText = new TextBlock
            {
                Text = $"{rectRoiItem.Point1Text}\n{rectRoiItem.SizeText}\n{rectRoiItem.AngleText}",
                Foreground = new SolidColorBrush(styleDict["rotRect"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };

            var labelBorder = new Border
            {
                Background = new SolidColorBrush(styleDict["rotRect"].labelBg),
                CornerRadius = new CornerRadius(6),
                Child = labelText,
                Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 4, ShadowDepth = 1, Opacity = 0.5 },
                Opacity = 0.95,
                Padding = new Thickness(4, 2, 4, 2),
                IsHitTestVisible = false
            };

            var (offsetX, offsetY) = GetLabelOffset(rectRoiItem.Center, canvas.ActualWidth, canvas.ActualHeight);
            Canvas.SetLeft(labelBorder, rectRoiItem.Center.X + offsetX);
            Canvas.SetTop(labelBorder, rectRoiItem.Center.Y + offsetY);
            canvas.Children.Add(labelBorder);
            Canvas.SetZIndex(labelBorder, rectRoiItem.ZIndex);
            rectRoiItem.LabelBorder = labelBorder;
        }

        /// <summary>
        /// 計算矩形的四個角落點
        /// </summary>
        private Point[] CalculateCorners(Point center, double width, double height, double angle)
        {
            double halfWidth = width / 2;
            double halfHeight = height / 2;
            double rad = angle * Math.PI / 180;

            Point[] corners = new Point[4];
            corners[0] = new Point(-halfWidth, -halfHeight); // 左上
            corners[1] = new Point(halfWidth, -halfHeight);  // 右上
            corners[2] = new Point(halfWidth, halfHeight);   // 右下
            corners[3] = new Point(-halfWidth, halfHeight);  // 左下

            // 旋轉和位移
            for (int i = 0; i < 4; i++)
            {
                double x = corners[i].X;
                double y = corners[i].Y;
                corners[i] = new Point(
                    x * Math.Cos(rad) - y * Math.Sin(rad) + center.X,
                    x * Math.Sin(rad) + y * Math.Cos(rad) + center.Y
                );
            }

            return corners;
        }

        /// <summary>
        /// 計算旋轉控制點位置
        /// </summary>
        private Point CalculateRotatePoint(Point center, double width, double height, double angle)
        {
            double rad = angle * Math.PI / 180.0;
            double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
            double hw = width / 2, hh = height / 2;
            // 上邊中點
            double xMid = center.X + hh * sinA;
            double yMid = center.Y - hh * cosA;
            double xDir = -sinA;
            double yDir = cosA;
            double rotDotDist = Math.Max(40, Math.Min(width, height) / 3); // 距離可依需求調整
            return new Point(xMid + rotDotDist * xDir, yMid + rotDotDist * yDir);
        }

        /// <summary>
        /// 獲取標籤偏移量
        /// </summary>
        private (double offsetX, double offsetY) GetLabelOffset(Point pt, double canvasWidth, double canvasHeight)
        {
            double offsetX = 25, offsetY = -25;
            if (pt.X > canvasWidth * 0.66 && pt.Y > canvasHeight * 0.66) { offsetX = -90; offsetY = -40; }
            else if (pt.X > canvasWidth * 0.66 && pt.Y < canvasHeight * 0.33) { offsetX = -90; offsetY = 25; }
            else if (pt.X < canvasWidth * 0.33 && pt.Y > canvasHeight * 0.66) { offsetX = 25; offsetY = -40; }
            else if (pt.X < canvasWidth * 0.33 && pt.Y < canvasHeight * 0.33) { offsetX = 25; offsetY = 25; }
            else if (pt.X > canvasWidth * 0.66) { offsetX = -90; }
            else if (pt.X < canvasWidth * 0.33) { offsetX = 25; }
            else if (pt.Y > canvasHeight * 0.66) { offsetY = -40; }
            else if (pt.Y < canvasHeight * 0.33) { offsetY = 25; }
            return (offsetX, offsetY);
        }

        /// <summary>
        /// 動畫縮放效果
        /// </summary>
        public void AnimateScale(ScaleTransform scaleTransform, double targetScale)
        {
            var scaleAnimation = new DoubleAnimation
            {
                To = targetScale,
                Duration = TimeSpan.FromMilliseconds(200),
                AutoReverse = true
            };
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }

        /// <summary>
        /// 開始動畫縮放
        /// </summary>
        public void StartAnimationScaling(IEnumerable<RectRoiItem> rotRectRois)
        {
            foreach (var rotRectRoi in rotRectRois)
            {
                if (rotRectRoi.CenterScale != null)
                {
                    rotRectRoi.CenterScale.ScaleX = 1.5;
                    rotRectRoi.CenterScale.ScaleY = 1.5;
                }
                for (int i = 0; i < 4; i++)
                {
                    if (rotRectRoi.CornerScales[i] != null)
                    {
                        rotRectRoi.CornerScales[i].ScaleX = 1.5;
                        rotRectRoi.CornerScales[i].ScaleY = 1.5;
                    }
                }
                if (rotRectRoi.RotateScale != null)
                {
                    rotRectRoi.RotateScale.ScaleX = 1.5;
                    rotRectRoi.RotateScale.ScaleY = 1.5;
                }
            }
        }

        /// <summary>
        /// 停止動畫縮放
        /// </summary>
        public void StopAnimationScaling(IEnumerable<RectRoiItem> rotRectRois)
        {
            foreach (var rotRectRoi in rotRectRois)
            {
                if (rotRectRoi.CenterScale != null)
                {
                    rotRectRoi.CenterScale.ScaleX = 1.0;
                    rotRectRoi.CenterScale.ScaleY = 1.0;
                }
                for (int i = 0; i < 4; i++)
                {
                    if (rotRectRoi.CornerScales[i] != null)
                    {
                        rotRectRoi.CornerScales[i].ScaleX = 1.0;
                        rotRectRoi.CornerScales[i].ScaleY = 1.0;
                    }
                }
                if (rotRectRoi.RotateScale != null)
                {
                    rotRectRoi.RotateScale.ScaleX = 1.0;
                    rotRectRoi.RotateScale.ScaleY = 1.0;
                }
            }
        }

        /// <summary>
        /// 設置主畫布引用
        /// </summary>
        public override void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        public override void DrawPreviewRoi(RectRoiItem rotRectItem, Canvas canvas, bool showLabels = true)
        {
            // 原本 DrawCurrentRotRect 的邏輯
            // 計算矩形的四個角落點
            Point[] corners = CalculateCorners(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);

            // 繪製矩形（不添加控制點）
            var polygon = new Polygon
            {
                Points = new PointCollection(corners),
                Stroke = new SolidColorBrush(styleDict["rotRect"].line),
                StrokeThickness = 3,
                Fill = Brushes.Transparent,
                Opacity = 0.7,
                Effect = new DropShadowEffect { Color = styleDict["rotRect"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 },
                IsHitTestVisible = false
            };
            canvas.Children.Add(polygon);
            Canvas.SetZIndex(polygon, rotRectItem.ZIndex);
            rotRectItem.Polygon = polygon;

            // 如果顯示標籤，添加標籤
            if (showLabels)
            {
                AddRotRectLabels(rotRectItem, canvas);
            }
        }
    }
} 