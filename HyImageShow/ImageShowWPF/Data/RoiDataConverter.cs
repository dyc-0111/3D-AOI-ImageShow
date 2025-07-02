using System.Collections.Generic;
using System.Windows;
using HyImageShow.ImageShowWPF.Models;

namespace HyImageShow.ImageShowWPF.Data
{
    public static class RoiDataConverter
    {
        // RectRoiItem <-> RoiData
        public static RoiData FromRectRoiItem(RectRoiItem item)
        {
            return new RoiData
            {
                Type = RoiType.RotRect,
                CenterX = item.CenterPoint.X,
                CenterY = item.CenterPoint.Y,
                Width = item.Width,
                Height = item.Height,
                Angle = item.RotationAngle
            };
        }
        public static RectRoiItem ToRectRoiItem(RoiData data)
        {
            return new RectRoiItem
            {
                CenterPoint = new Point(data.CenterX ?? 0, data.CenterY ?? 0),
                Width = data.Width ?? 0,
                Height = data.Height ?? 0,
                RotationAngle = data.Angle ?? 0,
                IsCompleted = true
            };
        }

        // PolygonRoiItem <-> RoiData
        public static RoiData FromPolygonRoiItem(PolygonRoiItem item)
        {
            return new RoiData
            {
                Type = RoiType.Polygon,
                Points = new List<Point>(item.Points),
                Angle = item.RotationAngle
            };
        }
        public static PolygonRoiItem ToPolygonRoiItem(RoiData data)
        {
            return new PolygonRoiItem
            {
                Points = new List<Point>(data.Points ?? new List<Point>()),
                RotationAngle = data.Angle ?? 0,
                IsCompleted = true
            };
        }

        // EllipseRoiItem <-> RoiData
        public static RoiData FromEllipseRoiItem(EllipseRoiItem item)
        {
            return new RoiData
            {
                Type = RoiType.Ellipse,
                CenterX = item.CenterPoint.X,
                CenterY = item.CenterPoint.Y,
                Width = item.Radius * 2,
                Height = item.Radius * 2
            };
        }
        public static EllipseRoiItem ToEllipseRoiItem(RoiData data)
        {
            return new EllipseRoiItem
            {
                CenterPoint = new Point(data.CenterX ?? 0, data.CenterY ?? 0),
                Radius = (data.Width ?? 0) / 2,
                IsCompleted = true
            };
        }

        // CircularArcRoiItem <-> RoiData
        public static RoiData FromCircularArcRoiItem(CircularArcRoiItem item)
        {
            return new RoiData
            {
                Type = RoiType.CircularArc,
                CenterX = item.CenterPoint.X,
                CenterY = item.CenterPoint.Y,
                Width = item.Radius * 2,
                Height = item.Radius * 2,
                Angle = item.StartAngle,
                Points = new List<Point> { item.StartPoint, item.EndPoint, item.UserMidPoint }
            };
        }
        public static CircularArcRoiItem ToCircularArcRoiItem(RoiData data)
        {
            var arc = new CircularArcRoiItem
            {
                CenterPoint = new Point(data.CenterX ?? 0, data.CenterY ?? 0),
                Radius = (data.Width ?? 0) / 2,
                StartAngle = data.Angle ?? 0,
                IsCompleted = true
            };
            if (data.Points != null && data.Points.Count >= 3)
            {
                arc.StartPoint = data.Points[0];
                arc.EndPoint = data.Points[1];
                arc.UserMidPoint = data.Points[2];
            }
            return arc;
        }

        // RulerItem <-> RoiData
        public static RoiData FromRulerItem(RulerItem item)
        {
            return new RoiData
            {
                Type = RoiType.Ruler,
                Points = new List<Point> { item.Point1, item.Point2 }
            };
        }
        public static RulerItem ToRulerItem(RoiData data)
        {
            var ruler = new RulerItem { IsCompleted = true };
            if (data.Points != null && data.Points.Count >= 2)
            {
                ruler.Point1 = data.Points[0];
                ruler.Point2 = data.Points[1];
            }
            return ruler;
        }

        // BezierArcRoiItem <-> RoiData
        public static RoiData FromBezierArcRoiItem(BezierArcRoiItem item)
        {
            return new RoiData
            {
                Type = RoiType.BezierArc,
                Points = new List<Point> { item.StartPoint, item.MiddlePoint, item.EndPoint }
            };
        }
        public static BezierArcRoiItem ToBezierArcRoiItem(RoiData data)
        {
            var bezier = new BezierArcRoiItem { IsCompleted = true };
            if (data.Points != null && data.Points.Count >= 3)
            {
                bezier.StartPoint = data.Points[0];
                bezier.MiddlePoint = data.Points[1];
                bezier.EndPoint = data.Points[2];
            }
            return bezier;
        }

        // LineItem <-> RoiData
        public static RoiData FromLineItem(LineItem item)
        {
            return new RoiData
            {
                Type = RoiType.Line,
                Points = new List<Point> { item.P1, item.P2 }
            };
        }
        public static LineItem ToLineItem(RoiData data)
        {
            var line = new LineItem();
            if (data.Points != null && data.Points.Count >= 2)
            {
                line.P1 = data.Points[0];
                line.P2 = data.Points[1];
            }
            return line;
        }

        // PointItem <-> RoiData
        public static RoiData FromPointItem(PointItem item)
        {
            return new RoiData
            {
                Type = RoiType.Point,
                Points = new List<Point> { item.Position }
            };
        }
        public static PointItem ToPointItem(RoiData data)
        {
            if (data.Points != null && data.Points.Count >= 1)
            {
                return new PointItem(data.Points[0]);
            }
            return null;
        }
    }
} 