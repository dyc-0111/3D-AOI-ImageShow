using System.Windows;
using Hyperbrid.UIX.WinForms;

namespace HyImageShow
{
    public class GluePathEditorElementHost : ElementHost
    {
        private FrameworkElement host;

        protected override FrameworkElement InitialChild()
        {
            if (host == null)
            {
                host = new GluePathEditorView();

                host.DataContext = new GluePathEditorViewModel();

                (host as GluePathEditorView).InitViewModelEvent();
            }

            return host;
        }
    }
}
