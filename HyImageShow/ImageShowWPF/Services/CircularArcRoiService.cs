using HyImageShow.ImageShowWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 圓弧ROI服務實現
    /// </summary>
    public class CircularArcRoiService : BaseRoiService<CircularArcRoiItem, CircularArcRoiDrawingService>
    {
        private ObservableCollection<CircularArcRoiItem> circularArcRois;
        private CircularArcRoiItem currentCircularArcRoi;
        private bool isCircularArcRoiMode;
        private int clickCount;
        private bool isDraggingDot;
        private int draggingDotIndex;
        private Canvas mainCanvas;
        private CircularArcRoiDrawingService drawingService;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private const int UPDATE_THROTTLE_MS = 16; // 約60FPS
        public bool AllowMultiDrag { get; set; } = false; // 預設單一拖曳

        public CircularArcRoiService()
        {
            circularArcRois = new ObservableCollection<CircularArcRoiItem>();
            isCircularArcRoiMode = false;
            clickCount = 0;
            isDraggingDot = false;
            draggingDotIndex = -1;
        }

        public ObservableCollection<CircularArcRoiItem> CircularArcRois => circularArcRois;

        public CircularArcRoiItem CurrentCircularArcRoi => currentCircularArcRoi;

        public bool IsCircularArcRoiMode => isCircularArcRoiMode;

        public int ClickCount => clickCount;

        public event Action<CircularArcRoiItem> CircularArcRoiCompleted;
        public event Action<CircularArcRoiItem> CircularArcRoiUpdated;
        public event Action<CircularArcRoiItem> CircularArcRoiRemoved;
        public event Action CircularArcRoisCleared;

        public void EnableCircularArcRoiMode()
        {
            isCircularArcRoiMode = true;
            clickCount = 0;
            currentCircularArcRoi = new CircularArcRoiItem();
            ZIndexManager.Instance.Register(currentCircularArcRoi);
        }

        public void DisableCircularArcRoiMode()
        {
            isCircularArcRoiMode = false;
            clickCount = 0;
            currentCircularArcRoi = null;
            isDraggingDot = false;
            draggingDotIndex = -1;
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            // 先清空所有 ROI 拖曳狀態（單選模式）
            if (!AllowMultiDrag || !isShift)
            {
                foreach (var roi in circularArcRois)
                {
                    roi.IsDraggingDot = false;
                    roi.DraggingDotIndex = -1;
                    roi.Selected = false;
                }
            }
            // 只找 Z-Index 最大的命中 ROI
            var hitRoi = GetHitCircularArcRoiItem(pos);
            if (hitRoi != null && hitRoi.IsCompleted)
            {
                if (AllowMultiDrag && isShift)
                {
                    hitRoi.Selected = !hitRoi.Selected;
                }
                else
                {
                    hitRoi.Selected = true;
                }
                Point[] points = { hitRoi.StartPoint, hitRoi.EndPoint, hitRoi.UserMidPoint };
                for (int i = 0; i < 3; i++)
                {
                    if ((pos - points[i]).Length < 15)
                    {
                        isDraggingDot = true;
                        draggingDotIndex = i;
                        currentCircularArcRoi = hitRoi;
                        ZIndexManager.Instance.BringToFront(currentCircularArcRoi);
                        if (canvas != null && !canvas.IsMouseCaptured)
                        {
                            canvas.CaptureMouse();
                        }
                        return;
                    }
                }
                return;
            }

            // 檢查是否點擊到剛完成的ROI（如果currentCircularArcRoi不為null且已完成）
            if (currentCircularArcRoi != null && currentCircularArcRoi.IsCompleted)
            {
                Point[] points = { currentCircularArcRoi.StartPoint, currentCircularArcRoi.EndPoint, currentCircularArcRoi.UserMidPoint };
                for (int i = 0; i < 3; i++)
                {
                    if ((pos - points[i]).Length < 15)  // 增加命中測試範圍
                    {
                        isDraggingDot = true;
                        draggingDotIndex = i;
                        
                        // 捕獲滑鼠以確保能接收到滑鼠釋放事件
                        if (canvas != null && !canvas.IsMouseCaptured)
                        {
                            canvas.CaptureMouse();
                        }
                        return;
                    }
                }
            }

            // 如果點擊空白區域且當前有已完成的 ROI，重置狀態以創建新的 ROI
            if (currentCircularArcRoi != null && currentCircularArcRoi.IsCompleted)
            {
                currentCircularArcRoi = new CircularArcRoiItem();
                clickCount = 0;
            }

            // 只有在 CircularArc ROI 模式下才允許創建新的 ROI
            if (!isCircularArcRoiMode)
            {
                return;
            }

            // 只有在沒有點擊到控制點時，才開始創建新的 ROI
            if (clickCount < 3)
            {
                switch (clickCount)
                {
                    case 0:
                        currentCircularArcRoi.StartPoint = pos;
                        break;
                    case 1:
                        currentCircularArcRoi.EndPoint = pos;
                        break;
                    case 2:
                        currentCircularArcRoi.UserMidPoint = pos;
                        break;
                }
                clickCount++;
                
                if (drawingService != null && mainCanvas != null)
                {
                    drawingService.DrawRois(mainCanvas, circularArcRois.ToList(), currentCircularArcRoi, true);
                }
                
                CircularArcRoiUpdated?.Invoke(currentCircularArcRoi);
            }
        }

        public void HandleMouseMove(Point pos, Canvas canvas)
        {
            var now = DateTime.Now;
            if ((now - lastUpdateTime).TotalMilliseconds < UPDATE_THROTTLE_MS)
            {
                return;
            }
            lastUpdateTime = now;

            if (isCircularArcRoiMode && clickCount == 3 && currentCircularArcRoi != null && !currentCircularArcRoi.IsCompleted)
            {
                currentCircularArcRoi.IsCompleted = true;
                currentCircularArcRoi.CalculateArcProperties();
                
                circularArcRois.Add(currentCircularArcRoi);
                
                CircularArcRoiCompleted?.Invoke(currentCircularArcRoi);
                
                if (drawingService != null && mainCanvas != null)
                {
                    drawingService.DrawRois(mainCanvas, circularArcRois.ToList(), currentCircularArcRoi, true);
                }
                return;
            }

            if (isDraggingDot && draggingDotIndex >= 0 && draggingDotIndex < 3 && currentCircularArcRoi != null)
            {
                switch (draggingDotIndex)
                {
                    case 0:
                        currentCircularArcRoi.StartPoint = pos;
                        break;
                    case 1:
                        currentCircularArcRoi.EndPoint = pos;
                        break;
                    case 2:
                        currentCircularArcRoi.UserMidPoint = pos;
                        break;
                }
                
                if (currentCircularArcRoi.IsCompleted)
                {
                    currentCircularArcRoi.CalculateArcProperties();
                }
                
                if (drawingService != null)
                {
                    drawingService.UpdateCircularArcVisual(currentCircularArcRoi);
                }
                
                CircularArcRoiUpdated?.Invoke(currentCircularArcRoi);
            }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            if (isDraggingDot)
            {
                if (draggingDotIndex >= 0 && draggingDotIndex < 3 && currentCircularArcRoi != null)
                {
                    switch (draggingDotIndex)
                    {
                        case 0:
                            currentCircularArcRoi.StartPoint = pos;
                            break;
                        case 1:
                            currentCircularArcRoi.EndPoint = pos;
                            break;
                        case 2:
                            currentCircularArcRoi.UserMidPoint = pos;
                            break;
                    }
                    
                    if (currentCircularArcRoi.IsCompleted)
                    {
                        currentCircularArcRoi.CalculateArcProperties();
                        CircularArcRoiUpdated?.Invoke(currentCircularArcRoi);
                    }
                }
                
                // 釋放滑鼠捕獲
                if (canvas != null && canvas.IsMouseCaptured)
                {
                    canvas.ReleaseMouseCapture();
                }
                
                isDraggingDot = false;
                draggingDotIndex = -1;
                
                // 如果不在 CircularArc ROI 模式下，設置 currentCircularArcRoi 為 null
                if (!isCircularArcRoiMode)
                {
                    currentCircularArcRoi = null;
                }
            }
        }

        public void RemoveCircularArcRoi(CircularArcRoiItem circularArcRoi, Canvas canvas)
        {
            if (circularArcRois.Contains(circularArcRoi))
            {
                circularArcRois.Remove(circularArcRoi);
                CircularArcRoiRemoved?.Invoke(circularArcRoi);
            }
        }

        public void RemoveAllCircularArcRois(Canvas canvas)
        {
            if (canvas != null)
            {
                var elementsToRemove = new List<UIElement>();
                foreach (var child in canvas.Children)
                {
                    if (child is System.Windows.Shapes.Path path && path.Tag is CircularArcRoiItem)
                    {
                        elementsToRemove.Add((UIElement)child);
                    }
                    else if (child is System.Windows.Shapes.Ellipse ellipse && ellipse.Tag is CircularArcRoiItem)
                    {
                        elementsToRemove.Add((UIElement)child);
                    }
                    else if (child is System.Windows.Controls.Border border && border.Tag is CircularArcRoiItem)
                    {
                        elementsToRemove.Add((UIElement)child);
                    }
                }
                
                foreach (var element in elementsToRemove)
                {
                    canvas.Children.Remove(element);
                }
            }
            
            circularArcRois.Clear();
            CircularArcRoisCleared?.Invoke();
        }

        public bool IsHitTest(Point pos)
        {
            foreach (var circularArcRoi in circularArcRois)
            {
                Point[] points = { circularArcRoi.StartPoint, circularArcRoi.EndPoint, circularArcRoi.UserMidPoint };
                for (int i = 0; i < 3; i++)
                {
                    if ((pos - points[i]).Length < 12)
                    {
                        return true;
                    }
                }
            }

            foreach (var circularArcRoi in circularArcRois)
            {
                if (IsPointNearCircularArc(pos, circularArcRoi))
                {
                    return true;
                }
            }

            if (isCircularArcRoiMode && clickCount > 0)
            {
                return true;
            }

            return false;
        }

        public CircularArcRoiItem GetHitCircularArcRoiItem(Point pos)
        {
            var hitItems = circularArcRois
                .Where(circularArcRoi =>
                {
                    Point[] points = { circularArcRoi.StartPoint, circularArcRoi.EndPoint, circularArcRoi.UserMidPoint };
                    for (int i = 0; i < 3; i++)
                    {
                        if ((pos - points[i]).Length < 15)
                        {
                            return true;
                        }
                    }
                    if (IsPointNearCircularArc(pos, circularArcRoi))
                        return true;
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        private bool IsPointNearCircularArc(Point pos, CircularArcRoiItem circularArcRoi)
        {
            if (IsPointNear(pos, circularArcRoi.StartPoint) ||
                IsPointNear(pos, circularArcRoi.EndPoint) ||
                IsPointNear(pos, circularArcRoi.UserMidPoint))
            {
                return true;
            }

            return IsPointNearCircularArcCurve(pos, circularArcRoi, 20);
        }

        private bool IsPointNear(Point p1, Point p2, double tolerance = 12)
        {
            return (p1 - p2).Length < tolerance;
        }

        private bool IsPointNearCircularArcCurve(Point point, CircularArcRoiItem circularArcRoi, double tolerance)
        {
            // 檢查點是否在圓弧附近
            double distanceToCenter = (point - circularArcRoi.CenterPoint).Length;
            double radiusDiff = Math.Abs(distanceToCenter - circularArcRoi.Radius);
            
            if (radiusDiff > tolerance) return false;
            
            // 檢查點是否在圓弧的角度範圍內
            double pointAngle = CalculateAngle(circularArcRoi.CenterPoint, point);
            double startAngle = circularArcRoi.StartAngle;
            double endAngle = circularArcRoi.EndAngle;
            
            // 標準化角度
            if (endAngle < startAngle) endAngle += 360;
            if (pointAngle < startAngle) pointAngle += 360;
            
            return pointAngle >= startAngle && pointAngle <= endAngle;
        }

        private double CalculateAngle(Point center, Point point)
        {
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            return angle < 0 ? angle + 360 : angle;
        }

        public void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        public void SetDrawingService(CircularArcRoiDrawingService drawingService)
        {
            this.drawingService = drawingService;
        }

        public void HandleRightMouseDown(Point pos, Canvas canvas)
        {
            if (isCircularArcRoiMode && currentCircularArcRoi != null && !currentCircularArcRoi.IsCompleted)
            {
                CancelCurrentCircularArcRoi(canvas);
            }
        }

        public void CancelCurrentCircularArcRoi(Canvas canvas)
        {
            if (currentCircularArcRoi != null && !currentCircularArcRoi.IsCompleted)
            {
                if (drawingService != null && mainCanvas != null)
                {
                    drawingService.ClearCircularArcRois(mainCanvas);
                }
                
                currentCircularArcRoi = new CircularArcRoiItem();
                clickCount = 0;
            }
        }

        public CircularArcRoiItem GetHitRoiItem(Point pos)
        {
            var hitItems = circularArcRois
                .Where(circularArcRoi =>
                {
                    Point[] points = { circularArcRoi.StartPoint, circularArcRoi.EndPoint, circularArcRoi.UserMidPoint };
                    for (int i = 0; i < 3; i++)
                    {
                        if ((pos - points[i]).Length < 15)
                        {
                            return true;
                        }
                    }
                    if (IsPointNearCircularArc(pos, circularArcRoi))
                        return true;
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }
    }
} 