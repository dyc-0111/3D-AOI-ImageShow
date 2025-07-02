using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using HyImageShow.ImageShowWPF.Models;
using System.Collections.Generic;
using System.Linq;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 十字線繪製服務實作
    /// </summary>
    public class CrossLinesDrawingService : BaseRoiDrawingService<CrossLineItem>
    {
        private Ellipse overlayCenterDot;
        private Storyboard centerDotStoryboard;

        public override void DrawRois(Canvas canvas, List<CrossLineItem> rois, CrossLineItem currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois, currentRoi);
            foreach (var crossLine in rois)
            {
                DrawSingleRoi(crossLine, canvas, showLabels);
            }
            if (currentRoi != null && !rois.Contains(currentRoi))
            {
                DrawSingleRoi(currentRoi, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(CrossLineItem crossLine, Canvas canvas, bool showLabels = true)
        {
            if (crossLine == null) return;
            // 繪製十字線（可根據 crossLine.Center, crossLine.Length, crossLine.Angle, crossLine.LineColor, crossLine.LineThickness）
            // 這裡可複製你原本的 DrawCrossLines 邏輯，並根據 crossLine 屬性繪製
        }

        public override void ClearRois(Canvas canvas, IEnumerable<CrossLineItem> rois, CrossLineItem currentRoi = null)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.HorizontalLine != null && canvas.Children.Contains(roi.HorizontalLine))
                    canvas.Children.Remove(roi.HorizontalLine);
                if (roi.VerticalLine != null && canvas.Children.Contains(roi.VerticalLine))
                    canvas.Children.Remove(roi.VerticalLine);
                roi.HorizontalLine = null;
                roi.VerticalLine = null;
            }
            if (currentRoi != null)
            {
                if (currentRoi.HorizontalLine != null && canvas.Children.Contains(currentRoi.HorizontalLine))
                    canvas.Children.Remove(currentRoi.HorizontalLine);
                if (currentRoi.VerticalLine != null && canvas.Children.Contains(currentRoi.VerticalLine))
                    canvas.Children.Remove(currentRoi.VerticalLine);
                currentRoi.HorizontalLine = null;
                currentRoi.VerticalLine = null;
            }
        }

        public void DrawCrossLines(Canvas canvas, double width, double height)
        {
            ClearCrossLines(canvas);

            if (width <= 0 || height <= 0) return;

            double gap = 18; // 缺口長度
            double centerX = width / 2;
            double centerY = height / 2;

            // 水平線（左）
            var hLineLeft = new Line
            {
                X1 = 0,
                Y1 = centerY,
                X2 = centerX - gap,
                Y2 = centerY,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };

            // 水平線（右）
            var hLineRight = new Line
            {
                X1 = centerX + gap,
                Y1 = centerY,
                X2 = width,
                Y2 = centerY,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };

            // 垂直線（上）
            var vLineTop = new Line
            {
                X1 = centerX,
                Y1 = 0,
                X2 = centerX,
                Y2 = centerY - gap,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };

            // 垂直線（下）
            var vLineBottom = new Line
            {
                X1 = centerX,
                Y1 = centerY + gap,
                X2 = centerX,
                Y2 = height,
                Stroke = Brushes.Red,
                StrokeThickness = 1.5,
                Opacity = 0.6,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 }
            };

            // 中心圓點
            overlayCenterDot = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = Brushes.White,
                Stroke = Brushes.Red,
                StrokeThickness = 2.5,
                Opacity = 0.9,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 12, ShadowDepth = 0, Opacity = 0.8 }
            };
            Canvas.SetLeft(overlayCenterDot, centerX - 8);
            Canvas.SetTop(overlayCenterDot, centerY - 8);

            // 添加所有元素到Canvas
            canvas.Children.Add(hLineLeft);
            canvas.Children.Add(hLineRight);
            canvas.Children.Add(vLineTop);
            canvas.Children.Add(vLineBottom);
            canvas.Children.Add(overlayCenterDot);

            // 呼吸動畫
            DoubleAnimation breathAnim = new DoubleAnimation
            {
                From = 0.7,
                To = 1.0,
                Duration = TimeSpan.FromSeconds(0.7),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            centerDotStoryboard = new Storyboard();
            centerDotStoryboard.Children.Add(breathAnim);
            Storyboard.SetTarget(breathAnim, overlayCenterDot);
            Storyboard.SetTargetProperty(breathAnim, new PropertyPath(UIElement.OpacityProperty));
            centerDotStoryboard.Begin();
        }

        public void ClearCrossLines(Canvas canvas)
        {
            if (centerDotStoryboard != null)
            {
                centerDotStoryboard.Stop();
                centerDotStoryboard = null;
            }

            // 清除所有十字線相關的元素
            var elementsToRemove = new System.Collections.Generic.List<UIElement>();
            foreach (UIElement element in canvas.Children)
            {
                if (element is Line || element == overlayCenterDot)
                {
                    elementsToRemove.Add(element);
                }
            }

            foreach (var element in elementsToRemove)
            {
                canvas.Children.Remove(element);
            }

            overlayCenterDot = null;
        }

        public void UpdateCrossLines(Canvas canvas, double width, double height)
        {
            DrawCrossLines(canvas, width, height);
        }
    }
} 