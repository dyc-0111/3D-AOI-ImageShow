using HyImageShow.ImageShowWPF.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HyImageShow.ImageShowWPF.Services
{
    public class PointRoiService : BaseRoiService<PointItem, PointRoiDrawingService>
    {
        private PointRoiDrawingService drawingService;
        public ObservableCollection<PointItem> Points { get; }
        public PointItem CurrentPoint { get; private set; }
        public bool IsDrawingPoint { get; private set; }
        public bool IsDrawPointMode { get; private set; }
        public bool AllowMultiDrag { get; set; } = false;
        public bool ShowLabels { get; set; } = true;

        public PointRoiService()
        {
            Points = new ObservableCollection<PointItem>();
        }

        public event Action<PointItem> PointCompleted;
        public event Action<PointItem> PointUpdated;
        public event Action PointsCleared;

        public void SetDrawingService(PointRoiDrawingService service)
        {
            drawingService = service;
        }

        public void EnableDrawPointMode() => IsDrawPointMode = true;
        public void DisableDrawPointMode()
        {
            IsDrawPointMode = false;
            IsDrawingPoint = false;
            CurrentPoint = null;
        }

        public void StartAnimationScaling()
        {
            foreach (var pt in Points)
            {
                if (pt.Selected)
                {
                    pt.PointScale.ScaleX = 1.5;
                    pt.PointScale.ScaleY = 1.5;
                }
            }
        }

        public void StopAnimationScaling()
        {
            foreach (var pt in Points)
            {
                pt.PointScale.ScaleX = 1.0;
                pt.PointScale.ScaleY = 1.0;
            }
        }

        public void HandleMouseDown(Point pos, Canvas canvas, bool isShift = false)
        {
            if (!AllowMultiDrag || !isShift)
            {
                foreach (var pt in Points)
                {
                    pt.Selected = false;
                }
            }
            var hitPoint = GetHitPointItem(pos);
            if (hitPoint != null)
            {
                if (AllowMultiDrag && isShift)
                    hitPoint.Selected = !hitPoint.Selected;
                else
                    hitPoint.Selected = true;
                hitPoint.LastDragPos = pos;
                ZIndexManager.Instance.BringToFront(hitPoint);
                hitPoint.IsCompleted = false;
                StartAnimationScaling();
                return;
            }
            if (!IsDrawPointMode) return;
            IsDrawingPoint = true;
            CurrentPoint = new PointItem(pos) { IsCompleted = true };
            Points.Add(CurrentPoint);
            ZIndexManager.Instance.Register(CurrentPoint);
            drawingService?.DrawSingleRoi(CurrentPoint, canvas, ShowLabels);
            PointCompleted?.Invoke(CurrentPoint);
            IsDrawingPoint = false;
            CurrentPoint = null;
            IsDrawPointMode = false;
        }

        public void HandleMouseMove(Point pos, Canvas canvas)
        {
            foreach (var pt in Points)
            {
                if (pt.Selected && !pt.IsCompleted)
                {
                    Vector delta = pos - pt.LastDragPos;
                    pt.Position = pt.Position + delta;
                    pt.LastDragPos = pos;
                    drawingService?.UpdateRoiVisual(pt);
                    PointUpdated?.Invoke(pt);
                }
            }
        }

        public void HandleMouseUp(Point pos, Canvas canvas)
        {
            foreach (var pt in Points)
            {
                if (pt.Selected && !pt.IsCompleted)
                {
                    pt.IsCompleted = true;
                    PointUpdated?.Invoke(pt);
                }
            }
            IsDrawingPoint = false;
            CurrentPoint = null;
            StopAnimationScaling();
        }

        public void RemoveAllPoints(Canvas canvas)
        {
            Points.Clear();
            drawingService?.ClearRois(canvas, Points, CurrentPoint);
            PointsCleared?.Invoke();
        }

        public void RedrawAllPoints(Canvas canvas, bool showLabels = true)
        {
            drawingService?.DrawRois(canvas, Points.ToList(), CurrentPoint, showLabels);
        }

        public PointItem GetHitPointItem(Point pos, double tol = 10)
        {
            var hitItems = Points.Where(pt => (pt.Position - pos).Length < tol).ToList();
            return hitItems.OrderByDescending(i => i.ZIndex).FirstOrDefault();
        }
    }
} 