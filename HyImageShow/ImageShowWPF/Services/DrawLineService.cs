using HyImageShow.ImageShowWPF.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HyImageShow.ImageShowWPF.Services
{
    /// <summary>
    /// 畫線服務實現類
    /// </summary>
    public class DrawLineService : BaseRoiService<LineItem, DrawLineDrawingService>
    {
        private readonly Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)> styleDict;
        private DrawLineDrawingService drawingService;
        public ObservableCollection<LineItem> DrawLines { get; }

        public LineItem CurrentDrawLine { get; private set; }

        public bool IsDrawingLine { get; private set; }

        public bool IsDrawLineMode { get; private set; }

        public bool AllowMultiDrag { get; set; } = false; // 預設單一拖曳

        public DrawLineService()
        {
            // 初始化樣式字典，與主視圖保持一致
            styleDict = new Dictionary<string, (Color line, Color glow, Color labelBg, Color labelFg)>
            {
                {"line", (Color.FromRgb(0, 188, 212), Color.FromRgb(0, 150, 167), Color.FromArgb(220, 178, 235, 242), Color.FromRgb(0, 105, 120))}, // 青
            };

            DrawLines = new ObservableCollection<LineItem>();
            ShowLabels = true; // 預設顯示標籤
        }

        /// <summary>
        /// 是否顯示標籤
        /// </summary>
        public bool ShowLabels { get; set; }

        public event Action<LineItem> LineCompleted;
        public event Action<LineItem> LineUpdated;
        public event Action<LineItem> LineRemoved;
        public event Action LinesCleared;

        /// <summary>
        /// 設置繪製服務引用
        /// </summary>
        public void SetDrawingService(DrawLineDrawingService service)
        {
            drawingService = service;
        }

        public void EnableDrawLineMode()
        {
            IsDrawLineMode = true;
        }

        public void DisableDrawLineMode()
        {
            IsDrawLineMode = false;
            IsDrawingLine = false;
            CurrentDrawLine = null;
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            // 先清空所有 ROI 拖曳狀態（單選模式）
            if (!AllowMultiDrag || !isShift)
            {
                foreach (var lineItem in DrawLines)
                {
                    lineItem.IsDraggingPoint1 = false;
                    lineItem.IsDraggingPoint2 = false;
                    lineItem.IsDraggingLine = false;
                    lineItem.Selected = false;
                }
            }
            // 只找 Z-Index 最大的命中 ROI
            var hitLineItem = GetHitLineItem(pos);
            if (hitLineItem != null)
            {
                if (AllowMultiDrag && isShift)
                {
                    hitLineItem.Selected = !hitLineItem.Selected;
                }
                else
                {
                    hitLineItem.Selected = true;
                }
                if (IsPointNear(pos, hitLineItem.P1, 12))
                {
                    hitLineItem.IsDraggingPoint1 = true;
                    hitLineItem.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(hitLineItem);
                }
                else if (IsPointNear(pos, hitLineItem.P2, 12))
                {
                    hitLineItem.IsDraggingPoint2 = true;
                    hitLineItem.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(hitLineItem);
                }
                else
                {
                    hitLineItem.IsDraggingLine = true;
                    hitLineItem.LastDragPos = pos;
                    ZIndexManager.Instance.BringToFront(hitLineItem);
                }
                AnimateScale();
                return;
            }
            if (!IsDrawLineMode) return;
            IsDrawingLine = true;
            CurrentDrawLine = new LineItem
            {
                P1 = pos,
                P2 = pos,
                IsCompleted = false
            };
            DrawLines.Add(CurrentDrawLine);
            ZIndexManager.Instance.Register(CurrentDrawLine);
            if (drawingService != null)
            {
                drawingService.DrawSingleLine(CurrentDrawLine, canvas, ShowLabels);
            }
        }

        public void HandleMouseMove(Point pos, Canvas canvas)
        {
            double maxX = canvas.ActualWidth;
            double maxY = canvas.ActualHeight;
            if (IsDrawLineMode && IsDrawingLine && CurrentDrawLine != null)
            {
                var clamp = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampLine(CurrentDrawLine.P1, pos, 0, 0, maxX, maxY);
                CurrentDrawLine.P2 = clamp.p2;
                if (drawingService != null)
                {
                    drawingService.UpdateLineVisual(CurrentDrawLine);
                }
                return;
            }
            foreach (var lineItem in DrawLines)
            {
                if (lineItem.IsDraggingPoint1)
                {
                    var clamp = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampLine(lineItem.P2, pos, 0, 0, maxX, maxY);
                    lineItem.P1 = clamp.p2;
                    if (drawingService != null)
                    {
                        drawingService.UpdateLineVisual(lineItem);
                    }
                    LineUpdated?.Invoke(lineItem);
                }
                else if (lineItem.IsDraggingPoint2)
                {
                    var clamp = HyImageShow.ImageShowWPF.Models.CanvasBoundaryHelper.ClampLine(lineItem.P1, pos, 0, 0, maxX, maxY);
                    lineItem.P2 = clamp.p2;
                    if (drawingService != null)
                    {
                        drawingService.UpdateLineVisual(lineItem);
                    }
                    LineUpdated?.Invoke(lineItem);
                }
                else if (lineItem.IsDraggingLine)
                {
                    Vector delta = pos - lineItem.LastDragPos;
                    Point newP1 = lineItem.P1 + delta;
                    Point newP2 = lineItem.P2 + delta;
                    if (newP1.X >= 0 && newP1.X <= maxX && newP1.Y >= 0 && newP1.Y <= maxY &&
                        newP2.X >= 0 && newP2.X <= maxX && newP2.Y >= 0 && newP2.Y <= maxY)
                    {
                        lineItem.P1 = newP1;
                        lineItem.P2 = newP2;
                        lineItem.LastDragPos = pos;
                        if (drawingService != null)
                        {
                            drawingService.UpdateLineVisual(lineItem);
                        }
                        LineUpdated?.Invoke(lineItem);
                    }
                }
            }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            // 完成繪製新線條（只有在畫線模式下）
            if (IsDrawLineMode && IsDrawingLine && CurrentDrawLine != null)
            {
                CurrentDrawLine.P2 = pos;

                // 檢查起點和終點的距離，只有當距離足夠時才完成畫線
                double distance = Math.Sqrt(Math.Pow(CurrentDrawLine.P2.X - CurrentDrawLine.P1.X, 2) + Math.Pow(CurrentDrawLine.P2.Y - CurrentDrawLine.P1.Y, 2));

                if (distance >= 5.0) // 最小距離閾值
                {
                    CurrentDrawLine.IsCompleted = true;
                    if (drawingService != null)
                    {
                        drawingService.UpdateLineVisual(CurrentDrawLine);
                        if (ShowLabels)
                        {
                            drawingService.AddLineLabels(CurrentDrawLine, canvas);
                        }
                    }
                    LineCompleted?.Invoke(CurrentDrawLine);
                }
                else
                {
                    // 距離太短，取消繪製
                    DrawLines.Remove(CurrentDrawLine);
                    if (drawingService != null)
                    {
                        drawingService.RemoveCurrentLine(canvas);
                    }
                }

                IsDrawingLine = false;
                CurrentDrawLine = null;
            }
            else
            {
                // 結束拖曳狀態
                foreach (var lineItem in DrawLines)
                {
                    lineItem.IsDraggingPoint1 = false;
                    lineItem.IsDraggingPoint2 = false;
                    lineItem.IsDraggingLine = false;
                }
            }
        }

        public void HandleRightMouseDown(Canvas canvas)
        {
            // 取消當前正在繪製的線條
            if (IsDrawingLine && CurrentDrawLine != null)
            {
                DrawLines.Remove(CurrentDrawLine);
                if (drawingService != null)
                {
                    drawingService.RemoveCurrentLine(canvas);
                }
                IsDrawingLine = false;
                CurrentDrawLine = null;
            }
        }

        public void RemoveAllLines(Canvas canvas)
        {
            // 清除所有線條
            DrawLines.Clear();
            if (drawingService != null)
            {
                drawingService.ClearLines(mainCanvas, DrawLines, CurrentDrawLine);
            }
            LinesCleared?.Invoke();
        }

        public override void RemoveRoi(LineItem lineRoi, Canvas canvas)
        {
            if (lineRoi == null || canvas == null) return;
            
            // 如果是當前繪製的線條，清除當前狀態
            if (CurrentDrawLine == lineRoi)
            {
                CurrentDrawLine = null;
                IsDrawingLine = false;
            }
            
            // 從Canvas清除視覺元素
            if (drawingService != null)
            {
                drawingService.ClearRois(canvas, new List<LineItem> { lineRoi });
            }
            
            // 從集合中移除
            if (DrawLines.Remove(lineRoi))
            {
                // 觸發移除事件
                LineRemoved?.Invoke(lineRoi);
            }
        }

        public void RedrawAllLines(Canvas canvas, bool showLabels = true)
        {
            // 重新繪製所有線條
            if (drawingService != null)
            {
                drawingService.DrawLines(canvas, DrawLines, CurrentDrawLine, showLabels);
            }
        }

        public bool IsHitTest(Point pos)
        {
            // 檢查是否點擊了任何線條
            foreach (var lineItem in DrawLines)
            {
                if (IsPointNear(pos, lineItem.P1, 12) || IsPointNear(pos, lineItem.P2, 12))
                {
                    return true;
                }
                else if (IsPointNearLine(pos, lineItem.P1, lineItem.P2, 8))
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
                drawingService.StartAnimationScaling(DrawLines);
            }
        }

        public void Clear()
        {
            // 清除所有線條
            IsDrawingLine = false;
            if (drawingService != null && mainCanvas != null)
            {
                drawingService.ClearLines(mainCanvas, DrawLines, CurrentDrawLine);
            }
            DrawLines.Clear();
            CurrentDrawLine = null;
            LinesCleared?.Invoke();
        }

        /// <summary>
        /// 檢查點是否接近指定位置
        /// </summary>
        private bool IsPointNear(Point a, Point b, double tol = 12)
        {
            return (a - b).Length < tol;
        }

        /// <summary>
        /// 檢查點是否接近線條
        /// </summary>
        private bool IsPointNearLine(Point p, Point a, Point b, double tol)
        {
            Vector v = b - a;
            Vector w = p - a;
            double c1 = Vector.Multiply(w, v);
            if (c1 <= 0) return IsPointNear(p, a, tol);
            double c2 = Vector.Multiply(v, v);
            if (c2 <= c1) return IsPointNear(p, b, tol);
            double b_param = c1 / c2;
            Point pb = a + b_param * v;
            return IsPointNear(p, pb, tol);
        }

        public LineItem GetHitLineItem(Point pos)
        {
            var hitItems = DrawLines
                .Where(lineItem =>
                {
                    if (IsPointNear(pos, lineItem.P1, 12) || IsPointNear(pos, lineItem.P2, 12))
                        return true;
                    else if (IsPointNearLine(pos, lineItem.P1, lineItem.P2, 8))
                        return true;
                    return false;
                })
                .ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }

        public LineItem GetHitRoiItem(Point pos)
        {
            foreach (var lineItem in DrawLines)
            {
                if (IsPointNear(pos, lineItem.P1, 12) || IsPointNear(pos, lineItem.P2, 12) ||
                    IsPointNearLine(pos, lineItem.P1, lineItem.P2, 8))
                {
                    return lineItem;
                }
            }
            return null;
        }

    }
}