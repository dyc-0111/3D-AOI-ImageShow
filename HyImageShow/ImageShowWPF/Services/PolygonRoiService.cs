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
    /// 多邊形ROI服務實作
    /// </summary>
    public class PolygonRoiService : BaseRoiService<PolygonRoiItem, PolygonRoiDrawingService>
    {
        private ObservableCollection<PolygonRoiItem> polygonRois;
        private PolygonRoiItem currentPolygonRoi;
        private bool isPolygonRoiMode;
        private Canvas mainCanvas;
        private PolygonRoiDrawingService drawingService;
        private DateTime lastUpdateTime = DateTime.MinValue;
        private const int UPDATE_THROTTLE_MS = 16; // 約60FPS
        private int clickCount;
        private bool isDraggingDot;
        private int draggingDotIndex;
        public bool AllowMultiDrag { get; set; } = false; // 預設單一拖曳

        public PolygonRoiService()
        {
            polygonRois = new ObservableCollection<PolygonRoiItem>();
            currentPolygonRoi = null;
            isPolygonRoiMode = false;
            clickCount = 0;
            isDraggingDot = false;
            draggingDotIndex = -1;
        }

        public ObservableCollection<PolygonRoiItem> PolygonRois => polygonRois;
        public PolygonRoiItem CurrentPolygonRoi => currentPolygonRoi;
        public bool IsPolygonRoiMode => isPolygonRoiMode;

        public event Action<PolygonRoiItem> PolygonRoiCompleted;
        public event Action<PolygonRoiItem> PolygonRoiUpdated;
        public event Action<PolygonRoiItem> PolygonRoiRemoved;
        public event Action PolygonRoisCleared;

        public void EnablePolygonRoiMode()
        {
            isPolygonRoiMode = true;
        }

        public void DisablePolygonRoiMode()
        {
            isPolygonRoiMode = false;
            currentPolygonRoi = null;
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            // 先清空所有 ROI 拖曳狀態（單選模式）
            if (!AllowMultiDrag || !isShift)
            {
                foreach (var roi in polygonRois)
                {
                    roi.IsDraggingBody = false;
                    roi.IsDraggingRotate = false;
                    roi.IsDraggingPoint = false;
                    roi.DraggingPointIndex = -1;
                    roi.Selected = false;
                }
            }

            // 1. 如果有「創建中」的多邊形，優先處理新增點/閉合
            if (isPolygonRoiMode && currentPolygonRoi != null && !currentPolygonRoi.IsCompleted)
            {
                Point center = currentPolygonRoi.Center;
                Point rotatedFirstPoint = RotatePoint(currentPolygonRoi.Points[0], center, currentPolygonRoi.Angle);
                if (currentPolygonRoi.Points.Count >= 3 && IsPointNear(pos, rotatedFirstPoint))
                {
                    currentPolygonRoi.IsCompleted = true;
                    PolygonRoiCompleted?.Invoke(currentPolygonRoi);
                    currentPolygonRoi = null;
                }
                else
                {
                    currentPolygonRoi.AddPoint(pos);
                    if (drawingService != null)
                    {
                        drawingService.UpdatePolygonVisual(currentPolygonRoi);
                    }
                }
                PolygonRoiUpdated?.Invoke(currentPolygonRoi);
                return;
            }

            // 2. 沒有創建中的多邊形時，才進行 hit 測試（拖曳、選取等）
            var hitRoi = GetHitPolygonRoiItem(pos);
            if (hitRoi != null)
            {
                if (AllowMultiDrag && isShift)
                {
                    hitRoi.Selected = !hitRoi.Selected;
                }
                else
                {
                    hitRoi.Selected = true;
                }
                Point center = hitRoi.Center;
                List<Point> rotatedPoints = new List<Point>();
                foreach (var point in hitRoi.Points)
                {
                    Point rotatedPoint = RotatePoint(point, center, hitRoi.Angle);
                    rotatedPoints.Add(rotatedPoint);
                }
                var rotatePt = GetPolygonRotatePoint(hitRoi);
                if (hitRoi.IsCompleted && IsPointNear(pos, rotatePt))
                {
                    hitRoi.IsDraggingRotate = true;
                    hitRoi.LastDragPos = pos;
                    hitRoi.RotateStartAngle = hitRoi.RotationAngle;
                    Vector v = pos - center;
                    hitRoi.RotateStartVectorAngle = Math.Atan2(v.Y, v.X) * 180 / Math.PI;
                    ZIndexManager.Instance.BringToFront(hitRoi);
                }
                for (int i = 0; i < rotatedPoints.Count; i++)
                {
                    if (IsPointNear(pos, rotatedPoints[i]))
                    {
                        hitRoi.IsDraggingPoint = true;
                        hitRoi.DraggingPointIndex = i;
                        hitRoi.LastDragPos = pos;
                        
                        // 🔧 修正：記錄拖曳開始時的固定中心點
                        hitRoi.DragStartCenter = hitRoi.Center;
                        
                        ZIndexManager.Instance.BringToFront(hitRoi);
                        return;
                    }
                }
                if (hitRoi.IsCompleted && IsPointInPolygon(pos, rotatedPoints))
                {
                    hitRoi.IsDraggingBody = true;
                    hitRoi.DragStart = pos;
                    hitRoi.DragStartPoints = new List<Point>(hitRoi.Points);
                    ZIndexManager.Instance.BringToFront(hitRoi);
                }
                return;
            }

            // 3. 如果在多邊形模式下且沒有創建中的多邊形，才開始新的多邊形
            if (isPolygonRoiMode)
            {
                currentPolygonRoi = new PolygonRoiItem();
                currentPolygonRoi.AddPoint(pos);
                polygonRois.Add(currentPolygonRoi);
                ZIndexManager.Instance.Register(currentPolygonRoi);
                if (drawingService != null)
                {
                    drawingService.DrawRois(canvas, polygonRois.ToList(), currentPolygonRoi, true);
                }
                PolygonRoiUpdated?.Invoke(currentPolygonRoi);
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

            // 取得Canvas邊界
            double minX = 0;
            double minY = 0;
            double maxX = canvas.ActualWidth;
            double maxY = canvas.ActualHeight;

            // 1. 創建中多邊形
            if (currentPolygonRoi != null && !currentPolygonRoi.IsCompleted)
            {
                // 更新正在繪製的多邊形ROI
                PolygonRoiUpdated?.Invoke(currentPolygonRoi);
                return;
            }

            // 2. 拖曳已完成的多邊形
            foreach (var roi in polygonRois)
            {
                // 拖曳頂點
                if (roi.IsDraggingPoint && roi.DraggingPointIndex >= 0 && roi.DraggingPointIndex < roi.Points.Count)
                {
                    // 🔧 修正：使用固定中心點進行座標轉換
                    Point fixedCenter = roi.DragStartCenter;
                    if (fixedCenter.X == 0 && fixedCenter.Y == 0)
                    {
                        fixedCenter = roi.Center;
                        roi.DragStartCenter = fixedCenter;
                    }
                    
                    // 使用固定中心點進行座標轉換
                    Point newOriginalPos = InverseRotatePoint(pos, fixedCenter, roi.RotationAngle);
                    
                    // 邊界檢查：檢查新位置旋轉後是否在邊界內
                    Point newRotatedPos = RotatePoint(newOriginalPos, fixedCenter, roi.RotationAngle);
                    bool inBounds = newRotatedPos.X >= minX && newRotatedPos.X <= maxX && 
                                   newRotatedPos.Y >= minY && newRotatedPos.Y <= maxY;
                    
                    if (inBounds)
                    {
                        // 🔧 關鍵修正：只更新被拖曳的頂點，其他頂點座標完全不變
                        roi.UpdatePoint(roi.DraggingPointIndex, newOriginalPos);
                        if (drawingService != null)
                        {
                            // 🔧 修正：在拖曳過程中使用固定中心點進行繪製
                            drawingService.UpdatePolygonVisual(roi, fixedCenter);
                        }
                        PolygonRoiUpdated?.Invoke(roi);
                    }
                    return;
                }
                // 拖曳整顆多邊形
                if (roi.IsDraggingBody && roi.DragStartPoints != null)
                {
                    Vector delta = pos - roi.DragStart;
                    var newPoints = new List<Point>();
                    for (int i = 0; i < roi.DragStartPoints.Count; i++)
                    {
                        newPoints.Add((Point)(roi.DragStartPoints[i] + delta));
                    }
                    
                    // 🔧 改進：檢查旋轉後的整體多邊形是否都在Canvas內
                    Point newCenter = CalculateCenter(newPoints);
                    var rotatedNewPoints = newPoints.Select(p => RotatePoint(p, newCenter, roi.RotationAngle)).ToList();
                    
                    // 檢查所有旋轉後的頂點是否都在邊界內
                    bool allPointsInBounds = rotatedNewPoints.All(p => 
                        p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY);
                    
                    if (allPointsInBounds)
                    {
                        roi.UpdatePointsBatch(newPoints);
                        if (drawingService != null)
                        {
                            drawingService.UpdatePolygonVisual(roi);
                        }
                        PolygonRoiUpdated?.Invoke(roi);
                    }
                    return;
                }
                // 拖曳旋轉點
                if (roi.IsDraggingRotate)
                {
                    // 限制鼠標位置在Canvas內
                    Point clampedPos = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampPoint(pos, minX, minY, maxX, maxY);
                    
                    Point center = roi.Center;
                    Vector v = clampedPos - center;
                    double currentVectorAngle = Math.Atan2(v.Y, v.X) * 180 / Math.PI;
                    double delta = currentVectorAngle - roi.RotateStartVectorAngle;
                    double newAngle = roi.RotateStartAngle + delta;
                    
                    // 🔧 改進：檢查旋轉後整個多邊形是否超出邊界
                    var rotatedPoints = roi.Points.Select(p => RotatePoint(p, center, newAngle)).ToList();
                    bool allPointsInBounds = rotatedPoints.All(p => 
                        p.X >= minX && p.X <= maxX && p.Y >= minY && p.Y <= maxY);
                    
                    if (allPointsInBounds)
                    {
                        roi.RotationAngle = newAngle;
                        roi.LastDragPos = clampedPos;
                        if (drawingService != null)
                        {
                            drawingService.UpdatePolygonVisual(roi);
                        }
                        PolygonRoiUpdated?.Invoke(roi);
                    }
                    return;
                }
            }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            // 釋放所有 ROI 的拖曳狀態
            foreach (var roi in polygonRois)
            {
                bool wasDraggingPoint = roi.IsDraggingPoint;
                bool wasDraggingRotate = roi.IsDraggingRotate;
                bool wasDraggingBody = roi.IsDraggingBody;

                roi.IsDraggingPoint = false;
                roi.IsDraggingRotate = false;
                roi.IsDraggingBody = false;
                roi.DraggingPointIndex = -1;
                roi.DragStartPoints = null;
                
                // 🔧 修正：拖曳結束後清理固定中心點，讓中心點自然重新計算
                if (wasDraggingPoint)
                {
                    roi.DragStartCenter = new Point(0, 0);
                }

                if (wasDraggingPoint || wasDraggingRotate || wasDraggingBody)
                {
                    PolygonRoiUpdated?.Invoke(roi);
                }
            }

            // 釋放創建中的 ROI（如果有）
            if (currentPolygonRoi != null)
            {
                bool wasDraggingPoint = currentPolygonRoi.IsDraggingPoint;
                bool wasDraggingRotate = currentPolygonRoi.IsDraggingRotate;
                bool wasDraggingBody = currentPolygonRoi.IsDraggingBody;

                currentPolygonRoi.IsDraggingPoint = false;
                currentPolygonRoi.IsDraggingRotate = false;
                currentPolygonRoi.IsDraggingBody = false;
                currentPolygonRoi.DraggingPointIndex = -1;
                currentPolygonRoi.DragStartPoints = null;
                
                // 🔧 修正：拖曳結束後清理固定中心點
                if (wasDraggingPoint)
                {
                    currentPolygonRoi.DragStartCenter = new Point(0, 0);
                }

                if (wasDraggingPoint || wasDraggingRotate || wasDraggingBody)
                {
                    PolygonRoiUpdated?.Invoke(currentPolygonRoi);
                }
            }
        }

        public bool IsHitTest(Point pos)
        {
            // 檢查是否點擊了任何多邊形ROI
            foreach (var polygonRoi in polygonRois)
            {
                // 計算旋轉後的點座標
                Point center = polygonRoi.Center;
                List<Point> rotatedPoints = new List<Point>();
                foreach (var point in polygonRoi.Points)
                {
                    Point rotatedPoint = RotatePoint(point, center, polygonRoi.Angle);
                    rotatedPoints.Add(rotatedPoint);
                }
                
                // 檢查是否點擊頂點（使用旋轉後的座標）
                for (int i = 0; i < rotatedPoints.Count; i++)
                {
                    if (IsPointNear(pos, rotatedPoints[i]))
                    {
                        return true;
                    }
                }
                
                // 檢查是否點擊旋轉點
                if (polygonRoi.IsCompleted && IsPointNear(pos, GetPolygonRotatePoint(polygonRoi)))
                {
                    return true;
                }
                
                // 檢查是否點擊多邊形內部（使用旋轉後的座標）
                if (polygonRoi.IsCompleted && IsPointInPolygon(pos, rotatedPoints))
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveAllPolygonRois(Canvas canvas)
        {
            // 先清除所有UI元素
            foreach (var roi in polygonRois)
            {
                // 安全移除UI元素
                if (roi.Polygon != null)
                {
                    if (canvas.Children.Contains(roi.Polygon))
                        canvas.Children.Remove(roi.Polygon);
                    roi.Polygon = null;
                }
                foreach (var dot in roi.PointDots)
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                roi.PointDots.Clear();
                if (roi.RotateDot != null)
                {
                    if (canvas.Children.Contains(roi.RotateDot))
                        canvas.Children.Remove(roi.RotateDot);
                    roi.RotateDot = null;
                }
                if (roi.MainLabelBorder != null)
                {
                    if (canvas.Children.Contains(roi.MainLabelBorder))
                        canvas.Children.Remove(roi.MainLabelBorder);
                    roi.MainLabelBorder = null;
                }
                foreach (var label in roi.VertexLabelBorders)
                {
                    if (label != null && canvas.Children.Contains(label))
                        canvas.Children.Remove(label);
                }
                roi.VertexLabelBorders.Clear();
            }
            
            // 清除列表
            polygonRois.Clear();
            currentPolygonRoi = null;
            PolygonRoisCleared?.Invoke();
        }

        public void RemovePolygonRoi(PolygonRoiItem polygonRoi, Canvas canvas)
        {
            if (polygonRois.Contains(polygonRoi))
            {
                // 安全移除UI元素
                if (polygonRoi.Polygon != null)
                {
                    if (canvas.Children.Contains(polygonRoi.Polygon))
                        canvas.Children.Remove(polygonRoi.Polygon);
                    polygonRoi.Polygon = null;
                }
                foreach (var dot in polygonRoi.PointDots)
                {
                    if (dot != null && canvas.Children.Contains(dot))
                        canvas.Children.Remove(dot);
                }
                polygonRoi.PointDots.Clear();
                if (polygonRoi.RotateDot != null)
                {
                    if (canvas.Children.Contains(polygonRoi.RotateDot))
                        canvas.Children.Remove(polygonRoi.RotateDot);
                    polygonRoi.RotateDot = null;
                }
                if (polygonRoi.MainLabelBorder != null)
                {
                    if (canvas.Children.Contains(polygonRoi.MainLabelBorder))
                        canvas.Children.Remove(polygonRoi.MainLabelBorder);
                    polygonRoi.MainLabelBorder = null;
                }
                foreach (var label in polygonRoi.VertexLabelBorders)
                {
                    if (label != null && canvas.Children.Contains(label))
                        canvas.Children.Remove(label);
                }
                polygonRoi.VertexLabelBorders.Clear();
                
                polygonRois.Remove(polygonRoi);
                PolygonRoiRemoved?.Invoke(polygonRoi);
            }
        }

        public void SetMainCanvas(Canvas canvas)
        {
            mainCanvas = canvas;
        }

        public void SetDrawingService(PolygonRoiDrawingService drawingService)
        {
            this.drawingService = drawingService;
        }

        public override void RemoveRoi(PolygonRoiItem polygonRoi, Canvas canvas)
        {
            RemovePolygonRoi(polygonRoi, canvas);
        }

        /// <summary>
        /// 處理滑鼠右鍵按下事件
        /// </summary>
        public void HandleRightMouseDown(Point pos, Canvas canvas)
        {
            // 如果正在創建多邊形ROI，取消創建
            if (currentPolygonRoi != null && !currentPolygonRoi.IsCompleted)
            {
                CancelCurrentPolygonRoi(canvas);
            }
        }

        /// <summary>
        /// 取消當前多邊形ROI的創建
        /// </summary>
        public void CancelCurrentPolygonRoi(Canvas canvas)
        {
            if (currentPolygonRoi != null && !currentPolygonRoi.IsCompleted)
            {
                // 從列表中移除未完成的多邊形ROI
                polygonRois.Remove(currentPolygonRoi);
                
                // 清除UI元素
                currentPolygonRoi.ClearUIElements();
                
                // 重置當前多邊形ROI
                currentPolygonRoi = null;
            }
        }

        /// <summary>
        /// 計算點列表的中心點
        /// </summary>
        private Point CalculateCenter(List<Point> points)
        {
            if (points == null || points.Count == 0)
                return new Point(0, 0);

            double sumX = 0, sumY = 0;
            foreach (var point in points)
            {
                sumX += point.X;
                sumY += point.Y;
            }
            return new Point(sumX / points.Count, sumY / points.Count);
        }

        /// <summary>
        /// 檢查點是否接近指定位置
        /// </summary>
        private bool IsPointNear(Point a, Point b, double tol = 12)
        {
            return (a - b).Length < tol;
        }

        /// <summary>
        /// 檢查點是否在多邊形內部
        /// </summary>
        private bool IsPointInPolygon(Point pos, List<Point> polygon)
        {
            if (polygon.Count < 3) return false;
            
            int intersections = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Point p1 = polygon[i];
                Point p2 = polygon[(i + 1) % polygon.Count];
                
                if (((p1.Y > pos.Y) != (p2.Y > pos.Y)) &&
                    (pos.X < (p2.X - p1.X) * (pos.Y - p1.Y) / (p2.Y - p1.Y) + p1.X))
                {
                    intersections++;
                }
            }
            return (intersections % 2) == 1;
        }

        /// <summary>
        /// 獲取多邊形旋轉點
        /// </summary>
        private Point GetPolygonRotatePoint(PolygonRoiItem polygonRoi)
        {
            if (polygonRoi.Points.Count == 0) return new Point();
            
            Point center = polygonRoi.Center;
            double maxDistance = polygonRoi.Points.Max(p => (p - center).Length);
            Point baseRotatePoint = (Point)(center + new Vector(0, -maxDistance - 20));
            
            // 將旋轉點也進行旋轉變換
            return RotatePoint(baseRotatePoint, center, polygonRoi.Angle);
        }

        /// <summary>
        /// 旋轉點
        /// </summary>
        private Point RotatePoint(Point point, Point center, double angle)
        {
            double radians = angle * Math.PI / 180;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            double dx = point.X - center.X;
            double dy = point.Y - center.Y;

            double rotatedX = dx * cos - dy * sin + center.X;
            double rotatedY = dx * sin + dy * cos + center.Y;

            return new Point(rotatedX, rotatedY);
        }

        /// <summary>
        /// 反旋轉點
        /// </summary>
        private Point InverseRotatePoint(Point point, Point center, double angle)
        {
            double radians = -angle * Math.PI / 180;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            double dx = point.X - center.X;
            double dy = point.Y - center.Y;

            double rotatedX = dx * cos - dy * sin + center.X;
            double rotatedY = dx * sin + dy * cos + center.Y;

            return new Point(rotatedX, rotatedY);
        }

        public PolygonRoiItem GetHitPolygonRoiItem(Point pos)
        {
            var hitItems = polygonRois
                .Where(polygonRoi =>
                {
                    Point center = polygonRoi.Center;
                    List<Point> rotatedPoints = new List<Point>();
                    foreach (var point in polygonRoi.Points)
                    {
                        Point rotatedPoint = RotatePoint(point, center, polygonRoi.Angle);
                        rotatedPoints.Add(rotatedPoint);
                    }
                    for (int i = 0; i < rotatedPoints.Count; i++)
                    {
                        if (IsPointNear(pos, rotatedPoints[i]))
                        {
                            return true;
                        }
                    }
                    if (polygonRoi.IsCompleted && IsPointNear(pos, GetPolygonRotatePoint(polygonRoi)))
                    {
                        return true;
                    }
                    if (polygonRoi.IsCompleted && IsPointInPolygon(pos, rotatedPoints))
                    {
                        return true;
                    }
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        public PolygonRoiItem GetHitRoiItem(Point pos)
        {
            foreach (var polygonRoi in polygonRois)
            {
                Point center = polygonRoi.Center;
                List<Point> rotatedPoints = new List<Point>();
                foreach (var point in polygonRoi.Points)
                {
                    Point rotatedPoint = RotatePoint(point, center, polygonRoi.Angle);
                    rotatedPoints.Add(rotatedPoint);
                }
                for (int i = 0; i < rotatedPoints.Count; i++)
                {
                    if (IsPointNear(pos, rotatedPoints[i]))
                        return polygonRoi;
                }
                if (polygonRoi.IsCompleted && IsPointNear(pos, GetPolygonRotatePoint(polygonRoi)))
                    return polygonRoi;
                if (polygonRoi.IsCompleted && IsPointInPolygon(pos, rotatedPoints))
                    return polygonRoi;
            }
            return null;
        }
    }
} 