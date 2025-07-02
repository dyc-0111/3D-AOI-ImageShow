using System.Collections.Generic;
using System.Windows;

namespace HyImageShow.ImageShowWPF.Data
{
    public enum RoiType
    {
        RotRect,
        Polygon,
        Ellipse,
        Circle,
        Line,
        Ruler,
        BezierArc,
        CircularArc
    }

    public class RoiData
    {
        public RoiType Type { get; set; }
        public double? CenterX { get; set; }
        public double? CenterY { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Angle { get; set; }
        public List<Point> Points { get; set; }
    }
} 