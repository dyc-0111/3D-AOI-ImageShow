using HyImageShow.ImageShowWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 貝塞爾弧線ROI服務實現
    /// </summary>
    public class BezierArcRoiService : BaseRoiService<BezierArcRoiItem, BezierArcRoiDrawingService>
    {
        public ObservableCollection<BezierArcRoiItem> BezierArcRois { get; }
        public BezierArcRoiItem CurrentBezierArcRoi { get; private set; }
        private bool isBezierArcRoiMode;
        private int clickCount;
        private bool isDraggingDot;
        private int draggingDotIndex;
        private Canvas mainCanvas;
        private BezierArcRoiDrawingService drawingService;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private const int UPDATE_THROTTLE_MS = 16; // 約60FPS
        public bool AllowMultiDrag { get; set; } = false; // 預設單一拖曳

        public BezierArcRoiService()
        {
            BezierArcRois = new ObservableCollection<BezierArcRoiItem>();
            isBezierArcRoiMode = false;
            clickCount = 0;
            isDraggingDot = false;
            draggingDotIndex = -1;
        }

        public bool IsBezierArcRoiMode => isBezierArcRoiMode;
        public int ClickCount => clickCount;

        public event Action<BezierArcRoiItem> BezierArcRoiCompleted;
        public event Action<BezierArcRoiItem> BezierArcRoiUpdated;
        public event Action<BezierArcRoiItem> BezierArcRoiRemoved;
        public event Action BezierArcRoisCleared;

        public void EnableBezierArcRoiMode()
        {
            isBezierArcRoiMode = true;
            clickCount = 0;
            CurrentBezierArcRoi = null;
        }

        public void DisableBezierArcRoiMode()
        {
            isBezierArcRoiMode = false;
            clickCount = 0;
            CurrentBezierArcRoi = null;
            isDraggingDot = false;
            draggingDotIndex = -1;
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            var hitRoi = GetHitBezierArcRoiItem(pos);
            if (hitRoi != null && hitRoi.IsCompleted)
            {
                Point[] points = { hitRoi.StartPoint, hitRoi.EndPoint, hitRoi.MiddlePoint };
                for (int i = 0; i < 3; i++)
                {
                    if ((pos - points[i]).Length < 15)
                    {
                        isDraggingDot = true;
                        draggingDotIndex = i;
                        CurrentBezierArcRoi = hitRoi;
                        ZIndexManager.Instance.BringToFront(CurrentBezierArcRoi);
                        if (canvas != null && !canvas.IsMouseCaptured)
                        {
                            canvas.CaptureMouse();
                        }
                        return;
                    }
                }
                return;
            }
            if (CurrentBezierArcRoi != null && CurrentBezierArcRoi.IsCompleted)
            {
                Point[] points = { CurrentBezierArcRoi.StartPoint, CurrentBezierArcRoi.EndPoint, CurrentBezierArcRoi.MiddlePoint };
                for (int i = 0; i < 3; i++)
                {
                    if ((pos - points[i]).Length < 15)
                    {
                        isDraggingDot = true;
                        draggingDotIndex = i;
                        if (canvas != null && !canvas.IsMouseCaptured)
                        {
                            canvas.CaptureMouse();
                        }
                        return;
                    }
                }
            }
            if (CurrentBezierArcRoi != null && CurrentBezierArcRoi.IsCompleted)
            {
                CurrentBezierArcRoi = null;
                clickCount = 0;
            }
            if (!isBezierArcRoiMode) return;
            if (clickCount == 0)
            {
                CurrentBezierArcRoi = new BezierArcRoiItem();
                ZIndexManager.Instance.Register(CurrentBezierArcRoi);
                CurrentBezierArcRoi.StartPoint = pos;
                clickCount = 1;
            }
            else if (clickCount == 1)
            {
                CurrentBezierArcRoi.EndPoint = pos;
                clickCount = 2;
            }
            else if (clickCount == 2)
            {
                CurrentBezierArcRoi.MiddlePoint = pos;
                clickCount = 3;
                CurrentBezierArcRoi.IsCompleted = true;
                CurrentBezierArcRoi.CalculateArcLength();
                BezierArcRois.Add(CurrentBezierArcRoi);
                BezierArcRoiCompleted?.Invoke(CurrentBezierArcRoi);
                CurrentBezierArcRoi = null;
                clickCount = 0;
            }
            if (drawingService != null && mainCanvas != null)
            {
                drawingService.DrawRois(mainCanvas, BezierArcRois, CurrentBezierArcRoi, true);
            }
            BezierArcRoiUpdated?.Invoke(CurrentBezierArcRoi);
        }

        public void HandleMouseMove(Point pos, Canvas canvas)
        {
            var now = DateTime.Now;
            if ((now - lastUpdateTime).TotalMilliseconds < UPDATE_THROTTLE_MS)
            {
                return;
            }
            lastUpdateTime = now;

            // 取得Canvas邊界
            double minX = 0;
            double minY = 0;
            double maxX = canvas.ActualWidth;
            double maxY = canvas.ActualHeight;

            if (isDraggingDot && draggingDotIndex >= 0 && draggingDotIndex < 3 && CurrentBezierArcRoi != null)
            {
                // 限制鼠標位置在Canvas內
                Point clampedPos = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampPoint(pos, minX, minY, maxX, maxY);
                
                switch (draggingDotIndex)
                {
                    case 0:
                        CurrentBezierArcRoi.StartPoint = clampedPos;
                        break;
                    case 1:
                        CurrentBezierArcRoi.EndPoint = clampedPos;
                        break;
                    case 2:
                        CurrentBezierArcRoi.MiddlePoint = clampedPos;
                        break;
                }
                if (CurrentBezierArcRoi.IsCompleted)
                {
                    CurrentBezierArcRoi.CalculateArcLength();
                }
                if (drawingService != null)
                {
                    drawingService.UpdateBezierArcVisual(CurrentBezierArcRoi);
                }
                BezierArcRoiUpdated?.Invoke(CurrentBezierArcRoi);
               }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            if (isDraggingDot)
            {
                if (draggingDotIndex >= 0 && draggingDotIndex < 3 && CurrentBezierArcRoi != null)
                {
                    // 取得Canvas邊界
                    double minX = 0;
                    double minY = 0;
                    double maxX = canvas.ActualWidth;
                    double maxY = canvas.ActualHeight;
                    
                    // 限制鼠標位置在Canvas內
                    Point clampedPos = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampPoint(pos, minX, minY, maxX, maxY);
                    
                    switch (draggingDotIndex)
                    {
                        case 0:
                            CurrentBezierArcRoi.StartPoint = clampedPos;
                            break;
                        case 1:
                            CurrentBezierArcRoi.EndPoint = clampedPos;
                            break;
                        case 2:
                            CurrentBezierArcRoi.MiddlePoint = clampedPos;
                            break;
                    }
                    if (CurrentBezierArcRoi.IsCompleted)
                    {
                        CurrentBezierArcRoi.CalculateArcLength();
                        BezierArcRoiUpdated?.Invoke(CurrentBezierArcRoi);
                    }
                }
                if (canvas != null && canvas.IsMouseCaptured)
                {
                    canvas.ReleaseMouseCapture();
                }
                isDraggingDot = false;
                draggingDotIndex = -1;
            }
        }

        public IEnumerable<BezierArcRoiItem> GetAllBezierArcRois()
        {
            return BezierArcRois;
        }

        public BezierArcRoiItem GetHitBezierArcRoiItem(Point pos)
        {
            var hitItems = GetAllBezierArcRois()
                .Where(bezierArcRoi =>
                {
                    Point[] points = { bezierArcRoi.StartPoint, bezierArcRoi.EndPoint, bezierArcRoi.MiddlePoint };
                    for (int i = 0; i < 3; i++)
                    {
                        if ((pos - points[i]).Length < 15)
                        {
                            return true;
                        }
                    }
                    if (IsPointNearBezierArc(pos, bezierArcRoi))
                        return true;
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        public bool IsHitTest(Point pos)
        {
            foreach (var bezierArcRoi in GetAllBezierArcRois())
            {
                Point[] points = { bezierArcRoi.StartPoint, bezierArcRoi.EndPoint, bezierArcRoi.MiddlePoint };
                for (int i = 0; i < 3; i++)
                {
                    if ((pos - points[i]).Length < 12)
                    {
                        return true;
                    }
                }
            }

            foreach (var bezierArcRoi in GetAllBezierArcRois())
            {
                if (IsPointNearBezierArc(pos, bezierArcRoi))
                {
                    return true;
                }
            }

            if (isBezierArcRoiMode && clickCount > 0)
            {
                return true;
            }

            return false;
        }

        public BezierArcRoiItem GetHitRoiItem(Point pos)
        {
            // 先收集所有命中的 ROI
            var hitItems = GetAllBezierArcRois()
                .Where(bezierArcRoi =>
                {
                    Point[] points = { bezierArcRoi.StartPoint, bezierArcRoi.EndPoint, bezierArcRoi.MiddlePoint };
                    for (int i = 0; i < 3; i++)
                    {
                        if ((pos - points[i]).Length < 15)
                        {
                            return true;
                        }
                    }
                    if (IsPointNearBezierArc(pos, bezierArcRoi))
                        return true;
                    return false;
                })
                .ToList();
            // 回傳 Z-Index 最大的
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        private bool IsPointNearBezierArc(Point pos, BezierArcRoiItem bezierArcRoi)
        {
            if (IsPointNear(pos, bezierArcRoi.StartPoint) ||
                IsPointNear(pos, bezierArcRoi.EndPoint) ||
                IsPointNear(pos, bezierArcRoi.MiddlePoint))
            {
                return true;
            }

            return IsPointNearBezierCurve(pos, bezierArcRoi.StartPoint, bezierArcRoi.MiddlePoint, bezierArcRoi.EndPoint, 20);
        }

        private bool IsPointNear(Point p1, Point p2, double tolerance = 12)
        {
            return (p1 - p2).Length < tolerance;
        }

        private bool IsPointNearBezierCurve(Point point, Point p0, Point p1, Point p2, double tolerance)
        {
            for (double t = 0; t <= 1; t += 0.1)
            {
                Point curvePoint = GetQuadraticBezierPoint(p0, p1, p2, t);
                if ((point - curvePoint).Length < tolerance)
                {
                    return true;
                }
            }
            return false;
        }

        private Point GetQuadraticBezierPoint(Point p0, Point p1, Point p2, double t)
        {
            double x = (1 - t) * (1 - t) * p0.X + 2 * (1 - t) * t * p1.X + t * t * p2.X;
            double y = (1 - t) * (1 - t) * p0.Y + 2 * (1 - t) * t * p1.Y + t * t * p2.Y;
            return new Point(x, y);
        }

        public void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        public void SetDrawingService(BezierArcRoiDrawingService Service)
        {
            drawingService = Service;
        }

        public void HandleRightMouseDown(Point pos, Canvas canvas)
        {
            if (isBezierArcRoiMode && CurrentBezierArcRoi != null && !CurrentBezierArcRoi.IsCompleted)
            {
                CancelCurrentBezierArcRoi(canvas);
            }
        }

        public void CancelCurrentBezierArcRoi(Canvas canvas)
        {
            if (CurrentBezierArcRoi != null && !CurrentBezierArcRoi.IsCompleted)
            {
                if (drawingService != null && mainCanvas != null)
                {
                    drawingService.ClearRois(mainCanvas, GetAllBezierArcRois().ToList());
                }

                CurrentBezierArcRoi = null;
                clickCount = 0;
            }
        }

        public void RemoveBezierArcRoi(BezierArcRoiItem bezierArcRoi)
        {
            if (CurrentBezierArcRoi == bezierArcRoi)
                CurrentBezierArcRoi = null;
            BezierArcRois.Remove(bezierArcRoi);
            BezierArcRoiRemoved?.Invoke(bezierArcRoi);
        }

        public override void RemoveRoi(BezierArcRoiItem bezierArcRoi, Canvas canvas)
        {
            if (bezierArcRoi == null || canvas == null) return;
            
            // 如果是當前繪製的貝茲弧，清除當前狀態
            if (CurrentBezierArcRoi == bezierArcRoi)
            {
                CurrentBezierArcRoi = null;
                clickCount = 0;
            }
            
            // 從Canvas清除視覺元素
            if (drawingService != null)
            {
                drawingService.ClearRois(canvas, new List<BezierArcRoiItem> { bezierArcRoi });
            }
            
            // 從集合中移除
            if (BezierArcRois.Remove(bezierArcRoi))
            {
                // 觸發移除事件
                BezierArcRoiRemoved?.Invoke(bezierArcRoi);
            }
        }

        public void RemoveAllBezierArcRois()
        {
            CurrentBezierArcRoi = null;
            BezierArcRois.Clear();
            BezierArcRoisCleared?.Invoke();
        }
    }
}