using System;
using System.Collections.Generic;
using System.Linq;

namespace HyImageShow
{
    public class PointEditCommand : IEditCommand
    {
        public PointEditCommand(
            LineLayer layer,
            List<LinePoint> oldPoints,
            List<LinePoint> newPoints,
            Action<LinePoint> updateLine)
        {
            lineLayer = layer;

            oldLinePoints = new List<LinePoint>();
            newLinePoints = new List<LinePoint>();

            int i = 0;

            foreach (var pts in oldPoints)
            {
                oldLinePoints.Add(new LinePoint());
                oldLinePoints[i].CopyFrom(pts);
                i++;
            }

            i = 0;

            foreach (var pts in newPoints)
            {
                newLinePoints.Add(new LinePoint());
                newLinePoints[i].CopyFrom(pts);
                i++;
            }

            UpdateLine = updateLine;
        }

        public PointEditCommand(
                LineLayer layer,
                LinePoint oldPoint,
                LinePoint newPoint,
                Action<LinePoint> updateLine)
        {
            lineLayer = layer;

            oldLinePoints = new List<LinePoint>();
            newLinePoints = new List<LinePoint>();

            oldLinePoints.Add(oldPoint);
            newLinePoints.Add(newPoint);

            Console.WriteLine($"PointEditCommand: " +
                $"({oldPoint.StartX},{oldPoint.StartY}) -> ({newPoint.StartX},{newPoint.StartY})");
            Console.WriteLine($"PointEditCommand: " +
                $"({oldPoint.EndX},{oldPoint.EndY}) -> ({newPoint.EndX},{newPoint.EndY})");

            UpdateLine = updateLine;
        }

        public string Name => $"Edit Point: Layer{lineLayer.Name} Lines Change.";

        private LineLayer lineLayer;
        private List<LinePoint> oldLinePoints;
        private List<LinePoint> newLinePoints;

        private Action<LinePoint> UpdateLine { get; set; }

        public void DoRedo()
        {
            foreach (var pts in newLinePoints)
            {
                var line = lineLayer.Lines.Where(x => x.LineIndex == pts.LineIndex).ToList().FirstOrDefault();

                if (line != null)
                {
                    line.CopyFrom(pts);

                    UpdateLine.Invoke(line);
                }
            }
        }

        public void DoUndo()
        {
            foreach (var pts in oldLinePoints)
            {
                var line = lineLayer.Lines.Where(x => x.LineIndex == pts.LineIndex).ToList().FirstOrDefault();

                if (line != null)
                {
                    line.CopyFrom(pts);

                    UpdateLine.Invoke(line);
                }
            }
        }
    }
}
