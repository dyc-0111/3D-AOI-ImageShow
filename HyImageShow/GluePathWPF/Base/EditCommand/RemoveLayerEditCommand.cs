using System.Collections.Generic;
using System.Linq;

namespace HyImageShow
{
    public class RemoveLayerEditCommand : IEditCommand
    {
        public RemoveLayerEditCommand(
            GluePathEditorViewModel model, 
            LineLayer layer, 
            List<IndexChange> changes = null)
        {
            viewModel = model;
            lineLayer = layer;

            if (changes != null)
                indexChanges = changes;
        }

        public string Name => $"Remove Layer: Layer{lineLayer.Name}";

        private GluePathEditorViewModel viewModel;
        private LineLayer lineLayer;
        private List<IndexChange> indexChanges;

        public void SetIndexChanges(List<IndexChange> changes)
        {
            indexChanges = changes;
        }

        public void DoRedo()
        {
            viewModel.Layers.Remove(lineLayer);

            // 重新整理 LineIndex
            for (int i = 0; i < viewModel.Layers.Count; i++)
            {
                viewModel.Layers[i].Name = i.ToString();
            }
        }

        public void DoUndo()
        {
            foreach (var change in indexChanges)
            {
                viewModel.Layers[change.AfterIndex].Name = change.BeforeIndex.ToString();
            }

            viewModel.Layers.Add(lineLayer);

            var sortLayers = viewModel.Layers.OrderBy(x => int.Parse(x.Name)).ToList();

            viewModel.Layers.Clear();

            foreach (var layer in sortLayers)
            {
                viewModel.Layers.Add(layer);
            }
        }
    }
}
