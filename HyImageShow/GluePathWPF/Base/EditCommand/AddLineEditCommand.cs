namespace HyImageShow
{
    public class AddLineEditCommand : IEditCommand
    {
        public AddLineEditCommand(LineLayer layer, LinePoint point)
        {
            lineLayer = layer;
            linePoint = point;
        }

        public string Name => $"Add Line: Layer{lineLayer.Name}, Line:{linePoint.LineIndex}";

        private LineLayer lineLayer;
        private LinePoint linePoint;

        public void DoRedo()
        {
            lineLayer.Lines.Add(linePoint);
        }

        public void DoUndo()
        {
            lineLayer.Lines.Remove(linePoint);
        }
    }
}
