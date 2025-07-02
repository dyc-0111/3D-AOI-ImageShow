using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HyImageShow.ImageShowWPF.Services
{
    public abstract class BaseRoiService<TModel, TDrawingService> 
        where TModel : class 
        where TDrawingService : BaseRoiDrawingService<TModel>
    {
        protected List<TModel> roiItems = new List<TModel>();
        protected TModel currentRoi;
        protected Canvas mainCanvas;
        protected TDrawingService drawingService;

        public IReadOnlyList<TModel> RoiItems => roiItems;
        public TModel CurrentRoi => currentRoi;

        public event Action<TModel> RoiCompleted;
        public event Action<TModel> RoiUpdated;
        public event Action<TModel> RoiRemoved;
        public event Action RoisCleared;

        public virtual void SetMainCanvas(Canvas canvas) => mainCanvas = canvas;
        public virtual void SetDrawingService(TDrawingService service) => drawingService = service;

        public virtual void AddRoi(TModel roi)
        {
            roiItems.Add(roi);
            RoiUpdated?.Invoke(roi);
        }

        public virtual void RemoveRoi(TModel roi)
        {
            if (roiItems.Remove(roi))
                RoiRemoved?.Invoke(roi);
        }

        public virtual void ClearRois()
        {
            roiItems.Clear();
            RoisCleared?.Invoke();
        }

        // 可擴充通用互動方法
    }
}