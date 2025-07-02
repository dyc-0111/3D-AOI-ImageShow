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
    /// 圓形ROI繪製服務實現
    /// </summary>
    public class EllipseRoiDrawingService : BaseRoiDrawingService<EllipseRoiItem>
    {
        private Canvas mainCanvas;
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;

        public EllipseRoiDrawingService()
        {
            styleDict = new Dictionary<string, (Color, Color, Color, Color)>
            {
                {"ellipse", (Color.FromRgb(140, 90, 220), Color.FromRgb(106, 58, 177), Color.FromArgb(220, 210, 180, 255), Color.FromRgb(106, 58, 177))}, // 紫
            };
        }

        public override void DrawRois(Canvas canvas, List<EllipseRoiItem> rois, EllipseRoiItem currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois);
            foreach (var ellipseRoi in rois)
            {
                DrawSingleRoi(ellipseRoi, canvas, showLabels);
            }
            if (currentRoi != null && !rois.Contains(currentRoi))
            {
                DrawPreviewRoi(currentRoi, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(EllipseRoiItem ellipseRoi, Canvas canvas, bool showLabels = true)
        {
            if (ellipseRoi == null || ellipseRoi.Radius <= 0) return;

            // 清除該圓形ROI的所有舊視覺元素（安全移除）
            if (ellipseRoi.Ellipse != null)
            {
                if (canvas.Children.Contains(ellipseRoi.Ellipse))
                    canvas.Children.Remove(ellipseRoi.Ellipse);
                ellipseRoi.Ellipse = null;
            }
            if (ellipseRoi.CenterDot != null)
            {
                if (canvas.Children.Contains(ellipseRoi.CenterDot))
                    canvas.Children.Remove(ellipseRoi.CenterDot);
                ellipseRoi.CenterDot = null;
            }
            if (ellipseRoi.RadiusDot != null)
            {
                if (canvas.Children.Contains(ellipseRoi.RadiusDot))
                    canvas.Children.Remove(ellipseRoi.RadiusDot);
                ellipseRoi.RadiusDot = null;
            }
            if (ellipseRoi.LabelBorder != null)
            {
                if (canvas.Children.Contains(ellipseRoi.LabelBorder))
                    canvas.Children.Remove(ellipseRoi.LabelBorder);
                ellipseRoi.LabelBorder = null;
            }

            // 繪製圓形
            var ellipse = new Ellipse
            {
                Width = ellipseRoi.Radius * 2,
                Height = ellipseRoi.Radius * 2,
                Stroke = new SolidColorBrush(styleDict["ellipse"].line),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(60, 156, 39, 176)),
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["ellipse"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            Canvas.SetLeft(ellipse, ellipseRoi.Center.X - ellipseRoi.Radius);
            Canvas.SetTop(ellipse, ellipseRoi.Center.Y - ellipseRoi.Radius);
            canvas.Children.Add(ellipse);
            Canvas.SetZIndex(ellipse, ellipseRoi.ZIndex);
            ellipseRoi.Ellipse = ellipse;

            // 繪製中心點
            var centerDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ellipse"].line),
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["ellipse"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = ellipseRoi.CenterScale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(centerDot, ellipseRoi.Center.X - 6);
            Canvas.SetTop(centerDot, ellipseRoi.Center.Y - 6);
            canvas.Children.Add(centerDot);
            Canvas.SetZIndex(centerDot, ellipseRoi.ZIndex);
            ellipseRoi.CenterDot = centerDot;

            var radiusDot = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.Yellow,
                Stroke = new SolidColorBrush(styleDict["ellipse"].line), 
                StrokeThickness = 4,
                Effect = new DropShadowEffect { Color = Color.FromRgb(255, 60, 0), BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = ellipseRoi.RadiusScale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false,
            };
            Point radiusPoint = (Point)(ellipseRoi.Center + new Vector(ellipseRoi.Radius, 0));
            Canvas.SetLeft(radiusDot, radiusPoint.X - 8);
            Canvas.SetTop(radiusDot, radiusPoint.Y - 8);
            canvas.Children.Add(radiusDot);
            Canvas.SetZIndex(radiusDot, ellipseRoi.ZIndex);
            ellipseRoi.RadiusDot = radiusDot;

            if (showLabels)
            {
                AddEllipseLabels(ellipseRoi, canvas);
            }
        }

        public override void DrawPreviewRoi(EllipseRoiItem ellipseRoi, Canvas canvas, bool showLabels = true)
        {
            // 原本 DrawCurrentEllipseRoi 的邏輯
            // ...（可直接複製 DrawCurrentEllipseRoi 內容）...
        }

        public override void ClearRois(Canvas canvas, IEnumerable<EllipseRoiItem> rois)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Ellipse != null && canvas.Children.Contains(roi.Ellipse))
                    canvas.Children.Remove(roi.Ellipse);
                if (roi.CenterDot != null && canvas.Children.Contains(roi.CenterDot))
                    canvas.Children.Remove(roi.CenterDot);
                if (roi.RadiusDot != null && canvas.Children.Contains(roi.RadiusDot))
                    canvas.Children.Remove(roi.RadiusDot);
                if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                    canvas.Children.Remove(roi.LabelBorder);
                roi.Ellipse = null;
                roi.CenterDot = null;
                roi.RadiusDot = null;
                roi.LabelBorder = null;
            }
        }

        /// <summary>
        /// 更新視覺元素
        /// </summary>
        public void UpdateVisualElements(Canvas canvas, List<EllipseRoiItem> ellipseRois, EllipseRoiItem currentEllipseRoi)
        {
            // 重新繪製所有圓形ROI
            DrawRois(canvas, ellipseRois, currentEllipseRoi);
        }

        /// <summary>
        /// 更新橢圓ROI視覺效果
        /// </summary>
        public void UpdateEllipseVisual(EllipseRoiItem ellipseRoiItem)
        {
            if (ellipseRoiItem == null) return;

            // 更新橢圓位置和大小
            if (ellipseRoiItem.Ellipse != null)
            {
                ellipseRoiItem.Ellipse.Width = ellipseRoiItem.Radius * 2;
                ellipseRoiItem.Ellipse.Height = ellipseRoiItem.Radius * 2;
                Canvas.SetLeft(ellipseRoiItem.Ellipse, ellipseRoiItem.Center.X - ellipseRoiItem.Radius);
                Canvas.SetTop(ellipseRoiItem.Ellipse, ellipseRoiItem.Center.Y - ellipseRoiItem.Radius);
            }

            // 更新中心點位置
            if (ellipseRoiItem.CenterDot != null)
            {
                Canvas.SetLeft(ellipseRoiItem.CenterDot, ellipseRoiItem.Center.X - 6);
                Canvas.SetTop(ellipseRoiItem.CenterDot, ellipseRoiItem.Center.Y - 6);
            }

            // 更新半徑點位置
            if (ellipseRoiItem.RadiusDot != null)
            {
                Point radiusPoint = (Point)(ellipseRoiItem.Center + new Vector(ellipseRoiItem.Radius, 0));
                Canvas.SetLeft(ellipseRoiItem.RadiusDot, radiusPoint.X - 10);
                Canvas.SetTop(ellipseRoiItem.RadiusDot, radiusPoint.Y - 10);
            }

            // 更新標籤位置和內容
            if (ellipseRoiItem.LabelBorder != null && mainCanvas != null)
            {
                // 更新標籤內容
                if (ellipseRoiItem.LabelBorder.Child is TextBlock labelText)
                {
                    labelText.Text = $"{ellipseRoiItem.Point1Text}\n{ellipseRoiItem.Point2Text}";
                }
                
                // 更新標籤位置
                var (offsetX, offsetY) = GetLabelOffset(ellipseRoiItem.Center, mainCanvas.ActualWidth, mainCanvas.ActualHeight);
                Canvas.SetLeft(ellipseRoiItem.LabelBorder, ellipseRoiItem.Center.X + offsetX);
                Canvas.SetTop(ellipseRoiItem.LabelBorder, ellipseRoiItem.Center.Y + offsetY);
            }
        }

        /// <summary>
        /// 添加標籤
        /// </summary>
        public void AddLabels(Canvas canvas, List<EllipseRoiItem> ellipseRois)
        {
            foreach (var ellipseRoi in ellipseRois)
            {
                if (ellipseRoi.IsCompleted)
                {
                    AddEllipseLabels(ellipseRoi, canvas);
                }
            }
        }

        /// <summary>
        /// 開始動畫縮放
        /// </summary>
        public void StartAnimationScaling(List<EllipseRoiItem> ellipseRois)
        {
            foreach (var ellipseRoi in ellipseRois)
            {
                if (ellipseRoi.CenterScale != null)
                {
                    ellipseRoi.CenterScale.ScaleX = 1.5;
                    ellipseRoi.CenterScale.ScaleY = 1.5;
                }
                if (ellipseRoi.RadiusScale != null)
                {
                    ellipseRoi.RadiusScale.ScaleX = 1.5;
                    ellipseRoi.RadiusScale.ScaleY = 1.5;
                }
            }
        }

        /// <summary>
        /// 停止動畫縮放
        /// </summary>
        public void StopAnimationScaling(List<EllipseRoiItem> ellipseRois)
        {
            foreach (var ellipseRoi in ellipseRois)
            {
                if (ellipseRoi.CenterScale != null)
                {
                    ellipseRoi.CenterScale.ScaleX = 1.0;
                    ellipseRoi.CenterScale.ScaleY = 1.0;
                }
                if (ellipseRoi.RadiusScale != null)
                {
                    ellipseRoi.RadiusScale.ScaleX = 1.0;
                    ellipseRoi.RadiusScale.ScaleY = 1.0;
                }
            }
        }

        /// <summary>
        /// 添加圓形ROI標籤
        /// </summary>
        private void AddEllipseLabels(EllipseRoiItem ellipseRoi, Canvas canvas)
        {
            var labelText = new TextBlock
            {
                Text = $"{ellipseRoi.Point1Text}\n{ellipseRoi.Point2Text}",
                Foreground = new SolidColorBrush(styleDict["ellipse"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Background = new SolidColorBrush(styleDict["ellipse"].labelBg),
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

            // 計算標籤位置
            var (offsetX, offsetY) = GetLabelOffset(ellipseRoi.Center, canvas.ActualWidth, canvas.ActualHeight);
            Canvas.SetLeft(labelBorder, ellipseRoi.Center.X + offsetX);
            Canvas.SetTop(labelBorder, ellipseRoi.Center.Y + offsetY);
            canvas.Children.Add(labelBorder);
            Canvas.SetZIndex(labelBorder, ellipseRoi.ZIndex);
            ellipseRoi.LabelBorder = labelBorder;
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

        // Helper: 判斷 currentEllipseRoi 是否已在 ellipseRois 裡
        private bool ellipseRoiListContains(List<EllipseRoiItem> list, EllipseRoiItem item)
        {
            return list != null && item != null && list.Contains(item);
        }
        public override void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }
    }
}