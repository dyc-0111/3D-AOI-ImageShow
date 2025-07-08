using System;
using System.Collections.Generic;
using System.Windows;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 提供ROI限制在Canvas邊界內的靜態方法
    /// </summary>
    public static class CanvasBoundaryHelper
    {
        /// <summary>
        /// 限制一個點在Canvas內
        /// </summary>
        public static Point ClampPoint(Point pt, double minX, double minY, double maxX, double maxY)
        {
            double x = Math.Max(minX, Math.Min(maxX, pt.X));
            double y = Math.Max(minY, Math.Min(maxY, pt.Y));
            return new Point(x, y);
        }

        /// <summary>
        /// 限制圓形ROI在Canvas內（回傳修正後的圓心與半徑）
        /// </summary>
        public static (Point center, double radius) ClampCircle(Point center, double radius, double minX, double minY, double maxX, double maxY)
        {
            double maxRadiusX = Math.Min(center.X - minX, maxX - center.X);
            double maxRadiusY = Math.Min(center.Y - minY, maxY - center.Y);
            double maxRadius = Math.Max(0, Math.Min(maxRadiusX, maxRadiusY));
            double newRadius = Math.Min(radius, maxRadius);
            double clampedX = Math.Max(minX + newRadius, Math.Min(maxX - newRadius, center.X));
            double clampedY = Math.Max(minY + newRadius, Math.Min(maxY - newRadius, center.Y));
            Point newCenter = new Point(clampedX, clampedY);
            return (newCenter, newRadius);
        }

        /// <summary>
        /// 嚴格限制圓形ROI完全在邊界內（確保圓的邊緣不超出範圍）
        /// </summary>
        public static (Point center, double radius) ClampCircleStrict(Point center, double radius, double minX, double minY, double maxX, double maxY)
        {
            // 確保圓心至少距離邊界 radius 的距離
            double safeBoundMinX = minX + radius;
            double safeBoundMinY = minY + radius;
            double safeBoundMaxX = maxX - radius;
            double safeBoundMaxY = maxY - radius;
            
            // 如果邊界太小，縮小半徑
            double maxPossibleRadius = Math.Min((maxX - minX) / 2, (maxY - minY) / 2);
            double actualRadius = Math.Min(radius, maxPossibleRadius);
            
            // 重新計算安全邊界
            safeBoundMinX = minX + actualRadius;
            safeBoundMinY = minY + actualRadius;
            safeBoundMaxX = maxX - actualRadius;
            safeBoundMaxY = maxY - actualRadius;
            
            // 限制圓心在安全邊界內
            double clampedX = Math.Max(safeBoundMinX, Math.Min(safeBoundMaxX, center.X));
            double clampedY = Math.Max(safeBoundMinY, Math.Min(safeBoundMaxY, center.Y));
            
            Point newCenter = new Point(clampedX, clampedY);
            return (newCenter, actualRadius);
        }

        /// <summary>
        /// 限制線段兩端點在Canvas內
        /// </summary>
        public static (Point p1, Point p2) ClampLine(Point p1, Point p2, double minX, double minY, double maxX, double maxY)
        {
            Point newP1 = ClampPoint(p1, minX, minY, maxX, maxY);
            Point newP2 = ClampPoint(p2, minX, minY, maxX, maxY);
            return (newP1, newP2);
        }

        /// <summary>
        /// 限制多邊形所有頂點在Canvas內
        /// </summary>
        public static List<Point> ClampPolygon(List<Point> points, double minX, double minY, double maxX, double maxY)
        {
            var newPoints = new List<Point>(points.Count);
            foreach (var pt in points)
            {
                newPoints.Add(ClampPoint(pt, minX, minY, maxX, maxY));
            }
            return newPoints;
        }
    }
} 