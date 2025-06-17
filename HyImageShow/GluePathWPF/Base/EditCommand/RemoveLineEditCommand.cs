using System.Collections.Generic;
using System.Linq;

namespace HyImageShow
{
    public class RemoveLineEditCommand : IEditCommand
    {
        public RemoveLineEditCommand(LineLayer layer, List<LinePoint> points, List<IndexChange> changes)
        {
            lineLayer = layer;
            linePoints = points;
            indexChanges = changes;
        }

        public string Name
        {
            get
            {
                string name = $"Remove Line: Layer{lineLayer.Name}, ";

                foreach (var line in linePoints)
                {
                    name += $"Line: {line.LineIndex} ";
                }

                return name;
            }
        }


        private LineLayer lineLayer;
        private List<LinePoint> linePoints;
        private List<IndexChange> indexChanges;

        public void DoRedo()
        {
            foreach (var linePoint in linePoints)
            {
                lineLayer.Lines.Remove(linePoint);
            }

            // 重新整理 LineIndex
            for (int i = 0; i < lineLayer.Lines.Count; i++)
            {
                lineLayer.Lines[i].LineIndex = i;
            }
        }

        public void DoUndo()
        {
            foreach (var change in indexChanges)
            {
                lineLayer.Lines[change.AfterIndex].LineIndex = change.BeforeIndex;
            }

            foreach (var linePoint in linePoints)
            {
                lineLayer.Lines.Add(linePoint);
            }

            var sortLines = lineLayer.Lines.OrderBy(x => x.LineIndex).ToList();

            lineLayer.Lines.Clear();

            foreach (var line in sortLines)
            {
                lineLayer.Lines.Add(line);
            }
        }
    }
}
