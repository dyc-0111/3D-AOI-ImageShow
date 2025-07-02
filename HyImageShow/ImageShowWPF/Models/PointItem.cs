using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 點型 ROI 項目
    /// </summary>
    public class PointItem : BaseItem
    {
        private Point position;

        public PointItem(Point pos)
        {
            Position = pos;
        }

        /// <summary>
        /// 點座標
        /// </summary>
        public Point Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    OnPropertiesChanged(nameof(Position), nameof(Center), nameof(DisplayText), nameof(SimpleDisplayText));
                }
            }
        }

        public override string ItemType => "Point";
        public override Point Center => Position;
        public override Size Size => new Size(0, 0);
        public override double Angle => 0;
        public override string DisplayText => $"座標: {FormatPointText(Position)}";
        public override string SimpleDisplayText => $"({Position.X:F0},{Position.Y:F0})";
        public Ellipse EllipseElement { get; set; }
        public Border LabelBorder { get; set; }
        public ScaleTransform PointScale { get; } = new ScaleTransform(1, 1);
        public override void ClearUIElements() { /* 無需特別處理 */ }
        public override void ResetDragState() { /* 無需特別處理 */ }
        public override void CalculateProperties() { /* 無需特別處理 */ }
    }
} 