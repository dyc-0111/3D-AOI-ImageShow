using System;
using System.Windows;
using System.Windows.Controls;

namespace HyImageShow.ImageShowWPF.View.RoiCards
{
    public class RoiCardTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LineTemplate { get; set; }
        public DataTemplate RulerTemplate { get; set; }
        public DataTemplate EllipseTemplate { get; set; }
        public DataTemplate PolygonTemplate { get; set; }
        public DataTemplate RotRectTemplate { get; set; }
        public DataTemplate BezierArcTemplate { get; set; }
        public DataTemplate CircularArcTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return null;
            var typeProp = item.GetType().GetProperty("ItemType");
            var type = typeProp?.GetValue(item)?.ToString();
            switch (type)
            {
                case "Line":
                case "線段":
                    return LineTemplate;
                case "Ruler":
                case "量尺":
                    return RulerTemplate;
                case "Ellipse":
                case "橢圓":
                    return EllipseTemplate;
                case "Polygon":
                case "多邊形":
                    return PolygonTemplate;
                case "RotRect":
                case "旋轉矩形":
                    return RotRectTemplate;
                case "BezierArc":
                case "貝塞爾弧":
                    return BezierArcTemplate;
                case "CircularArc":
                case "圓弧":
                    return CircularArcTemplate;
                default:
                    return LineTemplate;
            }
        }
    }
} 