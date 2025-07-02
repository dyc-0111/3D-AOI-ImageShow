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
        public DataTemplate PointTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null) return null;
            var typeProp = item.GetType().GetProperty("ItemType");
            var type = typeProp?.GetValue(item)?.ToString();
            switch (type)
            {
                case "Line":
                    return LineTemplate;
                case "Ruler":
                    return RulerTemplate;
                case "Ellipse":
                    return EllipseTemplate;
                case "Polygon":
                    return PolygonTemplate;
                case "RotRect":
                    return RotRectTemplate;
                case "BezierArc":
                    return BezierArcTemplate;
                case "CircularArc":
                    return CircularArcTemplate;
                case "Point":
                    return PointTemplate;
                default:
                    return PointTemplate;
            }
        }
    }
} 