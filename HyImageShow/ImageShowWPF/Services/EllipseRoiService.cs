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
    /// 圓形ROI服務實現
    /// </summary>
    public class EllipseRoiService : BaseRoiService<EllipseRoiItem, EllipseRoiDrawingService>
    {
        private ObservableCollection<EllipseRoiItem> ellipseRois;
        private EllipseRoiItem currentEllipseRoi;
        private bool isEllipseRoiMode;
        private Canvas mainCanvas;
        private EllipseRoiDrawingService drawingService;
        public bool AllowMultiDrag { get; set; } = false; // 預設單一拖曳

        public EllipseRoiService()
        {
            ellipseRois = new ObservableCollection<EllipseRoiItem>();
            isEllipseRoiMode = false;
        }

        public ObservableCollection<EllipseRoiItem> EllipseRois => ellipseRois;

        public EllipseRoiItem CurrentEllipseRoi => currentEllipseRoi;

        public bool IsEllipseRoiMode => isEllipseRoiMode;

        public event Action<EllipseRoiItem> EllipseRoiCompleted;
        public event Action<EllipseRoiItem> EllipseRoiUpdated;
        public event Action<EllipseRoiItem> EllipseRoiRemoved;
        public event Action EllipseRoisCleared;

        public void EnableEllipseRoiMode()
        {
            isEllipseRoiMode = true;
        }

        public void DisableEllipseRoiMode()
        {
            isEllipseRoiMode = false;
            currentEllipseRoi = null;
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            // 先清空所有 ROI 拖曳狀態（單選模式）
            if (!AllowMultiDrag || !isShift)
            {
                foreach (var roi in ellipseRois)
                {
                    roi.IsDraggingCenter = false;
                    roi.IsDraggingRadius = false;
                    roi.Selected = false;
                }
            }
            // 只找 Z-Index 最大的命中 ROI
            var hitRoi = GetHitEllipseRoiItem(pos);
            if (hitRoi != null)
            {
                if (AllowMultiDrag && isShift)
                {
                    hitRoi.Selected = !hitRoi.Selected; // Shift+點選切換選取
                }
                else
                {
                    hitRoi.Selected = true;
                }
                if (IsPointNear(pos, hitRoi.Center))
                {
                    currentEllipseRoi = hitRoi;
                    currentEllipseRoi.IsDraggingCenter = true;
                    currentEllipseRoi.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(currentEllipseRoi);
                }
                else if (Math.Abs((pos - hitRoi.Center).Length - hitRoi.Radius) < 12)
                {
                    currentEllipseRoi = hitRoi;
                    currentEllipseRoi.IsDraggingRadius = true;
                    currentEllipseRoi.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(currentEllipseRoi);
                }
                else if ((pos - hitRoi.Center).Length <= hitRoi.Radius)
                {
                    currentEllipseRoi = hitRoi;
                    currentEllipseRoi.IsDraggingCenter = true;
                    currentEllipseRoi.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(currentEllipseRoi);
                }
                return; // 只處理這一個 ROI
            }
            if (!isEllipseRoiMode) return;
            currentEllipseRoi = new EllipseRoiItem
            {
                CenterPoint = pos,
                Radius = 0,
                IsCompleted = false
            };
            ellipseRois.Add(currentEllipseRoi);
            ZIndexManager.Instance.Register(currentEllipseRoi);
            if (drawingService != null)
            {
                drawingService.DrawSingleRoi(currentEllipseRoi, canvas, true);
            }
            EllipseRoiUpdated?.Invoke(currentEllipseRoi);
        }

        public void HandleMouseMove(Point pos, Canvas canvas)
        {
            HandleMouseMove(pos, canvas, 0, 0, canvas.ActualWidth, canvas.ActualHeight);
        }

        public void HandleMouseMove(Point pos, Canvas canvas, double minX, double minY, double maxX, double maxY)
        {
            if (currentEllipseRoi != null && !currentEllipseRoi.IsCompleted)
            {
                // 更新正在繪製的圓形ROI半徑，並限制不超出Canvas
                var clamp = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampCircleStrict(currentEllipseRoi.Center, (pos - currentEllipseRoi.Center).Length, minX, minY, maxX, maxY);
                currentEllipseRoi.Radius = clamp.radius;
                // 修正：重新繪製所有橢圓ROI，包含當前正在繪製的橢圓
                if (drawingService != null)
                {
                    drawingService.DrawRois(canvas, ellipseRois.ToList(), currentEllipseRoi, false);
                }
                EllipseRoiUpdated?.Invoke(currentEllipseRoi);
            }
            else if (currentEllipseRoi != null && currentEllipseRoi.IsDraggingCenter)
            {
                // 拖曳圓心，限制圓心不超出Canvas（考慮半徑）
                Vector delta = pos - currentEllipseRoi.LastDragPos;
                Point newCenter = (Point)(currentEllipseRoi.Center + delta);
                double r = currentEllipseRoi.Radius;
                var clamp = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampCircleStrict(newCenter, currentEllipseRoi.Radius, minX, minY, maxX, maxY);
                currentEllipseRoi.CenterPoint = clamp.center;
                currentEllipseRoi.CenterPoint = clamp.center;
                currentEllipseRoi.LastDragPos = pos;
                // 新增：即時更新視覺元素
                if (drawingService != null)
                {
                    drawingService.UpdateEllipseVisual(currentEllipseRoi);
                }
                EllipseRoiUpdated?.Invoke(currentEllipseRoi);
            }
            else if (currentEllipseRoi != null && currentEllipseRoi.IsDraggingRadius)
            {
                // 拖曳半徑，限制半徑不超出Canvas，並同時修正center（避免半徑變大時圓心已經貼邊）
                var clamp = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampCircleStrict(currentEllipseRoi.Center, (pos - currentEllipseRoi.Center).Length, minX, minY, maxX, maxY);
                currentEllipseRoi.CenterPoint = clamp.center;
                currentEllipseRoi.Radius = clamp.radius;
                // 新增：即時更新視覺元素
                if (drawingService != null)
                {
                    drawingService.UpdateEllipseVisual(currentEllipseRoi);
                }
                EllipseRoiUpdated?.Invoke(currentEllipseRoi);
            }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            if (currentEllipseRoi != null && !currentEllipseRoi.IsCompleted)
            {
                // 完成圓形ROI的創建
                currentEllipseRoi.Radius = (pos - currentEllipseRoi.Center).Length;
                currentEllipseRoi.IsCompleted = true;
                
                // 新增：重新繪製所有橢圓ROI，顯示控制點和標籤
                if (drawingService != null)
                {
                    drawingService.DrawRois(canvas, ellipseRois.ToList(), null, true);
                }
                
                // 觸發完成事件
                EllipseRoiCompleted?.Invoke(currentEllipseRoi);

                // 重置當前圓形ROI
                currentEllipseRoi = null;
            }
            else if (currentEllipseRoi != null)
            {
                // 結束拖曳狀態
                currentEllipseRoi.IsDraggingCenter = false;
                currentEllipseRoi.IsDraggingRadius = false;
            }
        }

        public void HandleRightMouseDown(Canvas canvas)
        {
            // 取消當前正在繪製的圓形ROI
            CancelCurrentEllipseRoi(canvas);
        }

        public bool IsHitTest(Point pos)
        {
            // 檢查是否點擊了任何圓形ROI
            foreach (var ellipseRoi in ellipseRois)
            {
                // 檢查是否點擊中心點
                if (IsPointNear(pos, ellipseRoi.Center))
                {
                    return true;
                }
                // 檢查是否點擊半徑點
                else if (Math.Abs((pos - ellipseRoi.Center).Length - ellipseRoi.Radius) < 12)
                {
                    return true;
                }
                // 檢查是否點擊圓內
                else if ((pos - ellipseRoi.Center).Length <= ellipseRoi.Radius)
                {
                    return true;
                }
            }
            return false;
        }

        public void CancelCurrentEllipseRoi(Canvas canvas)
        {
            if (currentEllipseRoi != null && !currentEllipseRoi.IsCompleted)
            {
                // 移除當前正在繪製的圓形ROI
                ellipseRois.Remove(currentEllipseRoi);
                currentEllipseRoi = null;
                
                // 清除視覺元素
                if (drawingService != null)
                {
                    drawingService.ClearRois(canvas, ellipseRois);
                }
            }
        }

        public void ClearEllipseRois(Canvas canvas)
        {
            // 清除所有圓形ROI
            ellipseRois.Clear();
            currentEllipseRoi = null;
            
            // 清除視覺元素
            if (drawingService != null)
            {
                drawingService.ClearRois(canvas, ellipseRois);
            }
            
            EllipseRoisCleared?.Invoke();
        }

        public void RemoveAllEllipseRois(Canvas canvas)
        {
            // 先清除所有UI元素
            foreach (var roi in ellipseRois)
            {
                // 安全移除UI元素
                if (roi.Ellipse != null)
                {
                    if (canvas.Children.Contains(roi.Ellipse))
                        canvas.Children.Remove(roi.Ellipse);
                    roi.Ellipse = null;
                }
                if (roi.CenterDot != null)
                {
                    if (canvas.Children.Contains(roi.CenterDot))
                        canvas.Children.Remove(roi.CenterDot);
                    roi.CenterDot = null;
                }
                if (roi.RadiusDot != null)
                {
                    if (canvas.Children.Contains(roi.RadiusDot))
                        canvas.Children.Remove(roi.RadiusDot);
                    roi.RadiusDot = null;
                }
                if (roi.LabelBorder != null)
                {
                    if (canvas.Children.Contains(roi.LabelBorder))
                        canvas.Children.Remove(roi.LabelBorder);
                    roi.LabelBorder = null;
                }
            }
            
            // 清除列表
            ellipseRois.Clear();
            currentEllipseRoi = null;
            EllipseRoisCleared?.Invoke();
        }

        public void RemoveEllipseRoi(EllipseRoiItem ellipseRoi, Canvas canvas)
        {
            if (ellipseRois.Contains(ellipseRoi))
            {
                // 安全移除UI元素
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
                
                ellipseRois.Remove(ellipseRoi);
                EllipseRoiRemoved?.Invoke(ellipseRoi);
            }
        }

        public override void RemoveRoi(EllipseRoiItem ellipseRoi, Canvas canvas)
        {
            if (ellipseRois.Contains(ellipseRoi))
            {
                // 安全移除UI元素
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

                ellipseRois.Remove(ellipseRoi);
                EllipseRoiRemoved?.Invoke(ellipseRoi);
            }
        }

        public void UpdateVisualElements(Canvas canvas)
        {
            if (drawingService != null)
            {
                drawingService.UpdateVisualElements(canvas, ellipseRois.ToList(), currentEllipseRoi);
            }
        }

        public void AddLabels(Canvas canvas)
        {
            if (drawingService != null)
            {
                drawingService.AddLabels(canvas, ellipseRois.ToList());
            }
        }

        public void StartAnimationScaling()
        {
            if (drawingService != null)
            {
                drawingService.StartAnimationScaling(ellipseRois.ToList());
            }
        }

        public void StopAnimationScaling()
        {
            if (drawingService != null)
            {
                drawingService.StopAnimationScaling(ellipseRois.ToList());
            }
        }

        public override void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }
        /// <summary>
        /// 設置繪製服務引用
        /// </summary>
        public void SetDrawingService(EllipseRoiDrawingService service)
        {
            drawingService = service;
        }

        /// <summary>
        /// 檢查點是否接近指定位置
        /// </summary>
        private bool IsPointNear(Point a, Point b, double tolerance = 12)
        {
            return (a - b).Length < tolerance;
        }

        public EllipseRoiItem GetHitEllipseRoiItem(Point pos)
        {
            var hitItems = ellipseRois
                .Where(ellipseRoi =>
                {
                    if (IsPointNear(pos, ellipseRoi.Center))
                        return true;
                    else if (Math.Abs((pos - ellipseRoi.Center).Length - ellipseRoi.Radius) < 12)
                        return true;
                    else if ((pos - ellipseRoi.Center).Length <= ellipseRoi.Radius)
                        return true;
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        public EllipseRoiItem GetHitRoiItem(Point pos)
        {
            foreach (var ellipseRoi in ellipseRois)
            {
                if (IsPointNear(pos, ellipseRoi.Center))
                    return ellipseRoi;
                else if (Math.Abs((pos - ellipseRoi.Center).Length - ellipseRoi.Radius) < 12)
                    return ellipseRoi;
                else if ((pos - ellipseRoi.Center).Length <= ellipseRoi.Radius)
                    return ellipseRoi;
            }
            return null;
        }

    }
} 