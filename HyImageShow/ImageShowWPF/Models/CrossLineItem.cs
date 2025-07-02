using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 十字線項目類
    /// </summary>
    public class CrossLineItem : BaseItem
    {
        private Point center;
        private double length;
        private double angle;
        private Color lineColor = Colors.Red;
        private double lineThickness = 2;

        /// <summary>
        /// 中心點
        /// </summary>
        public override Point Center => Center;

        public override Size Size => new Size(Length, Length);

        /// <summary>
        /// 十字線長度
        /// </summary>
        public double Length
        {
            get => length;
            set { length = value; OnPropertyChanged(nameof(Length)); }
        }

        /// <summary>
        /// 十字線旋轉角度（度）
        /// </summary>
        public override double Angle => Angle;


        /// <summary>
        /// 線條顏色
        /// </summary>
        public Color LineColor
        {
            get => lineColor;
            set { lineColor = value; OnPropertyChanged(nameof(LineColor)); }
        }

        /// <summary>
        /// 線條粗細
        /// </summary>
        public double LineThickness
        {
            get => lineThickness;
            set { lineThickness = value; OnPropertyChanged(nameof(LineThickness)); }
        }

        /// <summary>
        /// 水平線 reference
        /// </summary>
        public Line HorizontalLine { get; set; }

        /// <summary>
        /// 垂直線 reference
        /// </summary>
        public Line VerticalLine { get; set; }

        public override string ItemType => "CrossLine";
        public override string DisplayText => $"中心: {FormatPointText(Center)} 長度: {FormatValueText(Length)} 角度: {FormatValueText(Angle)}°";
        public override string SimpleDisplayText => $"{FormatPointText(Center)} L:{FormatValueText(Length)} {FormatValueText(Angle)}°";
        public override void ClearUIElements()
        {
            HorizontalLine = null;
            VerticalLine = null;
        }
        public override void ResetDragState() { }
        public override void CalculateProperties() { }

       
    }
} 