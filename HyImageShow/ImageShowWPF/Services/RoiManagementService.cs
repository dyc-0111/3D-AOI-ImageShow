using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HyImageShow.ImageShowWPF.Models;
using HyImageShow.ImageShowWPF.Services;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// ROI 管理服務實作
    /// </summary>
    public class RoiManagementService : IRoiManagementService
    {
        private readonly ObservableCollection<RoiItem> roiItems;
        private RoiItem selectedRoiItem;
        private int nextRoiId = 1;
        private readonly Dictionary<RoiItem, Storyboard> roiHighlightStoryboards = new Dictionary<RoiItem, Storyboard>();
        private readonly Dictionary<RoiItem, DoubleAnimation> roiOpacityAnimations = new Dictionary<RoiItem, DoubleAnimation>();
        private readonly Dictionary<RoiItem, DoubleAnimation> roiStrokeThicknessAnimations = new Dictionary<RoiItem, DoubleAnimation>();
        private Canvas canvas;

        public RoiManagementService()
        {
            roiItems = new ObservableCollection<RoiItem>();
        }

        public ObservableCollection<RoiItem> RoiItems => roiItems;

        public RoiItem SelectedRoiItem
        {
            get => selectedRoiItem;
            set
            {
                if (selectedRoiItem != value)
                {
                    SetSelectedRoiItem(value);
                }
            }
        }

        public event Action<RoiItem> RoiItemAdded;
        public event Action<RoiItem> RoiItemRemoved;
        public event Action RoiItemsCleared;
        public event Action<RoiItem> RoiItemSelectionChanged;

        public void AddRoiItem(object originalObject)
        {
            var roiItem = new RoiItem
            {
                Id = nextRoiId++,
                OriginalObject = originalObject
            };

            // 根據原始物件類型訂閱屬性變更事件
            if (originalObject is LineItem drawLineItem)
            {
                drawLineItem.PropertyChanged += OnLineItemPropertyChanged;
            }
            else if (originalObject is RectRoiItem rectRoiItem)
            {
                rectRoiItem.PropertyChanged += OnRectRoiItemPropertyChanged;
            }
            else if (originalObject is EllipseRoiItem ellipseRoiItem)
            {
                ellipseRoiItem.PropertyChanged += OnEllipseRoiItemPropertyChanged;
            }
            else if (originalObject is RulerItem rulerItem)
            {
                rulerItem.PropertyChanged += OnRulerItemPropertyChanged;
            }
            else if (originalObject is PolygonRoiItem polygonRoiItem)
            {
                polygonRoiItem.PropertyChanged += OnPolygonRoiItemPropertyChanged;
            }
            else if (originalObject is BezierArcRoiItem bezierArcRoiItem)
            {
                bezierArcRoiItem.PropertyChanged += OnBezierArcRoiItemPropertyChanged;
            }
            else if (originalObject is CircularArcRoiItem circularArcRoiItem)
            {
                circularArcRoiItem.PropertyChanged += OnCircularArcRoiItemPropertyChanged;
            }

            roiItems.Add(roiItem);
            
            RoiItemAdded?.Invoke(roiItem);
        }

        public void RemoveRoiItem(RoiItem roiItem)
        {
            if (roiItem != null)
            {
                StopRoiHighlightAnimation(roiItem);
                
                // 取消訂閱屬性變更事件
                if (roiItem.OriginalObject is LineItem drawLineItem)
                {
                    drawLineItem.PropertyChanged -= OnLineItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is RectRoiItem rectRoiItem)
                {
                    rectRoiItem.PropertyChanged -= OnRectRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is EllipseRoiItem ellipseRoiItem)
                {
                    ellipseRoiItem.PropertyChanged -= OnEllipseRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is RulerItem rulerItem)
                {
                    rulerItem.PropertyChanged -= OnRulerItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is PolygonRoiItem polygonRoiItem)
                {
                    polygonRoiItem.PropertyChanged -= OnPolygonRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is BezierArcRoiItem bezierArcRoiItem)
                {
                    bezierArcRoiItem.PropertyChanged -= OnBezierArcRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is CircularArcRoiItem circularArcRoiItem)
                {
                    circularArcRoiItem.PropertyChanged -= OnCircularArcRoiItemPropertyChanged;
                }
                
                roiItems.Remove(roiItem);
                RoiItemRemoved?.Invoke(roiItem);
            }
        }

        public void SetCanvas(Canvas canvas)
        {
            this.canvas = canvas;
        }

        public void ClearRoiItems()
        {
            // 停止所有動畫
            StopAllRoiHighlightAnimations();

            // 取消所有事件訂閱
            foreach (var roiItem in roiItems)
            {
                if (roiItem.OriginalObject is LineItem drawLineItem)
                {
                    drawLineItem.PropertyChanged -= OnLineItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is RectRoiItem rectRoiItem)
                {
                    rectRoiItem.PropertyChanged -= OnRectRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is EllipseRoiItem ellipseRoiItem)
                {
                    ellipseRoiItem.PropertyChanged -= OnEllipseRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is RulerItem rulerItem)
                {
                    rulerItem.PropertyChanged -= OnRulerItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is PolygonRoiItem polygonRoiItem)
                {
                    polygonRoiItem.PropertyChanged -= OnPolygonRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is BezierArcRoiItem bezierArcRoiItem)
                {
                    bezierArcRoiItem.PropertyChanged -= OnBezierArcRoiItemPropertyChanged;
                }
                else if (roiItem.OriginalObject is CircularArcRoiItem circularArcRoiItem)
                {
                    circularArcRoiItem.PropertyChanged -= OnCircularArcRoiItemPropertyChanged;
                }
            }

            roiItems.Clear();
            nextRoiId = 1;
            selectedRoiItem = null;
            RoiItemsCleared?.Invoke();
        }

        public void UpdateRoiListView()
        {
            // 觸發 UI 更新 - 這裡可能需要通知 UI 層刷新
            // 由於 ObservableCollection 會自動通知 UI，通常不需要額外處理
        }

        public void StartRoiHighlightAnimation(RoiItem roiItem)
        {
            if (roiItem == null) return;

            // 停止之前的動畫
            StopRoiHighlightAnimation(roiItem);

            // 根據 ROI 類型創建不同的高亮效果
            if (roiItem.OriginalObject is LineItem drawLineItem)
            {
                // 為畫線創建高亮動畫
                var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 線條透明度動畫
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                if (drawLineItem.Line != null)
                {
                    Storyboard.SetTarget(opacityAnimation, drawLineItem.Line);
                    Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                    storyboard.Children.Add(opacityAnimation);
                }

                // 線條粗細動畫
                var strokeThicknessAnimation = new DoubleAnimation
                {
                    From = 3,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                if (drawLineItem.Line != null)
                {
                    Storyboard.SetTarget(strokeThicknessAnimation, drawLineItem.Line);
                    Storyboard.SetTargetProperty(strokeThicknessAnimation, new PropertyPath(System.Windows.Shapes.Shape.StrokeThicknessProperty));
                    storyboard.Children.Add(strokeThicknessAnimation);
                }

                // 點的高亮動畫
                if (drawLineItem.Point1 != null)
                {
                    var point1OpacityAnimation = new DoubleAnimation
                    {
                        From = 0.98,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(600),
                        AutoReverse = true
                    };
                    Storyboard.SetTarget(point1OpacityAnimation, drawLineItem.Point1);
                    Storyboard.SetTargetProperty(point1OpacityAnimation, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                    storyboard.Children.Add(point1OpacityAnimation);
                }

                if (drawLineItem.Point2 != null)
                {
                    var point2OpacityAnimation = new DoubleAnimation
                    {
                        From = 0.98,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(600),
                        AutoReverse = true
                    };
                    Storyboard.SetTarget(point2OpacityAnimation, drawLineItem.Point2);
                    Storyboard.SetTargetProperty(point2OpacityAnimation, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                    storyboard.Children.Add(point2OpacityAnimation);
                }

                // 標籤高亮動畫
                if (drawLineItem.Point1Label != null)
                {
                    var label1OpacityAnimation = new DoubleAnimation
                    {
                        From = 0.98,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(700),
                        AutoReverse = true
                    };
                    Storyboard.SetTarget(label1OpacityAnimation, drawLineItem.Point1Label);
                    Storyboard.SetTargetProperty(label1OpacityAnimation, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                    storyboard.Children.Add(label1OpacityAnimation);
                }

                if (drawLineItem.Point2Label != null)
                {
                    var label2OpacityAnimation = new DoubleAnimation
                    {
                        From = 0.98,
                        To = 1.0,
                        Duration = TimeSpan.FromMilliseconds(700),
                        AutoReverse = true
                    };
                    Storyboard.SetTarget(label2OpacityAnimation, drawLineItem.Point2Label);
                    Storyboard.SetTargetProperty(label2OpacityAnimation, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                    storyboard.Children.Add(label2OpacityAnimation);
                }

                // 儲存動畫並開始播放
                roiHighlightStoryboards[roiItem] = storyboard;
                storyboard.Begin();
            }
            else if (roiItem.OriginalObject is RulerItem rulerItem)
            {
                // 為量尺創建高亮動畫
                var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 線條透明度動畫
                var opacityAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 線條粗細動畫
                var strokeThicknessAnimation = new DoubleAnimation
                {
                    From = 3,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 端點透明度動畫
                var pointOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 標籤透明度動畫
                var labelOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(700),
                    AutoReverse = true
                };

                // 箭頭透明度動畫
                var arrowOpacityAnimation = new DoubleAnimation
                {
                    From = 0.6,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 在 Canvas 中尋找量尺的 UI 元素並設置動畫目標
                if (canvas != null)
                {
                    // 尋找量尺的線條元素
                    foreach (var child in canvas.Children)
                    {
                        if (child is System.Windows.Shapes.Line line)
                        {
                            // 檢查是否是量尺的線條（通過位置判斷）
                            if (Math.Abs(line.X1 - rulerItem.Point1.X) < 1 && Math.Abs(line.Y1 - rulerItem.Point1.Y) < 1 &&
                                Math.Abs(line.X2 - rulerItem.Point2.X) < 1 && Math.Abs(line.Y2 - rulerItem.Point2.Y) < 1)
                            {
                                // 設置線條動畫
                                Storyboard.SetTarget(opacityAnimation, line);
                                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(opacityAnimation);

                                Storyboard.SetTarget(strokeThicknessAnimation, line);
                                Storyboard.SetTargetProperty(strokeThicknessAnimation, new PropertyPath(System.Windows.Shapes.Shape.StrokeThicknessProperty));
                                storyboard.Children.Add(strokeThicknessAnimation);
                                break;
                            }
                        }
                    }

                    // 尋找量尺的端點元素
                    foreach (var child in canvas.Children)
                    {
                        if (child is System.Windows.Shapes.Ellipse ellipse)
                        {
                            double left = Canvas.GetLeft(ellipse);
                            double top = Canvas.GetTop(ellipse);
                            Point center = new Point(left + ellipse.Width / 2, top + ellipse.Height / 2);

                            // 檢查是否是量尺的端點
                            if ((Math.Abs(center.X - rulerItem.Point1.X) < 10 && Math.Abs(center.Y - rulerItem.Point1.Y) < 10) ||
                                (Math.Abs(center.X - rulerItem.Point2.X) < 10 && Math.Abs(center.Y - rulerItem.Point2.Y) < 10))
                            {
                                var pointAnim = pointOpacityAnimation.Clone();
                                Storyboard.SetTarget(pointAnim, ellipse);
                                Storyboard.SetTargetProperty(pointAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(pointAnim);
                            }
                        }
                    }

                    // 尋找量尺的標籤元素
                    foreach (var child in canvas.Children)
                    {
                        if (child is System.Windows.Controls.Border border)
                        {
                            double left = Canvas.GetLeft(border);
                            double top = Canvas.GetTop(border);
                            Point center = new Point(left + border.ActualWidth / 2, top + border.ActualHeight / 2);
                            Point rulerMidPoint = new Point((rulerItem.Point1.X + rulerItem.Point2.X) / 2, (rulerItem.Point1.Y + rulerItem.Point2.Y) / 2);

                            // 檢查是否是量尺的標籤
                            if (Math.Abs(center.X - rulerMidPoint.X) < 50 && Math.Abs(center.Y - rulerMidPoint.Y) < 50)
                            {
                                var labelAnim = labelOpacityAnimation.Clone();
                                Storyboard.SetTarget(labelAnim, border);
                                Storyboard.SetTargetProperty(labelAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(labelAnim);
                            }
                        }
                    }

                    // 尋找量尺的箭頭元素
                    foreach (var child in canvas.Children)
                    {
                        if (child is System.Windows.Shapes.Polygon polygon)
                        {
                            // 檢查是否是量尺的箭頭（通過位置判斷）
                            if (polygon.Points.Count == 3)
                            {
                                Point tip = polygon.Points[1]; // 箭頭尖端
                                if (Math.Abs(tip.X - rulerItem.Point2.X) < 5 && Math.Abs(tip.Y - rulerItem.Point2.Y) < 5)
                                {
                                    var arrowAnim = arrowOpacityAnimation.Clone();
                                    Storyboard.SetTarget(arrowAnim, polygon);
                                    Storyboard.SetTargetProperty(arrowAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                    storyboard.Children.Add(arrowAnim);
                                }
                            }
                        }
                    }
                }

                // 儲存動畫並開始播放
                roiHighlightStoryboards[roiItem] = storyboard;
                storyboard.Begin();
            }
            else if (roiItem.OriginalObject is RectRoiItem rectRoiItem)
            {
                // 為矩形 ROI 創建高亮動畫
                var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 矩形邊框透明度動畫
                var polygonOpacityAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 矩形邊框粗細動畫
                var polygonStrokeThicknessAnimation = new DoubleAnimation
                {
                    From = 3,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 尋找矩形ROI的Polygon元素
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Polygon polygon)
                    {
                        // 檢查是否是矩形ROI的Polygon（透過樣式判斷）
                        if (polygon.Stroke is SolidColorBrush brush && 
                            brush.Color == Color.FromRgb(33, 150, 243)) // 藍色
                        {
                            // 檢查位置是否匹配
                            if (polygon.Points.Count == 4)
                            {
                                Point center = new Point(
                                    polygon.Points.Average(p => p.X),
                                    polygon.Points.Average(p => p.Y)
                                );
                                
                                if (Math.Abs(center.X - rectRoiItem.CenterPoint.X) < 50 && 
                                    Math.Abs(center.Y - rectRoiItem.CenterPoint.Y) < 50)
                                {
                                    var opacityAnim = polygonOpacityAnimation.Clone();
                                    Storyboard.SetTarget(opacityAnim, polygon);
                                    Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                    storyboard.Children.Add(opacityAnim);

                                    var strokeAnim = polygonStrokeThicknessAnimation.Clone();
                                    Storyboard.SetTarget(strokeAnim, polygon);
                                    Storyboard.SetTargetProperty(strokeAnim, new PropertyPath(System.Windows.Shapes.Shape.StrokeThicknessProperty));
                                    storyboard.Children.Add(strokeAnim);
                                }
                            }
                        }
                    }
                }

                // 角落控制點高亮動畫
                var cornerOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var cornerScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 尋找角落控制點
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        // 檢查是否是矩形ROI的角落點（白色填充，藍色邊框）
                        if (ellipse.Fill is SolidColorBrush fillBrush && 
                            fillBrush.Color == Colors.White &&
                            ellipse.Stroke is SolidColorBrush strokeBrush && 
                            strokeBrush.Color == Color.FromRgb(33, 150, 243))
                        {
                            // 檢查位置是否在矩形ROI附近
                            Point ellipseCenter = new Point(
                                Canvas.GetLeft(ellipse) + ellipse.Width / 2,
                                Canvas.GetTop(ellipse) + ellipse.Height / 2
                            );
                            
                            if (Math.Abs(ellipseCenter.X - rectRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(ellipseCenter.Y - rectRoiItem.CenterPoint.Y) < 100)
                            {
                                var cornerOpacityAnim = cornerOpacityAnimation.Clone();
                                Storyboard.SetTarget(cornerOpacityAnim, ellipse);
                                Storyboard.SetTargetProperty(cornerOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(cornerOpacityAnim);

                                // 如果Ellipse有RenderTransform，添加縮放動畫
                                if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                                {
                                    var scaleAnim = cornerScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                    storyboard.Children.Add(scaleAnim);

                                    var scaleYAnim = cornerScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                    storyboard.Children.Add(scaleYAnim);
                                }
                            }
                        }
                    }
                }

                // 旋轉點高亮動畫
                var rotateOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var rotateScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.3,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 尋找旋轉點（亮橘色）
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        if (ellipse.Fill is SolidColorBrush fillBrush && 
                            fillBrush.Color == Color.FromRgb(255, 167, 38)) // 亮橘色
                        {
                            // 檢查位置是否在矩形ROI附近
                            Point ellipseCenter = new Point(
                                Canvas.GetLeft(ellipse) + ellipse.Width / 2,
                                Canvas.GetTop(ellipse) + ellipse.Height / 2
                            );
                            
                            if (Math.Abs(ellipseCenter.X - rectRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(ellipseCenter.Y - rectRoiItem.CenterPoint.Y) < 100)
                            {
                                var rotateOpacityAnim = rotateOpacityAnimation.Clone();
                                Storyboard.SetTarget(rotateOpacityAnim, ellipse);
                                Storyboard.SetTargetProperty(rotateOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(rotateOpacityAnim);

                                // 如果Ellipse有RenderTransform，添加縮放動畫
                                if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                                {
                                    var scaleAnim = rotateScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                    storyboard.Children.Add(scaleAnim);

                                    var scaleYAnim = rotateScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                    storyboard.Children.Add(scaleYAnim);
                                }
                            }
                        }
                    }
                }

                // 標籤高亮動畫
                var labelOpacityAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(700),
                    AutoReverse = true
                };

                // 尋找矩形ROI的標籤
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Controls.Border border)
                    {
                        // 檢查是否是矩形ROI的標籤（透過背景色判斷）
                        if (border.Background is SolidColorBrush brush && 
                            brush.Color == Color.FromArgb(220, 187, 222, 251)) // 淺藍色背景
                        {
                            // 檢查位置是否在矩形ROI附近
                            Point labelCenter = new Point(
                                Canvas.GetLeft(border) + border.ActualWidth / 2,
                                Canvas.GetTop(border) + border.ActualHeight / 2
                            );
                            
                            if (Math.Abs(labelCenter.X - rectRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(labelCenter.Y - rectRoiItem.CenterPoint.Y) < 100)
                            {
                                var labelAnim = labelOpacityAnimation.Clone();
                                Storyboard.SetTarget(labelAnim, border);
                                Storyboard.SetTargetProperty(labelAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(labelAnim);
                            }
                        }
                    }
                }

                // 儲存動畫並開始播放
                roiHighlightStoryboards[roiItem] = storyboard;
                storyboard.Begin();
            }
            else if (roiItem.OriginalObject is EllipseRoiItem ellipseRoiItem)
            {
                // 為圓形 ROI 創建高亮動畫
                var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 圓形邊框透明度動畫
                var ellipseOpacityAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 圓形邊框粗細動畫
                var ellipseStrokeThicknessAnimation = new DoubleAnimation
                {
                    From = 2,
                    To = 4,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 尋找圓形ROI的 Ellipse
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        // 檢查是否是圓形ROI的 Ellipse（透過樣式判斷）
                        if (ellipse.Stroke is SolidColorBrush brush && 
                            brush.Color == Color.FromRgb(140, 90, 220)) // 紫色
                        {
                            // 檢查位置是否在圓形ROI附近
                            Point ellipseCenter = new Point(
                                Canvas.GetLeft(ellipse) + ellipse.Width / 2,
                                Canvas.GetTop(ellipse) + ellipse.Height / 2
                            );
                            
                            if (Math.Abs(ellipseCenter.X - ellipseRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(ellipseCenter.Y - ellipseRoiItem.CenterPoint.Y) < 100)
                            {
                                var opacityAnim = ellipseOpacityAnimation.Clone();
                                Storyboard.SetTarget(opacityAnim, ellipse);
                                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(opacityAnim);

                                var thicknessAnim = ellipseStrokeThicknessAnimation.Clone();
                                Storyboard.SetTarget(thicknessAnim, ellipse);
                                Storyboard.SetTargetProperty(thicknessAnim, new PropertyPath(System.Windows.Shapes.Shape.StrokeThicknessProperty));
                                storyboard.Children.Add(thicknessAnim);
                            }
                        }
                    }
                }

                // 控制點高亮動畫
                var centerDotOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var centerDotScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.3,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var radiusDotOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var radiusDotScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.3,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 尋找中心點（白色填充，紫色邊框）
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        if (ellipse.Fill is SolidColorBrush fillBrush && 
                            fillBrush.Color == Colors.White &&
                            ellipse.Stroke is SolidColorBrush strokeBrush && 
                            strokeBrush.Color == Color.FromRgb(140, 90, 220))
                        {
                            // 檢查位置是否在圓形ROI附近
                            Point ellipseCenter = new Point(
                                Canvas.GetLeft(ellipse) + ellipse.Width / 2,
                                Canvas.GetTop(ellipse) + ellipse.Height / 2
                            );
                            
                            if (Math.Abs(ellipseCenter.X - ellipseRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(ellipseCenter.Y - ellipseRoiItem.CenterPoint.Y) < 100)
                            {
                                var centerOpacityAnim = centerDotOpacityAnimation.Clone();
                                Storyboard.SetTarget(centerOpacityAnim, ellipse);
                                Storyboard.SetTargetProperty(centerOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(centerOpacityAnim);

                                // 如果Ellipse有RenderTransform，添加縮放動畫
                                if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                                {
                                    var scaleAnim = centerDotScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                    storyboard.Children.Add(scaleAnim);

                                    var scaleYAnim = centerDotScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                    storyboard.Children.Add(scaleYAnim);
                                }
                            }
                        }
                    }
                }

                // 尋找半徑點（黃色填充，橘色邊框）
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        if (ellipse.Fill is SolidColorBrush fillBrush && 
                            fillBrush.Color == Color.FromRgb(255, 235, 59) &&
                            ellipse.Stroke is SolidColorBrush strokeBrush && 
                            strokeBrush.Color == Color.FromRgb(255, 87, 34))
                        {
                            // 檢查位置是否在圓形ROI附近
                            Point ellipseCenter = new Point(
                                Canvas.GetLeft(ellipse) + ellipse.Width / 2,
                                Canvas.GetTop(ellipse) + ellipse.Height / 2
                            );
                            
                            if (Math.Abs(ellipseCenter.X - ellipseRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(ellipseCenter.Y - ellipseRoiItem.CenterPoint.Y) < 100)
                            {
                                var radiusOpacityAnim = radiusDotOpacityAnimation.Clone();
                                Storyboard.SetTarget(radiusOpacityAnim, ellipse);
                                Storyboard.SetTargetProperty(radiusOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(radiusOpacityAnim);

                                // 如果Ellipse有RenderTransform，添加縮放動畫
                                if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                                {
                                    var scaleAnim = radiusDotScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                    storyboard.Children.Add(scaleAnim);

                                    var scaleYAnim = radiusDotScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                    storyboard.Children.Add(scaleYAnim);
                                }
                            }
                        }
                    }
                }

                // 標籤高亮動畫
                var labelOpacityAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(700),
                    AutoReverse = true
                };

                // 尋找圓形ROI的標籤
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Controls.Border border)
                    {
                        // 檢查是否是圓形ROI的標籤（透過背景色判斷）
                        if (border.Background is SolidColorBrush brush && 
                            brush.Color == Color.FromArgb(220, 210, 180, 255)) // 淺紫色背景
                        {
                            // 檢查位置是否在圓形ROI附近
                            Point labelCenter = new Point(
                                Canvas.GetLeft(border) + border.ActualWidth / 2,
                                Canvas.GetTop(border) + border.ActualHeight / 2
                            );
                            
                            if (Math.Abs(labelCenter.X - ellipseRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(labelCenter.Y - ellipseRoiItem.CenterPoint.Y) < 100)
                            {
                                var labelAnim = labelOpacityAnimation.Clone();
                                Storyboard.SetTarget(labelAnim, border);
                                Storyboard.SetTargetProperty(labelAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(labelAnim);
                            }
                        }
                    }
                }

                // 儲存動畫並開始播放
                roiHighlightStoryboards[roiItem] = storyboard;
                storyboard.Begin();
            }
            else if (roiItem.OriginalObject is CircularArcRoiItem circularArcRoiItem)
            {
                // 為圓弧 ROI 創建高亮動畫
                var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 圓弧邊框透明度動畫
                var pathOpacityAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 圓弧邊框粗細動畫
                var pathStrokeThicknessAnimation = new DoubleAnimation
                {
                    From = 2,
                    To = 4,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 尋找圓弧ROI的Path元素
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Path path)
                    {
                        // 檢查是否是圓弧ROI的Path（透過Tag判斷）
                        if (path.Tag == circularArcRoiItem)
                        {
                            var opacityAnim = pathOpacityAnimation.Clone();
                            Storyboard.SetTarget(opacityAnim, path);
                            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                            storyboard.Children.Add(opacityAnim);

                            var strokeAnim = pathStrokeThicknessAnimation.Clone();
                            Storyboard.SetTarget(strokeAnim, path);
                            Storyboard.SetTargetProperty(strokeAnim, new PropertyPath(System.Windows.Shapes.Shape.StrokeThicknessProperty));
                            storyboard.Children.Add(strokeAnim);
                        }
                    }
                }

                // 控制點高亮動畫
                var controlDotOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var controlDotScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 尋找控制點
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        // 檢查是否是圓弧ROI的控制點（透過Tag判斷）
                        if (ellipse.Tag == circularArcRoiItem)
                        {
                            var controlDotOpacityAnim = controlDotOpacityAnimation.Clone();
                            Storyboard.SetTarget(controlDotOpacityAnim, ellipse);
                            Storyboard.SetTargetProperty(controlDotOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                            storyboard.Children.Add(controlDotOpacityAnim);

                            // 如果Ellipse有RenderTransform，添加縮放動畫
                            if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                            {
                                var scaleAnim = controlDotScaleAnimation.Clone();
                                Storyboard.SetTarget(scaleAnim, scaleTransform);
                                Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                storyboard.Children.Add(scaleAnim);

                                var scaleYAnim = controlDotScaleAnimation.Clone();
                                Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                storyboard.Children.Add(scaleYAnim);
                            }
                        }
                    }
                }

                // 標籤高亮動畫
                var labelOpacityAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(700),
                    AutoReverse = true
                };

                // 尋找圓弧ROI的標籤
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Controls.Border border)
                    {
                        // 檢查是否是圓弧ROI的標籤（透過Tag判斷）
                        if (border.Tag == circularArcRoiItem)
                        {
                            var labelAnim = labelOpacityAnimation.Clone();
                            Storyboard.SetTarget(labelAnim, border);
                            Storyboard.SetTargetProperty(labelAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                            storyboard.Children.Add(labelAnim);
                        }
                    }
                }

                // 儲存動畫並開始播放
                roiHighlightStoryboards[roiItem] = storyboard;
                storyboard.Begin();
            }
            else if (roiItem.OriginalObject is PolygonRoiItem polygonRoiItem)
            {
                // 為多邊形 ROI 創建高亮動畫
                var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 多邊形邊框透明度動畫
                var polylineOpacityAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 多邊形邊框粗細動畫
                var polylineStrokeThicknessAnimation = new DoubleAnimation
                {
                    From = 3,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 尋找多邊形ROI的Polyline元素
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Polyline polyline)
                    {
                        // 檢查是否是多邊形ROI的Polyline（透過樣式判斷）
                        if (polyline.Stroke is SolidColorBrush brush && 
                            brush.Color == Color.FromRgb(220, 180, 40)) // 黃色
                        {
                            // 檢查位置是否匹配
                            if (polyline.Points.Count > 0)
                            {
                                Point center = new Point(
                                    polyline.Points.Average(p => p.X),
                                    polyline.Points.Average(p => p.Y)
                                );
                                
                                if (Math.Abs(center.X - polygonRoiItem.CenterPoint.X) < 50 && 
                                    Math.Abs(center.Y - polygonRoiItem.CenterPoint.Y) < 50)
                                {
                                    var opacityAnim = polylineOpacityAnimation.Clone();
                                    Storyboard.SetTarget(opacityAnim, polyline);
                                    Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                    storyboard.Children.Add(opacityAnim);

                                    var strokeAnim = polylineStrokeThicknessAnimation.Clone();
                                    Storyboard.SetTarget(strokeAnim, polyline);
                                    Storyboard.SetTargetProperty(strokeAnim, new PropertyPath(System.Windows.Shapes.Shape.StrokeThicknessProperty));
                                    storyboard.Children.Add(strokeAnim);
                                }
                            }
                        }
                    }
                }

                // 頂點控制點高亮動畫
                var vertexOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var vertexScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 尋找頂點控制點
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        // 檢查是否是多邊形ROI的頂點點（白色填充，黃色邊框）
                        if (ellipse.Fill is SolidColorBrush fillBrush && 
                            fillBrush.Color == Colors.White &&
                            ellipse.Stroke is SolidColorBrush strokeBrush && 
                            strokeBrush.Color == Color.FromRgb(220, 180, 40))
                        {
                            // 檢查位置是否在多邊形ROI附近
                            Point ellipseCenter = new Point(
                                Canvas.GetLeft(ellipse) + ellipse.Width / 2,
                                Canvas.GetTop(ellipse) + ellipse.Height / 2
                            );
                            
                            if (Math.Abs(ellipseCenter.X - polygonRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(ellipseCenter.Y - polygonRoiItem.CenterPoint.Y) < 100)
                            {
                                var vertexOpacityAnim = vertexOpacityAnimation.Clone();
                                Storyboard.SetTarget(vertexOpacityAnim, ellipse);
                                Storyboard.SetTargetProperty(vertexOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(vertexOpacityAnim);

                                // 如果Ellipse有RenderTransform，添加縮放動畫
                                if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                                {
                                    var scaleAnim = vertexScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                    storyboard.Children.Add(scaleAnim);

                                    var scaleYAnim = vertexScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                    storyboard.Children.Add(scaleYAnim);
                                }
                            }
                        }
                    }
                }

                // 旋轉點高亮動畫
                var rotateOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var rotateScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.3,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 尋找旋轉點（亮橘色）
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        if (ellipse.Fill is SolidColorBrush fillBrush && 
                            fillBrush.Color == Color.FromRgb(255, 235, 59)) // 亮橘色
                        {
                            // 檢查位置是否在多邊形ROI附近
                            Point ellipseCenter = new Point(
                                Canvas.GetLeft(ellipse) + ellipse.Width / 2,
                                Canvas.GetTop(ellipse) + ellipse.Height / 2
                            );
                            
                            if (Math.Abs(ellipseCenter.X - polygonRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(ellipseCenter.Y - polygonRoiItem.CenterPoint.Y) < 100)
                            {
                                var rotateOpacityAnim = rotateOpacityAnimation.Clone();
                                Storyboard.SetTarget(rotateOpacityAnim, ellipse);
                                Storyboard.SetTargetProperty(rotateOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(rotateOpacityAnim);

                                // 如果Ellipse有RenderTransform，添加縮放動畫
                                if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                                {
                                    var scaleAnim = rotateScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                    storyboard.Children.Add(scaleAnim);

                                    var scaleYAnim = rotateScaleAnimation.Clone();
                                    Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                    Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                    storyboard.Children.Add(scaleYAnim);
                                }
                            }
                        }
                    }
                }

                // 標籤高亮動畫
                var labelOpacityAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(700),
                    AutoReverse = true
                };

                // 尋找多邊形ROI的標籤
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Controls.Border border)
                    {
                        // 檢查是否是多邊形ROI的標籤（透過背景色判斷）
                        if (border.Background is SolidColorBrush brush && 
                            brush.Color == Color.FromArgb(220, 240, 220, 120)) // 淺黃色背景
                        {
                            // 檢查位置是否在多邊形ROI附近
                            Point labelCenter = new Point(
                                Canvas.GetLeft(border) + border.ActualWidth / 2,
                                Canvas.GetTop(border) + border.ActualHeight / 2
                            );
                            
                            if (Math.Abs(labelCenter.X - polygonRoiItem.CenterPoint.X) < 100 && 
                                Math.Abs(labelCenter.Y - polygonRoiItem.CenterPoint.Y) < 100)
                            {
                                var labelAnim = labelOpacityAnimation.Clone();
                                Storyboard.SetTarget(labelAnim, border);
                                Storyboard.SetTargetProperty(labelAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                                storyboard.Children.Add(labelAnim);
                            }
                        }
                    }
                }

                // 儲存動畫並開始播放
                roiHighlightStoryboards[roiItem] = storyboard;
                storyboard.Begin();
            }
            else if (roiItem.OriginalObject is BezierArcRoiItem bezierArcRoiItem)
            {
                // 為貝塞爾弧線 ROI 創建高亮動畫
                var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

                // 貝塞爾弧線邊框透明度動畫
                var pathOpacityAnimation = new DoubleAnimation
                {
                    From = 0.7,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 貝塞爾弧線邊框粗細動畫
                var pathStrokeThicknessAnimation = new DoubleAnimation
                {
                    From = 2,
                    To = 4,
                    Duration = TimeSpan.FromMilliseconds(800),
                    AutoReverse = true
                };

                // 尋找貝塞爾弧線ROI的Path元素
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Path path)
                    {
                        // 檢查是否是貝塞爾弧線ROI的Path（透過Tag判斷）
                        if (path.Tag == bezierArcRoiItem)
                        {
                            var opacityAnim = pathOpacityAnimation.Clone();
                            Storyboard.SetTarget(opacityAnim, path);
                            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                            storyboard.Children.Add(opacityAnim);

                            var strokeAnim = pathStrokeThicknessAnimation.Clone();
                            Storyboard.SetTarget(strokeAnim, path);
                            Storyboard.SetTargetProperty(strokeAnim, new PropertyPath(System.Windows.Shapes.Shape.StrokeThicknessProperty));
                            storyboard.Children.Add(strokeAnim);
                        }
                    }
                }

                // 控制點高亮動畫
                var controlDotOpacityAnimation = new DoubleAnimation
                {
                    From = 0.98,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                var controlDotScaleAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.2,
                    Duration = TimeSpan.FromMilliseconds(600),
                    AutoReverse = true
                };

                // 尋找控制點
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Ellipse ellipse)
                    {
                        // 檢查是否是貝塞爾弧線ROI的控制點（透過Tag判斷）
                        if (ellipse.Tag == bezierArcRoiItem)
                        {
                            var controlDotOpacityAnim = controlDotOpacityAnimation.Clone();
                            Storyboard.SetTarget(controlDotOpacityAnim, ellipse);
                            Storyboard.SetTargetProperty(controlDotOpacityAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                            storyboard.Children.Add(controlDotOpacityAnim);

                            // 如果Ellipse有RenderTransform，添加縮放動畫
                            if (ellipse.RenderTransform is ScaleTransform scaleTransform)
                            {
                                var scaleAnim = controlDotScaleAnimation.Clone();
                                Storyboard.SetTarget(scaleAnim, scaleTransform);
                                Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                                storyboard.Children.Add(scaleAnim);

                                var scaleYAnim = controlDotScaleAnimation.Clone();
                                Storyboard.SetTarget(scaleYAnim, scaleTransform);
                                Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath(ScaleTransform.ScaleYProperty));
                                storyboard.Children.Add(scaleYAnim);
                            }
                        }
                    }
                }

                // 標籤高亮動畫
                var labelOpacityAnimation = new DoubleAnimation
                {
                    From = 0.95,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(700),
                    AutoReverse = true
                };

                // 尋找貝塞爾弧線ROI的標籤
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Controls.Border border)
                    {
                        // 檢查是否是貝塞爾弧線ROI的標籤（透過Tag判斷）
                        if (border.Tag == bezierArcRoiItem)
                        {
                            var labelAnim = labelOpacityAnimation.Clone();
                            Storyboard.SetTarget(labelAnim, border);
                            Storyboard.SetTargetProperty(labelAnim, new PropertyPath(System.Windows.UIElement.OpacityProperty));
                            storyboard.Children.Add(labelAnim);
                        }
                    }
                }

                // 儲存動畫並開始播放
                roiHighlightStoryboards[roiItem] = storyboard;
                storyboard.Begin();
            }
            else if (roiItem.OriginalObject is PointItem pointItem)
            {
                // 停止之前的動畫
                pointItem.PointScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
                pointItem.PointScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
                pointItem.PointScale.ScaleX = 1.0;
                pointItem.PointScale.ScaleY = 1.0;

                // 建立縮放動畫
                var scaleAnim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 1.0,
                    To = 1.5,
                    Duration = TimeSpan.FromMilliseconds(300),
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                };
                pointItem.PointScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
                pointItem.PointScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
            }
        }

        public void StopRoiHighlightAnimation(RoiItem roiItem)
        {
            if (roiItem != null && roiHighlightStoryboards.ContainsKey(roiItem))
            {
                roiHighlightStoryboards[roiItem].Stop();
                roiHighlightStoryboards.Remove(roiItem);
            }

            if (roiItem != null && roiOpacityAnimations.ContainsKey(roiItem))
            {
                roiOpacityAnimations.Remove(roiItem);
            }

            if (roiItem != null && roiStrokeThicknessAnimations.ContainsKey(roiItem))
            {
                roiStrokeThicknessAnimations.Remove(roiItem);
            }

            // 停止 PointItem 的動畫
            if (roiItem?.OriginalObject is PointItem pointItem)
            {
                pointItem.PointScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
                pointItem.PointScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
                pointItem.PointScale.ScaleX = 1.0;
                pointItem.PointScale.ScaleY = 1.0;
            }
        }

        public void StopAllRoiHighlightAnimations()
        {
            foreach (var roiItem in roiItems)
            {
                StopRoiHighlightAnimation(roiItem);
            }
        }

        public void SetSelectedRoiItem(RoiItem roiItem)
        {
            // 先停止所有高亮動畫
            StopAllRoiHighlightAnimations();

            // 清除之前選中項目的 IsSelected 狀態
            foreach (var item in roiItems)
            {
                item.IsSelected = false;
            }

            // 設置新選中項目的 IsSelected 狀態
            if (roiItem != null)
            {
                roiItem.IsSelected = true;
            }

            // 直接設置 selectedRoiItem 字段，避免無限遞迴
            selectedRoiItem = roiItem;
            
            // 觸發選擇變更事件
            RoiItemSelectionChanged?.Invoke(selectedRoiItem);

            // 高亮選中的 ROI
            if (roiItem != null)
            {
                StartRoiHighlightAnimation(roiItem);
            }
        }

        public void ClearSelectedRoiItem()
        {
            SetSelectedRoiItem(null);
        }

        private void OnLineItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 當 LineItem 的 P1 或 P2 變更時，更新對應的 ROI 項目
            if (sender is LineItem lineItem && (e.PropertyName == nameof(LineItem.P1) || e.PropertyName == nameof(LineItem.P2)))
            {
                var roiItem = roiItems.FirstOrDefault(r => r.OriginalObject == lineItem);
                if (roiItem != null)
                {
                    // RoiItem 現在繼承 BaseItem，會自動從原始物件獲取最新資料
                    // 不需要手動更新，因為 RoiItem 會監聽原始物件的屬性變更
                }
            }
        }

        private void OnRectRoiItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 當 RectRoiItem 的屬性變更時，更新對應的 ROI 項目
            if (sender is RectRoiItem rectRoiItem && 
                (e.PropertyName == nameof(RectRoiItem.CenterPoint) || 
                 e.PropertyName == nameof(RectRoiItem.Width) || 
                 e.PropertyName == nameof(RectRoiItem.Height) || 
                 e.PropertyName == nameof(RectRoiItem.RotationAngle)))
            {
                var roiItem = roiItems.FirstOrDefault(r => r.OriginalObject == rectRoiItem);
                if (roiItem != null)
                {
                    // RoiItem 現在繼承 BaseItem，會自動從原始物件獲取最新資料
                    // 不需要手動更新，因為 RoiItem 會監聽原始物件的屬性變更
                }
            }
        }

        private void OnEllipseRoiItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 當 EllipseRoiItem 的屬性變更時，更新對應的 ROI 項目
            if (sender is EllipseRoiItem ellipseRoiItem && 
                (e.PropertyName == nameof(EllipseRoiItem.CenterPoint) || 
                 e.PropertyName == nameof(EllipseRoiItem.Radius)))
            {
                var roiItem = roiItems.FirstOrDefault(r => r.OriginalObject == ellipseRoiItem);
                if (roiItem != null)
                {
                    // RoiItem 現在繼承 BaseItem，會自動從原始物件獲取最新資料
                    // 不需要手動更新，因為 RoiItem 會監聽原始物件的屬性變更
                }
            }
        }

        private void OnRulerItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 當 RulerItem 的屬性變更時，更新對應的 ROI 項目
            if (sender is RulerItem rulerItem && 
                (e.PropertyName == nameof(RulerItem.Point1) || 
                 e.PropertyName == nameof(RulerItem.Point2) || 
                 e.PropertyName == nameof(RulerItem.Distance)))
            {
                var roiItem = roiItems.FirstOrDefault(r => r.OriginalObject == rulerItem);
                if (roiItem != null)
                {
                    // RoiItem 現在繼承 BaseItem，會自動從原始物件獲取最新資料
                    // 不需要手動更新，因為 RoiItem 會監聽原始物件的屬性變更
                }
            }
        }

        private void OnPolygonRoiItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 當 PolygonRoiItem 的屬性變更時，更新對應的 ROI 項目
            if (sender is PolygonRoiItem polygonRoiItem && 
                (e.PropertyName == nameof(PolygonRoiItem.Points) || 
                 e.PropertyName == nameof(PolygonRoiItem.Angle) ||
                 e.PropertyName == nameof(PolygonRoiItem.RotationAngle)))
            {
                var roiItem = roiItems.FirstOrDefault(r => r.OriginalObject == polygonRoiItem);
                if (roiItem != null)
                {
                    // RoiItem 現在繼承 BaseItem，會自動從原始物件獲取最新資料
                    // 不需要手動更新，因為 RoiItem 會監聽原始物件的屬性變更
                }
            }
        }

        private void OnBezierArcRoiItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 當 BezierArcRoiItem 的屬性變更時，更新對應的 ROI 項目
            if (sender is BezierArcRoiItem bezierArcRoiItem && 
                (e.PropertyName == nameof(BezierArcRoiItem.StartPoint) ||
                 e.PropertyName == nameof(BezierArcRoiItem.EndPoint) ||
                 e.PropertyName == nameof(BezierArcRoiItem.MiddlePoint) ||
                 e.PropertyName == nameof(BezierArcRoiItem.Center) ||
                 e.PropertyName == nameof(BezierArcRoiItem.ArcLength) ||
                 e.PropertyName == nameof(BezierArcRoiItem.ChordLength)))
            {
                var roiItem = roiItems.FirstOrDefault(r => r.OriginalObject == bezierArcRoiItem);
                if (roiItem != null)
                {
                    // RoiItem 現在繼承 BaseItem，會自動從原始物件獲取最新資料
                    // 不需要手動更新，因為 RoiItem 會監聽原始物件的屬性變更
                }
            }
        }

        private void OnCircularArcRoiItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // 當 CircularArcRoiItem 的屬性變更時，更新對應的 ROI 項目
            if (sender is CircularArcRoiItem circularArcRoiItem && 
                (e.PropertyName == nameof(CircularArcRoiItem.StartPoint) ||
                 e.PropertyName == nameof(CircularArcRoiItem.EndPoint) ||
                 e.PropertyName == nameof(CircularArcRoiItem.UserMidPoint) ||
                 e.PropertyName == nameof(CircularArcRoiItem.CenterPoint) ||
                 e.PropertyName == nameof(CircularArcRoiItem.ArcLength) ||
                 e.PropertyName == nameof(CircularArcRoiItem.ChordLength)))
            {
                var roiItem = roiItems.FirstOrDefault(r => r.OriginalObject == circularArcRoiItem);
                if (roiItem != null)
                {
                    // RoiItem 現在繼承 BaseItem，會自動從原始物件獲取最新資料
                    // 不需要手動更新，因為 RoiItem 會監聽原始物件的屬性變更
                }
            }
        }
    }
} 