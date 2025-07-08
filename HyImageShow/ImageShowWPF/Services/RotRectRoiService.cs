using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using HyImageShow.ImageShowWPF.Models;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 矩形ROI服務實現類
    /// </summary>
    public class RotRectRoiService : BaseRoiService<RectRoiItem, RotRectRoiDrawingService>
    {
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;
        private bool isDrawingRect = false;
        private Point rectStartPoint;
        private RectRoiItem rectPreviewItem = null;
        private RotRectRoiDrawingService drawingService;
        public bool AllowMultiDrag { get; set; } = false; // 預設單一拖曳

        public RotRectRoiService()
        {
            // 初始化樣式字典，與主視圖保持一致
            styleDict = new Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)>
            {
                {"rotRect", (Color.FromRgb(33, 150, 243), Color.FromRgb(25, 118, 210), Color.FromArgb(220, 187, 222, 251), Color.FromRgb(13, 71, 161))}, // 藍
            };

            RotRectRois = new ObservableCollection<RectRoiItem>();
            ShowLabels = true; // 預設顯示標籤
        }

        /// <summary>
        /// 是否顯示標籤
        /// </summary>
        public bool ShowLabels { get; set; }

        public ObservableCollection<RectRoiItem> RotRectRois { get; }

        public RectRoiItem CurrentRotRectRoi { get; private set; }

        public bool IsDrawingRotRectRoi { get; private set; }

        public bool IsRotRectRoiMode { get; private set; }

        public int RotRectRoiClickCount { get; private set; }

        public event Action<RectRoiItem> RotRectRoiCompleted;
        public event Action<RectRoiItem> RotRectRoiUpdated;
        public event Action<RectRoiItem> RotRectRoiRemoved;
        public event Action RotRectRoisCleared;

        /// <summary>
        /// 設置 MainCanvas 引用
        /// </summary>
        public override void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        /// <summary>
        /// 設置繪製服務引用
        /// </summary>
        public void SetDrawingService(RotRectRoiDrawingService service)
        {
            drawingService = service;
        }

        public void EnableRotRectRoiMode()
        {
            IsRotRectRoiMode = true;
        }

        public void DisableRotRectRoiMode()
        {
            IsRotRectRoiMode = false;
            IsDrawingRotRectRoi = false;
            CurrentRotRectRoi = null;
            RotRectRoiClickCount = 0;
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            // 先清空所有 ROI 拖曳狀態（單選模式）
            if (!AllowMultiDrag || !isShift)
            {
                foreach (var rotRectItem in RotRectRois)
                {
                    rotRectItem.IsDraggingCenter = false;
                    rotRectItem.IsDraggingCorner = false;
                    rotRectItem.IsDraggingRotate = false;
                    rotRectItem.DraggingCornerIndex = -1;
                    rotRectItem.Selected = false;
                }
            }
            // 只找 Z-Index 最大的命中 ROI
            var hitRotRectItem = GetHitRotRectRoiItem(pos);
            if (hitRotRectItem != null)
            {
                if (AllowMultiDrag && isShift)
                {
                    hitRotRectItem.Selected = !hitRotRectItem.Selected;
                }
                else
                {
                    hitRotRectItem.Selected = true;
                }
                if (IsPointNearRotate(pos, hitRotRectItem))
                {
                    hitRotRectItem.IsDraggingRotate = true;
                    hitRotRectItem.LastDragPos = pos;
                    hitRotRectItem.IsDraggingCenter = false;
                    hitRotRectItem.IsDraggingCorner = false;
                    hitRotRectItem.DraggingCornerIndex = -1;
                    ZIndexManager.Instance.BringToFront(hitRotRectItem);
                }
                else if (IsPointNear(pos, hitRotRectItem.Center, 12))
                {
                    hitRotRectItem.IsDraggingCenter = true;
                    hitRotRectItem.LastDragPos = pos;
                    hitRotRectItem.IsDraggingRotate = false;
                    hitRotRectItem.IsDraggingCorner = false;
                    hitRotRectItem.DraggingCornerIndex = -1;
                    ZIndexManager.Instance.BringToFront(hitRotRectItem);
                }
                else if (IsPointNearCorner(pos, hitRotRectItem))
                {
                    hitRotRectItem.IsDraggingCorner = true;
                    hitRotRectItem.DraggingCornerIndex = GetCornerIndex(pos, hitRotRectItem);
                    hitRotRectItem.LastDragPos = pos;
                    hitRotRectItem.IsDraggingCenter = false;
                    hitRotRectItem.IsDraggingRotate = false;
                    ZIndexManager.Instance.BringToFront(hitRotRectItem);
                }
                else
                {
                    hitRotRectItem.IsDraggingCenter = true;
                    hitRotRectItem.LastDragPos = pos;
                    hitRotRectItem.IsDraggingRotate = false;
                    hitRotRectItem.IsDraggingCorner = false;
                    hitRotRectItem.DraggingCornerIndex = -1;
                    ZIndexManager.Instance.BringToFront(hitRotRectItem);
                }
                if (hitRotRectItem.IsDraggingCorner && hitRotRectItem.DraggingCornerIndex >= 0)
                {
                    if (drawingService != null)
                    {
                        drawingService.AnimateScale(hitRotRectItem.CornerScales[hitRotRectItem.DraggingCornerIndex], 1.4);
                    }
                }
                else if (hitRotRectItem.IsDraggingRotate)
                {
                    if (drawingService != null)
                    {
                        drawingService.AnimateScale(hitRotRectItem.RotateScale, 1.4);
                    }
                }
                return;
            }
            else if (IsRotRectRoiMode && !isDrawingRect)
            {
                isDrawingRect = true;
                rectStartPoint = pos;
                rectPreviewItem = new RectRoiItem
                {
                    CenterPoint = pos,
                    Width = 1,
                    Height = 1,
                    RotationAngle = 0,
                    IsCompleted = false
                };
                RotRectRois.Add(rectPreviewItem);
                ZIndexManager.Instance.Register(rectPreviewItem);
            }
        }

        public void HandleMouseMove(Point pos, Canvas canvas)
        {
            double minX = 0, minY = 0;
            double maxX = canvas.ActualWidth;
            double maxY = canvas.ActualHeight;
            if (isDrawingRect && rectPreviewItem != null)
            {
                double width = Math.Abs(pos.X - rectStartPoint.X);
                double height = Math.Abs(pos.Y - rectStartPoint.Y);
                Point center = new Point((rectStartPoint.X + pos.X) / 2, (rectStartPoint.Y + pos.Y) / 2);
                // 限制中心點在Canvas內
                center = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampPoint(center, minX, minY, maxX, maxY);
                rectPreviewItem.CenterPoint = center;
                rectPreviewItem.Width = Math.Max(width, 1);
                rectPreviewItem.Height = Math.Max(height, 1);
                // 預覽時不顯示控制點
                if (drawingService != null)
                {
                    drawingService.DrawRois(canvas, RotRectRois.ToList(), rectPreviewItem, false);
                }
                return;
            }
            // 既有ROI拖曳功能
            foreach (var rotRectItem in RotRectRois)
            {
                if (rotRectItem.IsDraggingCenter)
                {
                    Vector delta = pos - rotRectItem.LastDragPos;
                    Point newCenter = (Point)(rotRectItem.Center + delta);
                    // 限制中心點在Canvas內（考慮旋轉後四個角都要在內部）
                    double hw = rotRectItem.Width / 2, hh = rotRectItem.Height / 2;
                    double rad = rotRectItem.Angle * Math.PI / 180.0;
                    double[] dx = { -hw, hw, hw, -hw };
                    double[] dy = { -hh, -hh, hh, hh };
                    bool isInside = true;
                    for (int i = 0; i < 4; i++)
                    {
                        double x = newCenter.X + dx[i] * Math.Cos(rad) - dy[i] * Math.Sin(rad);
                        double y = newCenter.Y + dx[i] * Math.Sin(rad) + dy[i] * Math.Cos(rad);
                        if (x < minX || x > maxX || y < minY || y > maxY)
                        {
                            isInside = false;
                            break;
                        }
                    }
                    if (isInside)
                        rotRectItem.CenterPoint = newCenter;
                    rotRectItem.LastDragPos = pos;
                    // 更新視覺效果
                    if (drawingService != null)
                    {
                        drawingService.UpdateRotRectVisual(rotRectItem);
                    }
                    RotRectRoiUpdated?.Invoke(rotRectItem);
                }
                else if (rotRectItem.IsDraggingCorner)
                {
                    double rad = rotRectItem.Angle * Math.PI / 180.0;
                    double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
                    int oppIdx = (rotRectItem.DraggingCornerIndex + 2) % 4;
                    Point[] corners = new Point[4];
                    double hw = rotRectItem.Width / 2, hh = rotRectItem.Height / 2;
                    corners[0] = rotRectItem.Center + new Vector(-hw * cosA + hh * sinA, -hw * sinA - hh * cosA);
                    corners[1] = rotRectItem.Center + new Vector(hw * cosA + hh * sinA, hw * sinA - hh * cosA);
                    corners[2] = rotRectItem.Center + new Vector(hw * cosA - hh * sinA, hw * sinA + hh * cosA);
                    corners[3] = rotRectItem.Center + new Vector(-hw * cosA - hh * sinA, -hw * sinA + hh * cosA);
                    Point opp = corners[oppIdx];
                    Point mouse = pos;
                    // 限制mouse點在Canvas內
                    mouse = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampPoint(mouse, minX, minY, maxX, maxY);
                    rotRectItem.CenterPoint = new Point((mouse.X + opp.X) / 2, (mouse.Y + opp.Y) / 2);
                    Vector v = mouse - rotRectItem.Center;
                    double localX = v.X * cosA + v.Y * sinA;
                    double localY = -v.X * sinA + v.Y * cosA;
                    rotRectItem.Width = Math.Max(Math.Abs(localX) * 2, 20);
                    rotRectItem.Height = Math.Max(Math.Abs(localY) * 2, 20);
                    rotRectItem.LastDragPos = pos;
                    // 更新視覺效果
                    if (drawingService != null)
                    {
                        drawingService.UpdateRotRectVisual(rotRectItem);
                    }
                    RotRectRoiUpdated?.Invoke(rotRectItem);
                }
                else if (rotRectItem.IsDraggingRotate)
                {
                    Vector v1 = rotRectItem.LastDragPos - rotRectItem.Center;
                    Vector v2 = pos - rotRectItem.Center;
                    double a1 = Math.Atan2(v1.Y, v1.X);
                    double a2 = Math.Atan2(v2.Y, v2.X);
                    rotRectItem.RotationAngle = rotRectItem.Angle + (a2 - a1) * 180.0 / Math.PI;
                    rotRectItem.LastDragPos = pos;
                    // 更新視覺效果
                    if (drawingService != null)
                    {
                        drawingService.UpdateRotRectVisual(rotRectItem);
                    }
                    RotRectRoiUpdated?.Invoke(rotRectItem);
                }
            }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            if (isDrawingRect && rectPreviewItem != null)
            {
                // 完成繪製
                rectPreviewItem.IsCompleted = true;
                
                // 先觸發完成事件，傳遞正確的項目
                RotRectRoiCompleted?.Invoke(rectPreviewItem);
                
                isDrawingRect = false;
                rectPreviewItem = null;
                
                // 重新繪製所有ROI，顯示控制點
                if (drawingService != null)
                {
                    drawingService.DrawRois(canvas, RotRectRois.ToList(), CurrentRotRectRoi, ShowLabels);
                }
            }
            else
            {
                // 結束拖曳狀態
                foreach (var rotRectItem in RotRectRois)
                {
                    rotRectItem.IsDraggingCenter = false;
                    rotRectItem.IsDraggingCorner = false;
                    rotRectItem.IsDraggingRotate = false;
                }
            }
        }

        public void HandleRightMouseDown(Canvas canvas)
        {
            // 取消當前正在繪製的矩形ROI
            if (isDrawingRect && rectPreviewItem != null)
            {
                RotRectRois.Remove(rectPreviewItem);
                if (drawingService != null)
                {
                    drawingService.ClearRois(canvas, RotRectRois);
                }
                isDrawingRect = false;
                rectPreviewItem = null;
            }
        }

        public override void RemoveRoi(RectRoiItem rectRoi, Canvas canvas)
        {
            if (rectRoi == null || canvas == null) return;
            
            // 如果是當前繪製的矩形，清除當前狀態
            if (CurrentRotRectRoi == rectRoi)
            {
                CurrentRotRectRoi = null;
                IsDrawingRotRectRoi = false;
            }
            
            // 如果是預覽矩形，清除預覽狀態
            if (rectPreviewItem == rectRoi)
            {
                rectPreviewItem = null;
                isDrawingRect = false;
            }
            
            // 從Canvas清除視覺元素
            if (drawingService != null)
            {
                drawingService.ClearRois(canvas, new List<RectRoiItem> { rectRoi });
            }
            
            // 從集合中移除
            if (RotRectRois.Remove(rectRoi))
            {
                // 觸發移除事件
                RotRectRoiRemoved?.Invoke(rectRoi);
            }
        }
        public void RemoveAllRotRectRois(Canvas canvas)
        {
            // 清除所有矩形ROI
            RotRectRois.Clear();
            if (drawingService != null)
            {
                drawingService.ClearRois(canvas, RotRectRois);
            }
            RotRectRoisCleared?.Invoke();
        }

        public void RedrawAllRotRectRois(Canvas canvas, bool showLabels = true)
        {
            // 重新繪製所有矩形ROI
            if (drawingService != null)
            {
                drawingService.DrawRois(canvas, RotRectRois.ToList(), CurrentRotRectRoi, showLabels);
            }
        }

        public bool IsHitTest(Point pos)
        {
            // 檢查是否點擊了任何矩形ROI
            foreach (var rotRectItem in RotRectRois)
            {
                if (IsPointNear(pos, rotRectItem.Center, 12) || 
                    IsPointNearCorner(pos, rotRectItem) || 
                    IsPointNearRotate(pos, rotRectItem) ||
                    IsPointInRotRect(pos, rotRectItem))
                {
                    return true;
                }
            }
            return false;
        }

        public void AnimateScale()
        {
            // 觸發動畫縮放
            if (drawingService != null)
            {
                drawingService.StartAnimationScaling(RotRectRois);
            }
        }

        public void Clear()
        {
            // 清除所有矩形ROI
            RotRectRois.Clear();
            CurrentRotRectRoi = null;
            IsDrawingRotRectRoi = false;
            RotRectRoisCleared?.Invoke();
        }

        /// <summary>
        /// 檢查點是否接近指定位置
        /// </summary>
        private bool IsPointNear(Point a, Point b, double tol = 12)
        {
            return (a - b).Length < tol;
        }

        /// <summary>
        /// 檢查點是否接近角點
        /// </summary>
        private bool IsPointNearCorner(Point pos, RectRoiItem rotRectItem)
        {
            Point[] corners = CalculateCorners(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);
            foreach (var corner in corners)
            {
                if (IsPointNear(pos, corner, 12))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 檢查點是否接近旋轉控制點
        /// </summary>
        private bool IsPointNearRotate(Point pos, RectRoiItem rotRectItem)
        {
            Point rotatePoint = CalculateRotatePoint(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);
            return IsPointNear(pos, rotatePoint, 12);
        }

        /// <summary>
        /// 檢查點是否在矩形內
        /// </summary>
        private bool IsPointInRotRect(Point pos, RectRoiItem rotRectItem)
        {
            Point[] corners = CalculateCorners(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);
            return IsPointInPolygon(pos, corners);
        }

        /// <summary>
        /// 獲取角點索引
        /// </summary>
        private int GetCornerIndex(Point pos, RectRoiItem rotRectItem)
        {
            Point[] corners = CalculateCorners(rotRectItem.Center, rotRectItem.Width, rotRectItem.Height, rotRectItem.Angle);
            for (int i = 0; i < 4; i++)
            {
                if (IsPointNear(pos, corners[i], 12))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 檢查點是否在多邊形內
        /// </summary>
        private bool IsPointInPolygon(Point point, Point[] polygon)
        {
            int n = polygon.Length;
            bool inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// 計算四個角點
        /// </summary>
        private Point[] CalculateCorners(Point center, double width, double height, double angle)
        {
            double rad = angle * Math.PI / 180.0;
            double cosA = Math.Cos(rad), sinA = Math.Sin(rad);
            double hw = width / 2, hh = height / 2;

            return new Point[]
            {
                center + new Vector(-hw * cosA + hh * sinA, -hw * sinA - hh * cosA),
                center + new Vector(hw * cosA + hh * sinA, hw * sinA - hh * cosA),
                center + new Vector(hw * cosA - hh * sinA, hw * sinA + hh * cosA),
                center + new Vector(-hw * cosA - hh * sinA, -hw * sinA + hh * cosA)
            };
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

        public RectRoiItem GetHitRotRectRoiItem(Point pos)
        {
            var hitItems = RotRectRois
                .Where(rotRectItem =>
                {
                    if (IsPointNear(pos, rotRectItem.Center, 12) || 
                        IsPointNearCorner(pos, rotRectItem) || 
                        IsPointNearRotate(pos, rotRectItem) ||
                        IsPointInRotRect(pos, rotRectItem))
                        return true;
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        public RectRoiItem GetHitRoiItem(Point pos)
        {
            foreach (var rotRectItem in RotRectRois)
            {
                if (IsPointNear(pos, rotRectItem.Center, 12) ||
                    IsPointNearCorner(pos, rotRectItem) ||
                    IsPointNearRotate(pos, rotRectItem) ||
                    IsPointInRotRect(pos, rotRectItem))
                {
                    return rotRectItem;
                }
            }
            return null;
        }
    }
} 