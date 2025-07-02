using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HyImageShow.ImageShowWPF.Models;

namespace HyImageShow.ImageShowWPF.Services
{
    public class RulerService : BaseRoiService<RulerItem, RulerDrawingService>
    {
        private readonly ObservableCollection<RulerItem> rulerItems;
        private RulerDrawingService rulerDrawingService;
        private RulerItem currentRuler;
        private bool isRulerMode;
        private Canvas mainCanvas;

        public RulerService()
        {
            rulerItems = new ObservableCollection<RulerItem>();
            currentRuler = null;
            isRulerMode = false;
            ShowLabels = true; // 預設顯示標籤
        }

        public ObservableCollection<RulerItem> RulerItems => rulerItems;

        /// <summary>
        /// 是否顯示標籤
        /// </summary>
        public bool ShowLabels { get; set; }

        public void SetDrawingService(RulerDrawingService service)
        {
            rulerDrawingService = service;
        }

        public bool IsRulerMode
        {
            get => isRulerMode;
            set
            {
                if (isRulerMode != value)
                {
                    isRulerMode = value;
                    if (!isRulerMode)
                    {
                        CancelRuler();
                    }
                }
            }
        }

        public void EnableRulerMode()
        {
            IsRulerMode = true;
        }

        public void DisableRulerMode()
        {
            IsRulerMode = false;
        }

        public void StartRuler(Point startPoint)
        {
            if (!IsRulerMode) return;
            currentRuler = new RulerItem
            {
                Point1 = startPoint,
                Point2 = startPoint,
                IsCompleted = false
            };
            ZIndexManager.Instance.Register(currentRuler);
        }

        public void UpdateRuler(Point currentPoint)
        {
            if (currentRuler == null || !IsRulerMode) return;
            currentRuler.Point2 = currentPoint;
            RulerUpdated?.Invoke(currentRuler);
            if (rulerDrawingService != null && mainCanvas != null)
            {
                rulerDrawingService.DrawRois(mainCanvas, rulerItems.ToList(), currentRuler, ShowLabels);
            }
        }

        public void CompleteRuler(Point endPoint)
        {
            if (currentRuler == null || !IsRulerMode) return;
            currentRuler.Point2 = endPoint;
            currentRuler.IsCompleted = true;
            rulerItems.Add(currentRuler);
            if (rulerDrawingService != null && mainCanvas != null)
            {
                rulerDrawingService.DrawRois(mainCanvas, rulerItems.ToList(), null, ShowLabels);
            }
            RulerCompleted?.Invoke(currentRuler);
            currentRuler = null;
        }

        public void CancelRuler()
        {
            if (currentRuler != null)
            {
                RulerUpdated?.Invoke(currentRuler);
                
                // 調用繪製服務清除當前量尺
                if (rulerDrawingService != null && mainCanvas != null)
                {
                    rulerDrawingService.RemoveCurrentRuler(mainCanvas);
                }
                
                currentRuler = null;
            }
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            var hitRuler = GetHitRulerItem(pos);
            if (hitRuler != null)
            {
                if (IsPointNear(pos, hitRuler.Point1, 12))
                {
                    hitRuler.IsDraggingPoint1 = true;
                    hitRuler.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(hitRuler);
                }
                else if (IsPointNear(pos, hitRuler.Point2, 12))
                {
                    hitRuler.IsDraggingPoint2 = true;
                    hitRuler.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(hitRuler);
                }
                else if (IsPointNearLine(pos, hitRuler.Point1, hitRuler.Point2, 8))
                {
                    hitRuler.IsDraggingRuler = true;
                    hitRuler.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(hitRuler);
                }
                AnimateScale();
                return;
            }
            if (!IsRulerMode) return;
            StartRuler(pos);
        }

        public void HandleMouseMove(Point pos, Canvas canvas)
        {
            if (IsRulerMode && currentRuler != null)
            {
                UpdateRuler(pos);
            }
            foreach (var rulerItem in rulerItems)
            {
                if (rulerItem.IsDraggingPoint1)
                {
                    rulerItem.Point1 = pos;
                    rulerDrawingService?.UpdateRulerVisual(rulerItem);
                    RulerUpdated?.Invoke(rulerItem);
                }
                else if (rulerItem.IsDraggingPoint2)
                {
                    rulerItem.Point2 = pos;
                    rulerDrawingService?.UpdateRulerVisual(rulerItem);
                    RulerUpdated?.Invoke(rulerItem);
                }
                else if (rulerItem.IsDraggingRuler)
                {
                    Vector delta = pos - rulerItem.LastDragPos;
                    rulerItem.Point1 = (Point)(rulerItem.Point1 + delta);
                    rulerItem.Point2 = (Point)(rulerItem.Point2 + delta);
                    rulerItem.LastDragPos = pos;
                    rulerDrawingService?.UpdateRulerVisual(rulerItem);
                    RulerUpdated?.Invoke(rulerItem);
                }
            }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            // 完成繪製新量尺（只有在量尺模式下）
            if (IsRulerMode && currentRuler != null)
            {
                // 檢查起點和終點的距離，只有當距離足夠時才完成量尺
                double distance = Math.Sqrt(Math.Pow(currentRuler.Point2.X - currentRuler.Point1.X, 2) + Math.Pow(currentRuler.Point2.Y - currentRuler.Point1.Y, 2));
                
                if (distance >= 5.0) // 最小距離閾值
                {
                    CompleteRuler(pos);
                }
                else
                {
                    CancelRuler();
                }
            }

            // 停止拖曳（無論模式是否開啟）
            foreach (var rulerItem in rulerItems)
            {
                rulerItem.IsDraggingPoint1 = false;
                rulerItem.IsDraggingPoint2 = false;
                rulerItem.IsDraggingRuler = false;
            }

            // 更新縮放動畫
            AnimateScale();
        }

        public void HandleRightMouseDown(Canvas canvas)
        {
            if (!IsRulerMode) return;

            // 右鍵點擊取消當前繪製
            if (currentRuler != null)
            {
                CancelRuler();
            }
        }

        public bool IsHitTest(Point pos)
        {
            // 總是檢查現有量尺的點擊
            foreach (var rulerItem in rulerItems)
            {
                if (IsPointNear(pos, rulerItem.Point1, 12) || IsPointNear(pos, rulerItem.Point2, 12))
                {
                    return true;
                }
                else if (IsPointNearLine(pos, rulerItem.Point1, rulerItem.Point2, 8))
                {
                    return true;
                }
            }
            // 只有在量尺模式下才允許開始新量尺
            return IsRulerMode;
        }

        private RulerItem GetHitRulerItem(Point pos)
        {
            var hitItems = rulerItems
                .Where(rulerItem =>
                {
                    if (IsPointNear(pos, rulerItem.Point1, 12) || IsPointNear(pos, rulerItem.Point2, 12) ||
                        IsPointNearLine(pos, rulerItem.Point1, rulerItem.Point2, 8))
                        return true;
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        private bool IsPointNear(Point a, Point b, double tol = 12)
        {
            return (a - b).Length < tol;
        }

        private bool IsPointNearLine(Point p, Point a, Point b, double tol)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            if (dx == 0 && dy == 0) return false;
            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));
            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;
            double dist = Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
            return dist < tol;
        }

        public event Action<RulerItem> RulerCompleted;
        public event Action<RulerItem> RulerUpdated;
        public event Action<RulerItem> RulerRemoved;
        public event Action RulersCleared;

        public void AnimateScale()
        {
            foreach (var rulerItem in rulerItems)
            {
                double scale1 = rulerItem.IsDraggingPoint1 ? 1.4 : 1.0;
                double scale2 = rulerItem.IsDraggingPoint2 ? 1.4 : 1.0;
                var anim1 = new DoubleAnimation(scale1, TimeSpan.FromMilliseconds(120));
                var anim2 = new DoubleAnimation(scale2, TimeSpan.FromMilliseconds(120));
                rulerItem.Point1Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim1);
                rulerItem.Point1Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim1);
                rulerItem.Point2Scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim2);
                rulerItem.Point2Scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim2);
            }
        }

        public void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        public RulerItem GetHitRoiItem(Point pos)
        {
            foreach (var rulerItem in rulerItems)
            {
                if (IsPointNear(pos, rulerItem.Point1, 12) || IsPointNear(pos, rulerItem.Point2, 12) ||
                    IsPointNearLine(pos, rulerItem.Point1, rulerItem.Point2, 8))
                {
                    return rulerItem;
                }
            }
            return null;
        }
    }
} 