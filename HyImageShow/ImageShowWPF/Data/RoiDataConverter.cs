using System;
using System.Collections.Generic;
using System.Windows;
using HyImageShow.ImageShowWPF.Models;

namespace HyImageShow.ImageShowWPF.Data
{
    public static class RoiDataConverter
    {
        // RectRoiItem <-> RoiData
        public static RoiData FromRectRoiItem(
                RectRoiItem item,
                Func<Point, Point> displayToImageConverter,
                Func<double, double> widthConverter = null,
                Func<double, double> heightConverter = null)
        {
            var center = displayToImageConverter != null ? displayToImageConverter(item.CenterPoint) : item.CenterPoint;
            double width = widthConverter != null ? widthConverter(item.Width) : item.Width;
            double height = heightConverter != null ? heightConverter(item.Height) : item.Height;
            return new RoiData
            {
                Type = RoiType.RotRect,
                CenterX = center.X,
                CenterY = center.Y,
                Width = width,
                Height = height,
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
        public static RoiData FromPolygonRoiItem(PolygonRoiItem item, System.Func<Point, Point> displayToImageConverter)
        {
            var points = displayToImageConverter != null ? item.Points.ConvertAll(p => displayToImageConverter(p)) : new List<Point>(item.Points);
            return new RoiData
            {
                Type = RoiType.Polygon,
                Points = points,
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
        public static RoiData FromEllipseRoiItem(EllipseRoiItem item, System.Func<Point, Point> displayToImageConverter, System.Func<double, double> lengthConverter = null)
        {
            var center = displayToImageConverter != null ? displayToImageConverter(item.CenterPoint) : item.CenterPoint;
            double diameter = lengthConverter != null ? lengthConverter(item.Radius * 2) : item.Radius * 2;
            return new RoiData
            {
                Type = RoiType.Ellipse,
                CenterX = center.X,
                CenterY = center.Y,
                Width = diameter,
                Height = diameter
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
        public static RoiData FromCircularArcRoiItem(CircularArcRoiItem item, System.Func<Point, Point> displayToImageConverter, System.Func<double, double> lengthConverter = null)
        {
            var center = displayToImageConverter != null ? displayToImageConverter(item.CenterPoint) : item.CenterPoint;
            var start = displayToImageConverter != null ? displayToImageConverter(item.StartPoint) : item.StartPoint;
            var end = displayToImageConverter != null ? displayToImageConverter(item.EndPoint) : item.EndPoint;
            var mid = displayToImageConverter != null ? displayToImageConverter(item.UserMidPoint) : item.UserMidPoint;
            double diameter = lengthConverter != null ? lengthConverter(item.Radius * 2) : item.Radius * 2;
            return new RoiData
            {
                Type = RoiType.CircularArc,
                CenterX = center.X,
                CenterY = center.Y,
                Width = diameter,
                Height = diameter,
                Angle = item.StartAngle,
                Points = new List<Point> { start, end, mid }
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
        public static RoiData FromRulerItem(RulerItem item, System.Func<Point, Point> displayToImageConverter)
        {
            var p1 = displayToImageConverter != null ? displayToImageConverter(item.Point1) : item.Point1;
            var p2 = displayToImageConverter != null ? displayToImageConverter(item.Point2) : item.Point2;
            return new RoiData
            {
                Type = RoiType.Ruler,
                Points = new List<Point> { p1, p2 }
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
        public static RoiData FromBezierArcRoiItem(BezierArcRoiItem item, System.Func<Point, Point> displayToImageConverter)
        {
            var start = displayToImageConverter != null ? displayToImageConverter(item.StartPoint) : item.StartPoint;
            var mid = displayToImageConverter != null ? displayToImageConverter(item.MiddlePoint) : item.MiddlePoint;
            var end = displayToImageConverter != null ? displayToImageConverter(item.EndPoint) : item.EndPoint;
            return new RoiData
            {
                Type = RoiType.BezierArc,
                Points = new List<Point> { start, mid, end }
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
        public static RoiData FromLineItem(LineItem item, System.Func<Point, Point> displayToImageConverter)
        {
            var p1 = displayToImageConverter != null ? displayToImageConverter(item.P1) : item.P1;
            var p2 = displayToImageConverter != null ? displayToImageConverter(item.P2) : item.P2;
            return new RoiData
            {
                Type = RoiType.Line,
                Points = new List<Point> { p1, p2 }
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
        public static RoiData FromPointItem(PointItem item, System.Func<Point, Point> displayToImageConverter)
        {
            var pos = displayToImageConverter != null ? displayToImageConverter(item.Position) : item.Position;
            return new RoiData
            {
                Type = RoiType.Point,
                Points = new List<Point> { pos }
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