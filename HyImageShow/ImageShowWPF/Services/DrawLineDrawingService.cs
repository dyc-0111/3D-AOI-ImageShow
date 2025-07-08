using HyImageShow.ImageShowWPF.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 畫線繪製服務實現
    /// </summary>
    public class DrawLineDrawingService : BaseRoiDrawingService<LineItem>
    {
        private Canvas mainCanvas;
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;

        public DrawLineDrawingService()
        {
            styleDict = new Dictionary<string, (Color, Color, Color, Color)>
            {
                {"line", (Color.FromRgb(0, 188, 212), Color.FromRgb(0, 150, 167), Color.FromArgb(220, 178, 235, 242), Color.FromRgb(0, 105, 120))}, // 青
            };
        }

        public void DrawLines(Canvas canvas, ObservableCollection<LineItem> lines, LineItem currentLine, bool showLabels = true)
        {
            // 清除所有現有的線條繪製
            ClearLines(canvas, lines, currentLine);

            // 繪製所有已完成的線條
            foreach (var line in lines)
            {
                if (line.IsCompleted)
                {
                    DrawSingleLine(line, canvas, showLabels);
                }
            }

            // 繪製當前正在繪製的線條
            if (currentLine != null && !currentLine.IsCompleted)
            {
                DrawCurrentLine(currentLine, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(LineItem roi, Canvas canvas, bool showLabels = true)
        {
            if (roi == null) return;

            // 清除該線條的所有舊視覺元素（安全移除）
            RemoveLineVisual(roi, canvas);

            // 繪製線條
            var line = new Line
            {
                X1 = roi.P1.X,
                Y1 = roi.P1.Y,
                X2 = roi.P2.X,
                Y2 = roi.P2.Y,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            canvas.Children.Add(line);
            Canvas.SetZIndex(line, roi.ZIndex);
            roi.Line = line;

            // 繪製起點
            var startDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = roi.Point1Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(startDot, roi.P1.X - 6);
            Canvas.SetTop(startDot, roi.P1.Y - 6);
            canvas.Children.Add(startDot);
            Canvas.SetZIndex(startDot, roi.ZIndex);
            roi.Point1 = startDot;

            // 繪製終點
            var endDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = roi.Point2Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(endDot, roi.P2.X - 6);
            Canvas.SetTop(endDot, roi.P2.Y - 6);
            canvas.Children.Add(endDot);
            Canvas.SetZIndex(endDot, roi.ZIndex);
            roi.Point2 = endDot;

            // 繪製標籤
            if (showLabels)
            {
                AddLineLabels(roi, canvas);
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<LineItem> rois, LineItem currentRoi = null)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Line != null && canvas.Children.Contains(roi.Line))
                    canvas.Children.Remove(roi.Line);
                if (roi.Point1 != null && canvas.Children.Contains(roi.Point1))
                    canvas.Children.Remove(roi.Point1);
                if (roi.Point2 != null && canvas.Children.Contains(roi.Point2))
                    canvas.Children.Remove(roi.Point2);
                if (roi.Label != null && canvas.Children.Contains(roi.Label))
                    canvas.Children.Remove(roi.Label);
                roi.Line = null;
                roi.Point1 = null;
                roi.Point2 = null;
                roi.Label = null;
            }
            if (currentRoi != null)
            {
                if (currentRoi.Line != null && canvas.Children.Contains(currentRoi.Line))
                    canvas.Children.Remove(currentRoi.Line);
                if (currentRoi.Point1 != null && canvas.Children.Contains(currentRoi.Point1))
                    canvas.Children.Remove(currentRoi.Point1);
                if (currentRoi.Point2 != null && canvas.Children.Contains(currentRoi.Point2))
                    canvas.Children.Remove(currentRoi.Point2);
                if (currentRoi.Label != null && canvas.Children.Contains(currentRoi.Label))
                    canvas.Children.Remove(currentRoi.Label);
                currentRoi.Line = null;
                currentRoi.Point1 = null;
                currentRoi.Point2 = null;
                currentRoi.Label = null;
            }
        }

        public override void ClearRois(Canvas canvas, IEnumerable<LineItem> rois)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Line != null && canvas.Children.Contains(roi.Line))
                    canvas.Children.Remove(roi.Line);
                if (roi.Point1 != null && canvas.Children.Contains(roi.Point1))
                    canvas.Children.Remove(roi.Point1);
                if (roi.Point2 != null && canvas.Children.Contains(roi.Point2))
                    canvas.Children.Remove(roi.Point2);
                if (roi.Label != null && canvas.Children.Contains(roi.Label))
                    canvas.Children.Remove(roi.Label);
                roi.Line = null;
                roi.Point1 = null;
                roi.Point2 = null;
                roi.Label = null;
            }
        }

        public void DrawSingleLine(LineItem lineItem, Canvas canvas, bool showLabels = true)
        {
            if (lineItem == null) return;

            // 清除該線條的所有舊視覺元素（安全移除）
            RemoveLineVisual(lineItem, canvas);

            // 繪製線條
            var line = new Line
            {
                X1 = lineItem.P1.X,
                Y1 = lineItem.P1.Y,
                X2 = lineItem.P2.X,
                Y2 = lineItem.P2.Y,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            canvas.Children.Add(line);
            Canvas.SetZIndex(line, lineItem.ZIndex);
            lineItem.Line = line;

            // 繪製起點
            var startDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = lineItem.Point1Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(startDot, lineItem.P1.X - 6);
            Canvas.SetTop(startDot, lineItem.P1.Y - 6);
            canvas.Children.Add(startDot);
            Canvas.SetZIndex(startDot, lineItem.ZIndex);
            lineItem.Point1 = startDot;

            // 繪製終點（與起點顏色一致）
            var endDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = lineItem.Point2Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(endDot, lineItem.P2.X - 6);
            Canvas.SetTop(endDot, lineItem.P2.Y - 6);
            canvas.Children.Add(endDot);
            Canvas.SetZIndex(endDot, lineItem.ZIndex);
            lineItem.Point2 = endDot;

            // 繪製標籤
            if (showLabels)
            {
                AddLineLabels(lineItem, canvas);
            }
        }

        public void RemoveCurrentLine(Canvas canvas)
        {
            // 移除當前正在繪製的線條
            var currentElements = canvas.Children.OfType<UIElement>().Where(e =>
                e is Line || e is Ellipse || e is Border).ToList();

            foreach (var element in currentElements)
            {
                canvas.Children.Remove(element);
            }
        }

        public void ClearLines(Canvas canvas, IEnumerable<LineItem> lineItems, LineItem currentDrawLine = null)
        {
            if (canvas == null) return;
            if (lineItems != null)
            {
                foreach (var lineItem in lineItems)
                {
                    RemoveLineVisual(lineItem, canvas);
                }
            }
            if (currentDrawLine != null)
            {
                RemoveLineVisual(currentDrawLine, canvas);
            }
        }

        public void UpdateLineVisual(LineItem lineItem)
        {
            if (lineItem.Line != null)
            {
                lineItem.Line.X1 = lineItem.P1.X;
                lineItem.Line.Y1 = lineItem.P1.Y;
                lineItem.Line.X2 = lineItem.P2.X;
                lineItem.Line.Y2 = lineItem.P2.Y;
            }

            if (lineItem.Point1 != null)
            {
                Canvas.SetLeft(lineItem.Point1, lineItem.P1.X - 6);
                Canvas.SetTop(lineItem.Point1, lineItem.P1.Y - 6);
            }

            if (lineItem.Point2 != null)
            {
                Canvas.SetLeft(lineItem.Point2, lineItem.P2.X - 6);
                Canvas.SetTop(lineItem.Point2, lineItem.P2.Y - 6);
            }

            // 更新 label 位置與內容
            if (lineItem.Label != null)
            {
                // 更新 Label 的文字內容
                if (lineItem.Label.Child is TextBlock textBlock)
                {
                    textBlock.Text = $"{lineItem.Point1Text}\n{lineItem.Point2Text}";
                }
                // 更新 Label 的位置
                var (offsetX, offsetY) = GetLabelOffset(lineItem.MidPoint, mainCanvas?.ActualWidth ?? 800, mainCanvas?.ActualHeight ?? 600);
                Canvas.SetLeft(lineItem.Label, lineItem.MidPoint.X + offsetX);
                Canvas.SetTop(lineItem.Label, lineItem.MidPoint.Y + offsetY);
            }
        }

        public void AddLineLabels(LineItem lineItem, Canvas canvas)
        {
            // 先移除舊的 label
            if (lineItem.Label != null && canvas.Children.Contains(lineItem.Label))
                canvas.Children.Remove(lineItem.Label);
            lineItem.Label = null;
            if (lineItem.Point2Label != null && canvas.Children.Contains(lineItem.Point2Label))
                canvas.Children.Remove(lineItem.Point2Label);
            lineItem.Point2Label = null;

            // 使用 LineItem 的 Text 屬性
            string labelTextStr = $"{lineItem.Point1Text}\n{lineItem.Point2Text}";
            var labelText = new TextBlock
            {
                Text = labelTextStr,
                Foreground = new SolidColorBrush(styleDict["line"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Background = new SolidColorBrush(styleDict["line"].labelBg),
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
            var (offsetX, offsetY) = GetLabelOffset(lineItem.MidPoint, canvas.ActualWidth, canvas.ActualHeight);
            Canvas.SetLeft(labelBorder, lineItem.MidPoint.X + offsetX);
            Canvas.SetTop(labelBorder, lineItem.MidPoint.Y + offsetY);
            canvas.Children.Add(labelBorder);
            Canvas.SetZIndex(labelBorder, lineItem.ZIndex);
            lineItem.Label = labelBorder;
        }

        public void StartAnimationScaling(ObservableCollection<LineItem> lines)
        {
            foreach (var line in lines)
            {
                if (line.Point1Scale != null)
                {
                    line.Point1Scale.ScaleX = 1.5;
                    line.Point1Scale.ScaleY = 1.5;
                }
                if (line.Point2Scale != null)
                {
                    line.Point2Scale.ScaleX = 1.5;
                    line.Point2Scale.ScaleY = 1.5;
                }
            }
        }

        public void StopAnimationScaling(ObservableCollection<LineItem> lines)
        {
            foreach (var line in lines)
            {
                if (line.Point1Scale != null)
                {
                    line.Point1Scale.ScaleX = 1.0;
                    line.Point1Scale.ScaleY = 1.0;
                }
                if (line.Point2Scale != null)
                {
                    line.Point2Scale.ScaleX = 1.0;
                    line.Point2Scale.ScaleY = 1.0;
                }
            }
        }

        public override void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        /// <summary>
        /// 繪製當前正在繪製的線條
        /// </summary>
        private void DrawCurrentLine(LineItem lineItem, Canvas canvas, bool showLabels = true)
        {
            if (lineItem == null) return;

            // 清除舊的繪製元素（安全移除）
            RemoveLineVisual(lineItem, canvas);

            // 繪製線條
            var line = new Line
            {
                X1 = lineItem.P1.X,
                Y1 = lineItem.P1.Y,
                X2 = lineItem.P2.X,
                Y2 = lineItem.P2.Y,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            canvas.Children.Add(line);
            Canvas.SetZIndex(line, lineItem.ZIndex);
            lineItem.Line = line;

            // 繪製起點
            var startDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = lineItem.Point1Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(startDot, lineItem.P1.X - 6);
            Canvas.SetTop(startDot, lineItem.P1.Y - 6);
            canvas.Children.Add(startDot);
            Canvas.SetZIndex(startDot, lineItem.ZIndex);
            lineItem.Point1 = startDot;

            // 繪製終點（與起點顏色一致）
            var endDot = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["line"].line),
                StrokeThickness = 2,
                Effect = new DropShadowEffect { Color = styleDict["line"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = lineItem.Point2Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(endDot, lineItem.P2.X - 6);
            Canvas.SetTop(endDot, lineItem.P2.Y - 6);
            canvas.Children.Add(endDot);
            Canvas.SetZIndex(endDot, lineItem.ZIndex);
            lineItem.Point2 = endDot;

            // 繪製標籤
            if (showLabels)
            {
                AddLineLabels(lineItem, canvas);
            }
        }

        /// <summary>
        /// 移除線條視覺元素
        /// </summary>
        private void RemoveLineVisual(LineItem lineItem, Canvas canvas)
        {
            if (lineItem.Line != null)
            {
                if (canvas.Children.Contains(lineItem.Line))
                    canvas.Children.Remove(lineItem.Line);
                lineItem.Line = null;
            }
            if (lineItem.Point1 != null)
            {
                if (canvas.Children.Contains(lineItem.Point1))
                    canvas.Children.Remove(lineItem.Point1);
                lineItem.Point1 = null;
            }
            if (lineItem.Point2 != null)
            {
                if (canvas.Children.Contains(lineItem.Point2))
                    canvas.Children.Remove(lineItem.Point2);
                lineItem.Point2 = null;
            }
            if (lineItem.Label != null)
            {
                if (canvas.Children.Contains(lineItem.Label))
                    canvas.Children.Remove(lineItem.Label);
                lineItem.Label = null;
            }
            if (lineItem.Point2Label != null)
            {
                if (canvas.Children.Contains(lineItem.Point2Label))
                    canvas.Children.Remove(lineItem.Point2Label);
                lineItem.Point2Label = null;
            }
        }

        /// <summary>
        /// 計算標籤偏移量
        /// </summary>
        private (double offsetX, double offsetY) GetLabelOffset(Point pt, double canvasWidth, double canvasHeight)
        {
            double offsetX = 20;
            double offsetY = -20;

            // 檢查右邊界
            if (pt.X + offsetX + 100 > canvasWidth)
            {
                offsetX = -120;
            }

            // 檢查下邊界
            if (pt.Y + offsetY + 60 > canvasHeight)
            {
                offsetY = 20;
            }

            return (offsetX, offsetY);
        }
    }
} 