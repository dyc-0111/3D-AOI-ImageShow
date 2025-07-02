using System.Windows.Controls;

namespace HyImageShow.ImageShowWPF.View.RoiCards
{
    /// <summary>
    /// BezierArcRoiCard.xaml 的互動邏輯
    /// </summary>
    public partial class BezierArcRoiCard : UserControl
    {
        public BezierArcRoiCard()
        {
            InitializeComponent();
            this.DataContextChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("BezierArcRoiCard DataContext: " + (this.DataContext?.GetType().Name ?? "null"));
            };
        }
    }
} 