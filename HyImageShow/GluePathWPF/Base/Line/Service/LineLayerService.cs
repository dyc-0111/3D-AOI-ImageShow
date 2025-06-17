using System;
using System.Collections.Generic;
using System.Linq;

namespace HyImageShow
{
    public class LineLayerService
    {
        public void AddLine(LineLayer lineLayer)
        {
            var linePoint = CreateLinePoint(lineLayer);
            lineLayer.Lines.Add(linePoint);
            lineLayer.AddUndoHistory?.Invoke(new AddLineEditCommand(lineLayer, linePoint), true);
        }

        public void AddLine(LineLayer lineLayer, LinePoint linePoint)
        {
            lineLayer.Lines.Add(linePoint);

            lineLayer.AddUndoHistory?.Invoke(new AddLineEditCommand(lineLayer, linePoint), true);
        }

        public void AddLines(LineLayer lineLayer, params LinePoint[] linePoints)
        {
            foreach (var linePoint in linePoints)
            {
                lineLayer.Lines.Add(linePoint);

                lineLayer.AddUndoHistory?.Invoke(new AddLineEditCommand(lineLayer, linePoint), true);
            }
        }

        private LinePoint CreateLinePoint(LineLayer lineLayer)
        {
            var baseLinePoint = new LinePoint
            {
                ParentLayer = lineLayer,
                LineIndex = lineLayer.Lines.Count,
                StartX = 360,
                StartY = 360,
                StartXDist = 0,
                StartYDist = 0,
                StartXEdge = 0,
                StartYEdge = 0,
                EndX = 360,
                EndY = 360,
                EndXDist = 0,
                EndYDist = 0,
                EndXEdge = 0,
                EndYEdge = 0,
                p1_XEdgeType = enuEdgeType.Unknow,
                p1_YEdgeType = enuEdgeType.Unknow
            };

            switch (lineLayer.GetDrawingMode())
            {
                case EDrawingMode.Line:
                    baseLinePoint.LinePointType = ELinePointType.Line;
                    baseLinePoint.EndX = 260;
                    baseLinePoint.EndY = 260;
                    break;
                case EDrawingMode.Point:
                    baseLinePoint.LinePointType = ELinePointType.Point;
                    break;
                case EDrawingMode.AlignmentPoint:
                    baseLinePoint.LinePointType = ELinePointType.Align;
                    break;
            }

            return baseLinePoint;
        }

        public void DeleteLine(LineLayer lineLayer)
        {
            List<LinePoint> selectedPts = new List<LinePoint>();

            foreach (var line in lineLayer.Lines.Where(x => x.IsSelected).ToList())
            {
                selectedPts.Add(line);
                lineLayer.Lines.Remove(line);
            }

            List<IndexChange> indexChanges = new List<IndexChange>();

            // 重新整理 LineIndex
            for (int i = 0; i < lineLayer.Lines.Count; i++)
            {
                if (lineLayer.Lines[i].LineIndex != i)
                {
                    indexChanges.Add(new IndexChange(lineLayer.Lines[i].LineIndex, i));
                }

                lineLayer.Lines[i].LineIndex = i;
            }

            lineLayer.AddUndoHistory.Invoke(new RemoveLineEditCommand(lineLayer, selectedPts, indexChanges), true);

            lineLayer.AfterLineChangeIndexAction.Invoke();
        }

        public void MoveLineUp(LineLayer lineLayer)
        {
            if (lineLayer.Lines.Where(x => x.IsSelected) == null
                || !lineLayer.Lines.Any(x => x.IsSelected))
                return;

            var firstSelected = lineLayer.Lines.First(x => x.IsSelected);
            int index = lineLayer.Lines.IndexOf(firstSelected);
            if (index > 0)
            {
                lineLayer.Lines.Move(index, index - 1);

                // 更新 LineIndex 
                UpdateLineIndices(lineLayer);
            }

            lineLayer.AfterLineChangeIndexAction.Invoke();
        }

        private void UpdateLineIndices(LineLayer lineLayer)
        {
            for (int i = 0; i < lineLayer.Lines.Count; i++)
            {
                lineLayer.Lines[i].LineIndex = i; // 確保觸發 PropertyChanged
            }
        }

        public bool CanMoveLine(LineLayer lineLayer, Func<int, bool> condition)
        {
            var selectedLine = lineLayer.Lines.FirstOrDefault(x => x.IsSelected);
            if (selectedLine == null) return false;

            int index = lineLayer.Lines.IndexOf(selectedLine);
            return condition(index);
        }



        public bool CanMoveLineUp(LineLayer lineLayer) =>
            CanMoveLine(lineLayer, index => index > 0);

        public bool CanMoveLineDown(LineLayer lineLayer) =>
            CanMoveLine(lineLayer, index => index < lineLayer.Lines.Count - 1);

        public void MoveLineDown(LineLayer lineLayer)
        {
            if (lineLayer.Lines.Where(x => x.IsSelected) == null
                || !lineLayer.Lines.Any(x => x.IsSelected))
                return;

            var firstSelected = lineLayer.Lines.First(x => x.IsSelected);
            int index = lineLayer.Lines.IndexOf(firstSelected);
            if (index < lineLayer.Lines.Count - 1)
            {
                lineLayer.Lines.Move(index, index + 1);

                // 更新 LineIndex
                UpdateLineIndices(lineLayer);
            }

            lineLayer.AfterLineChangeIndexAction.Invoke();
        }
    }
}
