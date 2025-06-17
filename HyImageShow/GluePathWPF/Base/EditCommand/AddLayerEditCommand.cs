namespace HyImageShow
{
    public class AddLayerEditCommand : IEditCommand
    {
        public AddLayerEditCommand(GluePathEditorViewModel model, LineLayer layer)
        {
            viewModel = model;
            lineLayer = layer;
        }

        public string Name => $"Add Layer: Layer{lineLayer.Name}";

        private GluePathEditorViewModel viewModel;
        private LineLayer lineLayer;

        public void DoRedo()
        {
            viewModel.Layers.Add(lineLayer);
        }

        public void DoUndo()
        {
            viewModel.Layers.Remove(lineLayer);
        }
    }
}
