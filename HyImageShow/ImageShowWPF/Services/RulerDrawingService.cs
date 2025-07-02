using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using HyImageShow.ImageShowWPF.Models;

namespace HyImageShow.ImageShowWPF.Services
{
    public class RulerDrawingService : BaseRoiDrawingService<RulerItem>
    {
        private readonly Dictionary<int, List<UIElement>> rulerElements = new Dictionary<int, List<UIElement>>();
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;
        private Canvas mainCanvas;

        public RulerDrawingService()
        {
            styleDict = new Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)>
            {
                {"ruler", (Color.FromRgb(255, 82, 82), Color.FromRgb(211, 47, 47), Color.FromArgb(220, 255, 205, 210), Color.FromRgb(183, 28, 28))}
            };
        }

        public override void DrawRois(Canvas canvas, List<RulerItem> rois, RulerItem currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois);
            foreach (var ruler in rois)
            {
                DrawSingleRoi(ruler, canvas, showLabels);
            }
            if (currentRoi != null && !rois.Contains(currentRoi))
            {
                DrawSingleRoi(currentRoi, canvas, showLabels);
            }
        }

        public override void DrawSingleRoi(RulerItem ruler, Canvas canvas, bool showLabels = true)
        {
            RemoveRuler(canvas, ruler);

            var elements = new List<UIElement>();

            // 繪製線條
            var line = new Line
            {
                X1 = ruler.Point1.X,
                Y1 = ruler.Point1.Y,
                X2 = ruler.Point2.X,
                Y2 = ruler.Point2.Y,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.7,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            canvas.Children.Add(line);
            Canvas.SetZIndex(line, ruler.ZIndex);
            elements.Add(line);
            ruler.Line = line; // 保存引用

            // 繪製端點1
            var point1 = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = ruler.Point1Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(point1, ruler.Point1.X - 9);
            Canvas.SetTop(point1, ruler.Point1.Y - 9);
            canvas.Children.Add(point1);
            Canvas.SetZIndex(point1, ruler.ZIndex);
            elements.Add(point1);
            ruler.Point1Dot = point1; // 保存引用

            // 繪製端點2
            var point2 = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                RenderTransform = ruler.Point2Scale,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(point2, ruler.Point2.X - 9);
            Canvas.SetTop(point2, ruler.Point2.Y - 9);
            canvas.Children.Add(point2);
            Canvas.SetZIndex(point2, ruler.ZIndex);
            elements.Add(point2);
            ruler.Point2Dot = point2; // 保存引用

            // 更新縮放動畫
            double scale1 = ruler.IsDraggingPoint1 ? 1.4 : 1.0;
            double scale2 = ruler.IsDraggingPoint2 ? 1.4 : 1.0;
            var anim1 = new DoubleAnimation(scale1, TimeSpan.FromMilliseconds(120));
            var anim2 = new DoubleAnimation(scale2, TimeSpan.FromMilliseconds(120));
            ruler.Point1Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim1);
            ruler.Point1Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim1);
            ruler.Point2Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim2);
            ruler.Point2Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim2);

            // 繪製箭頭
            double dx = ruler.Point2.X - ruler.Point1.X;
            double dy = ruler.Point2.Y - ruler.Point1.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            
            if (dist > 30)
            {
                double angle = Math.Atan2(dy, dx);
                double arrowLen = 36;
                double arrowWidth = 18;
                
                Point tip = new Point(ruler.Point2.X, ruler.Point2.Y);
                Point base1 = new Point(
                    tip.X - arrowLen * Math.Cos(angle) + arrowWidth * Math.Sin(angle) / 2,
                    tip.Y - arrowLen * Math.Sin(angle) - arrowWidth * Math.Cos(angle) / 2);
                Point base2 = new Point(
                    tip.X - arrowLen * Math.Cos(angle) - arrowWidth * Math.Sin(angle) / 2,
                    tip.Y - arrowLen * Math.Sin(angle) + arrowWidth * Math.Cos(angle) / 2);
                
                var arrow = new Polygon
                {
                    Points = new PointCollection { base1, tip, base2 },
                    Fill = new SolidColorBrush(Color.FromRgb(255, 60, 60)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2.5,
                    Opacity = 0.6,
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
                };
                canvas.Children.Add(arrow);
                Canvas.SetZIndex(arrow, ruler.ZIndex);
                elements.Add(arrow);
                ruler.Arrow = arrow; // 保存引用
            }

            // 繪製標籤
            if (showLabels)
            {
                double midX = (ruler.Point1.X + ruler.Point2.X) / 2;
                double midY = (ruler.Point1.Y + ruler.Point2.Y) / 2;

                // 主要標籤
                var label = new TextBlock
                {
                    Text = ruler.DistanceText,
                    Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 18,
                    Background = Brushes.Transparent,
                    Opacity = 1.0,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var labelBorder = new Border
                {
                    Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                    CornerRadius = new CornerRadius(12),
                    Child = label,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(10, 4, 10, 4)
                };
                Canvas.SetLeft(labelBorder, midX - 80);
                Canvas.SetTop(labelBorder, midY - 32);
                canvas.Children.Add(labelBorder);
                Canvas.SetZIndex(labelBorder, ruler.ZIndex);
                elements.Add(labelBorder);
                ruler.LabelBorder = labelBorder; // 保存引用

                // 端點標籤
                var p1Text = new TextBlock
                {
                    Text = ruler.Point1Text,
                    Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var p1Border = new Border
                {
                    Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = p1Text,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(p1Border, ruler.Point1.X + 12);
                Canvas.SetTop(p1Border, ruler.Point1.Y - 8);
                canvas.Children.Add(p1Border);
                Canvas.SetZIndex(p1Border, ruler.ZIndex);
                elements.Add(p1Border);
                ruler.Point1Label = p1Border; // 保存引用

                var p2Text = new TextBlock
                {
                    Text = ruler.Point2Text,
                    Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    FontFamily = new FontFamily("Segoe UI"),
                    Background = Brushes.Transparent,
                    Padding = new Thickness(0),
                    TextAlignment = TextAlignment.Center
                };
                var p2Border = new Border
                {
                    Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                    CornerRadius = new CornerRadius(8),
                    Child = p2Text,
                    Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                    Opacity = 0.98,
                    Padding = new Thickness(6, 2, 6, 2),
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(p2Border, ruler.Point2.X + 12);
                Canvas.SetTop(p2Border, ruler.Point2.Y - 8);
                canvas.Children.Add(p2Border);
                Canvas.SetZIndex(p2Border, ruler.ZIndex);
                elements.Add(p2Border);
                ruler.Point2Label = p2Border; // 保存引用
            }

            // 儲存元素引用
            rulerElements[ruler.Id] = elements;
        }

        public override void ClearRois(Canvas canvas, IEnumerable<RulerItem> rois)
        {
            if (canvas == null) return;
            foreach (var roi in rois.ToList())
            {
                if (roi.Line != null && canvas.Children.Contains(roi.Line))
                    canvas.Children.Remove(roi.Line);
                if (roi.Point1Dot != null && canvas.Children.Contains(roi.Point1Dot))
                    canvas.Children.Remove(roi.Point1Dot);
                if (roi.Point2Dot != null && canvas.Children.Contains(roi.Point2Dot))
                    canvas.Children.Remove(roi.Point2Dot);
                if (roi.LabelBorder != null && canvas.Children.Contains(roi.LabelBorder))
                    canvas.Children.Remove(roi.LabelBorder);
                if (roi.Point1Label != null && canvas.Children.Contains(roi.Point1Label))
                    canvas.Children.Remove(roi.Point1Label);
                if (roi.Point2Label != null && canvas.Children.Contains(roi.Point2Label))
                    canvas.Children.Remove(roi.Point2Label);
                if (roi.Arrow != null && canvas.Children.Contains(roi.Arrow))
                    canvas.Children.Remove(roi.Arrow);
                roi.Line = null;
                roi.Point1Dot = null;
                roi.Point2Dot = null;
                roi.LabelBorder = null;
                roi.Point1Label = null;
                roi.Point2Label = null;
                roi.Arrow = null;
            }
        }

        /// <summary>
        /// 重新繪製所有量尺
        /// </summary>
        public void RedrawAllRulers(Canvas canvas, bool showLabels = true)
        {
            // 清除所有現有量尺
            foreach (var elements in rulerElements.Values)
            {
                foreach (var element in elements)
                {
                    canvas.Children.Remove(element);
                }
            }
            rulerElements.Clear();

            // 重新繪製所有量尺
            // 注意：這個方法需要從外部傳入rulerItems集合
            // 實際的重新繪製邏輯應該在RulerService中實現
        }

        public void DrawCurrentRuler(Canvas canvas, RulerItem currentRuler, bool showLabels = true)
        {
            // 移除舊的當前量尺元素
            RemoveCurrentRuler(canvas);

            var elements = new List<UIElement>();

            // 繪製線條
            var line = new Line
            {
                X1 = currentRuler.Point1.X,
                Y1 = currentRuler.Point1.Y,
                X2 = currentRuler.Point2.X,
                Y2 = currentRuler.Point2.Y,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.7,
                IsHitTestVisible = false,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
            };
            canvas.Children.Add(line);
            Canvas.SetZIndex(line, currentRuler.ZIndex);
            elements.Add(line);
            currentRuler.Line = line; // 保存引用

            // 繪製起點
            var point1 = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = Brushes.White,
                Stroke = new SolidColorBrush(styleDict["ruler"].line),
                StrokeThickness = 3,
                Opacity = 0.98,
                Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                IsHitTestVisible = false
            };
            Canvas.SetLeft(point1, currentRuler.Point1.X - 9);
            Canvas.SetTop(point1, currentRuler.Point1.Y - 9);
            canvas.Children.Add(point1);
            Canvas.SetZIndex(point1, currentRuler.ZIndex);
            elements.Add(point1);
            currentRuler.Point1Dot = point1; // 保存引用

            // 繪製終點（如果與起點不同）
            if (currentRuler.Point1 != currentRuler.Point2)
            {
                var point2 = new Ellipse
                {
                    Width = 18,
                    Height = 18,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(styleDict["ruler"].line),
                    StrokeThickness = 3,
                    Opacity = 0.98,
                    Effect = new DropShadowEffect { Color = styleDict["ruler"].glow, BlurRadius = 8, ShadowDepth = 0, Opacity = 0.7 },
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(point2, currentRuler.Point2.X - 9);
                Canvas.SetTop(point2, currentRuler.Point2.Y - 9);
                canvas.Children.Add(point2);
                Canvas.SetZIndex(point2, currentRuler.ZIndex);
                elements.Add(point2);
                currentRuler.Point2Dot = point2; // 保存引用

                // 繪製箭頭
                double dx = currentRuler.Point2.X - currentRuler.Point1.X;
                double dy = currentRuler.Point2.Y - currentRuler.Point1.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist > 30)
                {
                    double angle = Math.Atan2(dy, dx);
                    double arrowLen = 36;
                    double arrowWidth = 18;
                    
                    Point tip = new Point(currentRuler.Point2.X, currentRuler.Point2.Y);
                    Point base1 = new Point(
                        tip.X - arrowLen * Math.Cos(angle) + arrowWidth * Math.Sin(angle) / 2,
                        tip.Y - arrowLen * Math.Sin(angle) - arrowWidth * Math.Cos(angle) / 2);
                    Point base2 = new Point(
                        tip.X - arrowLen * Math.Cos(angle) - arrowWidth * Math.Sin(angle) / 2,
                        tip.Y - arrowLen * Math.Sin(angle) + arrowWidth * Math.Cos(angle) / 2);
                    
                    var arrow = new Polygon
                    {
                        Points = new PointCollection { base1, tip, base2 },
                        Fill = new SolidColorBrush(Color.FromRgb(255, 60, 60)),
                        Stroke = Brushes.White,
                        StrokeThickness = 2.5,
                        Opacity = 0.6,
                        IsHitTestVisible = false,
                        Effect = new DropShadowEffect { Color = Colors.Red, BlurRadius = 10, ShadowDepth = 0, Opacity = 0.7 }
                    };
                    canvas.Children.Add(arrow);
                    Canvas.SetZIndex(arrow, currentRuler.ZIndex);
                    elements.Add(arrow);
                    currentRuler.Arrow = arrow; // 保存引用
                }

                // 繪製預覽標籤
                if (showLabels)
                {
                    double midX = (currentRuler.Point1.X + currentRuler.Point2.X) / 2;
                    double midY = (currentRuler.Point1.Y + currentRuler.Point2.Y) / 2;

                    var label = new TextBlock
                    {
                        Text = currentRuler.DistanceText,
                        Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                        FontWeight = FontWeights.Bold,
                        FontSize = 18,
                        Background = Brushes.Transparent,
                        Opacity = 1.0,
                        Padding = new Thickness(0),
                        TextAlignment = TextAlignment.Center
                    };
                    var labelBorder = new Border
                    {
                        Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                        CornerRadius = new CornerRadius(12),
                        Child = label,
                        Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                        Opacity = 0.98,
                        Padding = new Thickness(10, 4, 10, 4)
                    };
                    Canvas.SetLeft(labelBorder, midX - 80);
                    Canvas.SetTop(labelBorder, midY - 32);
                    canvas.Children.Add(labelBorder);
                    Canvas.SetZIndex(labelBorder, currentRuler.ZIndex);
                    elements.Add(labelBorder);
                    currentRuler.LabelBorder = labelBorder; // 保存引用
                }
            }

            // 儲存當前量尺元素引用（使用特殊ID -1）
            rulerElements[-1] = elements;
        }

        public void RemoveRuler(Canvas canvas, RulerItem ruler)
        {
            if (rulerElements.ContainsKey(ruler.Id))
            {
                foreach (var element in rulerElements[ruler.Id])
                {
                    canvas.Children.Remove(element);
                }
                rulerElements.Remove(ruler.Id);
            }
        }

        public void RemoveCurrentRuler(Canvas canvas)
        {
            if (rulerElements.ContainsKey(-1))
            {
                foreach (var element in rulerElements[-1])
                {
                    canvas.Children.Remove(element);
                }
                rulerElements.Remove(-1);
            }
        }

        /// <summary>
        /// 更新量尺視覺效果
        /// </summary>
        public void UpdateRulerVisual(RulerItem rulerItem)
        {
            if (rulerItem.Line != null)
            {
                rulerItem.Line.X1 = rulerItem.Point1.X;
                rulerItem.Line.Y1 = rulerItem.Point1.Y;
                rulerItem.Line.X2 = rulerItem.Point2.X;
                rulerItem.Line.Y2 = rulerItem.Point2.Y;
            }

            if (rulerItem.Point1Dot != null)
            {
                Canvas.SetLeft(rulerItem.Point1Dot, rulerItem.Point1.X - 9);
                Canvas.SetTop(rulerItem.Point1Dot, rulerItem.Point1.Y - 9);
            }

            if (rulerItem.Point2Dot != null)
            {
                Canvas.SetLeft(rulerItem.Point2Dot, rulerItem.Point2.X - 9);
                Canvas.SetTop(rulerItem.Point2Dot, rulerItem.Point2.Y - 9);
            }

            // 更新標籤位置和內容
            if (rulerItem.Point1Label != null)
            {
                Canvas.SetLeft(rulerItem.Point1Label, rulerItem.Point1.X + 12);
                Canvas.SetTop(rulerItem.Point1Label, rulerItem.Point1.Y - 8);
                
                if (rulerItem.Point1Label.Child is TextBlock textBlock1)
                {
                    textBlock1.Text = rulerItem.Point1Text;
                }
            }

            if (rulerItem.Point2Label != null)
            {
                Canvas.SetLeft(rulerItem.Point2Label, rulerItem.Point2.X + 12);
                Canvas.SetTop(rulerItem.Point2Label, rulerItem.Point2.Y - 8);
                
                if (rulerItem.Point2Label.Child is TextBlock textBlock2)
                {
                    textBlock2.Text = rulerItem.Point2Text;
                }
            }

            if (rulerItem.LabelBorder != null)
            {
                double midX = (rulerItem.Point1.X + rulerItem.Point2.X) / 2;
                double midY = (rulerItem.Point1.Y + rulerItem.Point2.Y) / 2;
                Canvas.SetLeft(rulerItem.LabelBorder, midX - 80);
                Canvas.SetTop(rulerItem.LabelBorder, midY - 32);
                
                if (rulerItem.LabelBorder.Child is TextBlock textBlock)
                {
                    textBlock.Text = rulerItem.DistanceText;
                }
            }

            // 更新箭頭位置
            if (rulerItem.Arrow != null)
            {
                double dx = rulerItem.Point2.X - rulerItem.Point1.X;
                double dy = rulerItem.Point2.Y - rulerItem.Point1.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                
                if (dist > 30)
                {
                    double angle = Math.Atan2(dy, dx);
                    double arrowLen = 36;
                    double arrowWidth = 18;
                    
                    Point tip = new Point(rulerItem.Point2.X, rulerItem.Point2.Y);
                    Point base1 = new Point(
                        tip.X - arrowLen * Math.Cos(angle) + arrowWidth * Math.Sin(angle) / 2,
                        tip.Y - arrowLen * Math.Sin(angle) - arrowWidth * Math.Cos(angle) / 2);
                    Point base2 = new Point(
                        tip.X - arrowLen * Math.Cos(angle) - arrowWidth * Math.Sin(angle) / 2,
                        tip.Y - arrowLen * Math.Sin(angle) + arrowWidth * Math.Cos(angle) / 2);
                    
                    rulerItem.Arrow.Points = new PointCollection { base1, tip, base2 };
                }
            }
        }

        /// <summary>
        /// 移除量尺視覺元素
        /// </summary>
        public void RemoveRulerVisual(RulerItem rulerItem, Canvas canvas)
        {
            if (rulerItem.Line != null)
            {
                canvas.Children.Remove(rulerItem.Line);
                rulerItem.Line = null;
            }

            if (rulerItem.Point1Dot != null)
            {
                canvas.Children.Remove(rulerItem.Point1Dot);
                rulerItem.Point1Dot = null;
            }

            if (rulerItem.Point2Dot != null)
            {
                canvas.Children.Remove(rulerItem.Point2Dot);
                rulerItem.Point2Dot = null;
            }

            if (rulerItem.LabelBorder != null)
            {
                canvas.Children.Remove(rulerItem.LabelBorder);
                rulerItem.LabelBorder = null;
            }

            if (rulerItem.Point1Label != null)
            {
                canvas.Children.Remove(rulerItem.Point1Label);
                rulerItem.Point1Label = null;
            }

            if (rulerItem.Point2Label != null)
            {
                canvas.Children.Remove(rulerItem.Point2Label);
                rulerItem.Point2Label = null;
            }

            if (rulerItem.Arrow != null)
            {
                canvas.Children.Remove(rulerItem.Arrow);
                rulerItem.Arrow = null;
            }

            if (rulerItem.ArrowDot != null)
            {
                canvas.Children.Remove(rulerItem.ArrowDot);
                rulerItem.ArrowDot = null;
            }
        }

        /// <summary>
        /// 添加量尺標籤
        /// </summary>
        public void AddRulerLabels(RulerItem rulerItem, Canvas canvas)
        {
            // 創建起點標籤
            var point1Text = new TextBlock
            {
                Text = rulerItem.Point1Text,
                Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };

            var point1Border = new Border
            {
                Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                CornerRadius = new CornerRadius(8),
                Child = point1Text,
                Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                Opacity = 0.98,
                Padding = new Thickness(6, 2, 6, 2),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(point1Border, rulerItem.Point1.X + 12);
            Canvas.SetTop(point1Border, rulerItem.Point1.Y - 8);
            canvas.Children.Add(point1Border);
            Canvas.SetZIndex(point1Border, rulerItem.ZIndex);
            rulerItem.Point1Label = point1Border;

            // 創建終點標籤
            var point2Text = new TextBlock
            {
                Text = rulerItem.Point2Text,
                Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI"),
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };

            var point2Border = new Border
            {
                Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                CornerRadius = new CornerRadius(8),
                Child = point2Text,
                Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                Opacity = 0.98,
                Padding = new Thickness(6, 2, 6, 2),
                IsHitTestVisible = false
            };

            Canvas.SetLeft(point2Border, rulerItem.Point2.X + 12);
            Canvas.SetTop(point2Border, rulerItem.Point2.Y - 8);
            canvas.Children.Add(point2Border);
            Canvas.SetZIndex(point2Border, rulerItem.ZIndex);
            rulerItem.Point2Label = point2Border;

            // 創建主要標籤
            double midX = (rulerItem.Point1.X + rulerItem.Point2.X) / 2;
            double midY = (rulerItem.Point1.Y + rulerItem.Point2.Y) / 2;

            var label = new TextBlock
            {
                Text = rulerItem.DistanceText,
                Foreground = new SolidColorBrush(styleDict["ruler"].labelFg),
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                Background = Brushes.Transparent,
                Opacity = 1.0,
                Padding = new Thickness(0),
                TextAlignment = TextAlignment.Center
            };
            var labelBorder = new Border
            {
                Background = new SolidColorBrush(styleDict["ruler"].labelBg),
                CornerRadius = new CornerRadius(12),
                Child = label,
                Effect = new DropShadowEffect { Color = Colors.Gray, BlurRadius = 6, ShadowDepth = 2, Opacity = 0.5 },
                Opacity = 0.98,
                Padding = new Thickness(10, 4, 10, 4),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(labelBorder, midX - 80);
            Canvas.SetTop(labelBorder, midY - 32);
            canvas.Children.Add(labelBorder);
            Canvas.SetZIndex(labelBorder, rulerItem.ZIndex);
            rulerItem.LabelBorder = labelBorder;
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
        /// 設置主畫布引用
        /// </summary>
        /// <param name="canvas">主畫布</param>
        public override void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }
    }
} 