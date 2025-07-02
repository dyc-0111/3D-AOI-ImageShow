using System.Collections.Generic;
using System.Windows.Controls;

namespace HyImageShow.ImageShowWPF.Services
{
    public abstract class BaseRoiDrawingService<TModel>
        where TModel : class
    {
        protected Canvas mainCanvas;

        public virtual void SetMainCanvas(Canvas canvas) => mainCanvas = canvas;

        public virtual void DrawRois(Canvas canvas, List<TModel> rois, TModel currentRoi, bool showLabels = true)
        {
            ClearRois(canvas, rois, currentRoi);
            foreach (var roi in rois)
                DrawSingleRoi(roi, canvas, showLabels);
            if (currentRoi != null && !rois.Contains(currentRoi))
                DrawPreviewRoi(currentRoi, canvas, showLabels);
        }

        public abstract void DrawSingleRoi(TModel roi, Canvas canvas, bool showLabels = true);

        public virtual void DrawPreviewRoi(TModel roi, Canvas canvas, bool showLabels = true)
        {
        }

        public virtual void ClearRois(Canvas canvas, IEnumerable<TModel> rois)
        {
        }

        public virtual void ClearRois(Canvas canvas, IEnumerable<TModel> rois, TModel currentRoi = null)
        {
        }

        public virtual void UpdateRoiVisual(TModel roi)
        {
        }
    }
} 