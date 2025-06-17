using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HyImageShow
{
    public class DrawPath
    {
        public const double lineStrokeThickness = 5;
        public const double selectedLineStrokeThickness = 5; //選中的線段不放大
        public const double _pointSize = 5;
        public const double _alignPointSize = 15;

        public static double GetPointSize(ELinePointType type)
        {
            return type == ELinePointType.Align ?
                    _alignPointSize : _pointSize;
        }

        public static Brush GetStartStrokeBrush(ELinePointType type)
        {
            return type == ELinePointType.Align ?
                   Brushes.Salmon :
                   Brushes.Blue;
        }

        public static Brush GetFillBrush(ELinePointType type, Brush selectedBrush)
        {
            return type == ELinePointType.Align ?
                   Brushes.Salmon : selectedBrush;
        }

        public static Brush GetEndStrokeBrush(ELinePointType type)
        {
            return type == ELinePointType.Align ?
                   Brushes.Salmon : Brushes.Green;
        }


        public static Line CreateLine(
            Point start, Point end, Brush stroke)
        {
            return new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = stroke,
                StrokeThickness = lineStrokeThickness,
            };
        }

        public static Line CreateLine(LinePoint line, Brush stroke)
        {
            return new Line
            {
                X1 = line.StartX,
                Y1 = line.StartY,
                X2 = line.EndX,
                Y2 = line.EndY,
                Stroke = stroke,
                StrokeThickness = lineStrokeThickness,
                Tag = line,
            };
        }

        public static (Storyboard storyboard, Line line) CreateLineAnimation(
            Line line, Point start, Point end, double durationInSeconds)
        {
            var storyboard = new Storyboard();

            var x2Animation = new DoubleAnimation
            {
                From = start.X,
                To = end.X,
                Duration = TimeSpan.FromSeconds(durationInSeconds),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            var y2Animation = new DoubleAnimation
            {
                From = start.Y,
                To = end.Y,
                Duration = TimeSpan.FromSeconds(durationInSeconds),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(x2Animation, line);
            Storyboard.SetTargetProperty(x2Animation, new PropertyPath(Line.X2Property));
            Storyboard.SetTarget(y2Animation, line);
            Storyboard.SetTargetProperty(y2Animation, new PropertyPath(Line.Y2Property));

            storyboard.Children.Add(x2Animation);
            storyboard.Children.Add(y2Animation);

            return (storyboard, line);
        }


        public static Ellipse CreatePoint(
                LinePoint linePoint,
                EPointType type,
                Brush selectedBrush,
                double pointScale = 1)
        {
            var pointConfig = GetPointConfiguration(linePoint.LinePointType, type, selectedBrush, pointScale);

            var point = new Ellipse
            {
                Width = pointConfig.Size,
                Height = pointConfig.Size,
                Stroke = pointConfig.StrokeBrush,
                Fill = pointConfig.FillBrush,
                Tag = linePoint,
            };

            double x = type == EPointType.Start ? linePoint.StartX : linePoint.EndX;
            double y = type == EPointType.Start ? linePoint.StartY : linePoint.EndY;

            SetCanvasPosition(point, x, y, pointConfig.Size);

            return point;
        }

        public static Ellipse CreatePoint(
               ELinePointType lineType,
               EPointType type,
               Point position,
               Brush selectedBrush,
               double pointScale)
        {
            var pointConfig = GetPointConfiguration(lineType, type, selectedBrush, pointScale);

            var point = new Ellipse
            {
                Width = pointConfig.Size,
                Height = pointConfig.Size,
                Stroke = pointConfig.StrokeBrush,
                Fill = pointConfig.FillBrush,
            };

            SetCanvasPosition(point, position, pointConfig.Size);

            return point;
        }

        public static void SetCanvasPosition(
            UIElement element, Point position, double size = 0)
        {
            Canvas.SetLeft(element, position.X - size / 2);
            Canvas.SetTop(element, position.Y - size / 2);
        }

        public static void SetCanvasPosition(
            UIElement element, double x, double y, double size = 0)
        {
            Canvas.SetLeft(element, x - size / 2);
            Canvas.SetTop(element, y - size / 2);
        }

        private static PointConfiguration GetPointConfiguration(
            ELinePointType lineType, EPointType type, Brush selectedBrush, double pointScale)
        {
            return new PointConfiguration
            {
                Size = GetPointSize(lineType) * pointScale,

                StrokeBrush = type == EPointType.Start ?
                        GetStartStrokeBrush(lineType) :
                        GetEndStrokeBrush(lineType),

                FillBrush = GetFillBrush(lineType, selectedBrush),
            };
        }

        public static (TextBlock, Line, Line) DrawScale(LinePoint linePoint, Canvas canvas)
        {
            // 計算方向與垂直向量
            Vector direction = new Vector(linePoint.EndX - linePoint.StartX, linePoint.EndY - linePoint.StartY);
            direction.Normalize();
            Vector normal = new Vector(-direction.Y, direction.X); // 垂直向量

            //刻度長度
            double tickLength = 5;

            //起點刻度
            var tick1 = GetTickLine(new Point(linePoint.StartX, linePoint.StartY), normal, tickLength);

            //終點刻度
            var tick2 = GetTickLine(new Point(linePoint.EndX, linePoint.EndY), normal, tickLength);

            // 顯示距離文字
            double distance =
                Math.Sqrt(Math.Pow(linePoint.EndXInMm - linePoint.StartXInMm, 2) +
                          Math.Pow(linePoint.EndYInMm - linePoint.StartYInMm, 2));

            var text = new TextBlock
            {
                Text = $"{Math.Round(distance, 3)} mm",
                Foreground = Brushes.Blue,
                FontSize = 12
            };

            // 文字放在中間稍微偏上
            Point mid = new Point(
                (linePoint.StartX + linePoint.EndX) / 2, 
                (linePoint.StartY + linePoint.EndY) / 2);

            Canvas.SetLeft(text, mid.X + 4);
            Canvas.SetTop(text, mid.Y - 20);

            return (text, tick1, tick2);
        }

        private static Line GetTickLine(Point position, Vector normal, double length)
        {
            var tick = new Line
            {
                Tag = "Tick",
                X1 = position.X - normal.X * length,
                Y1 = position.Y - normal.Y * length,
                X2 = position.X + normal.X * length,
                Y2 = position.Y + normal.Y * length,
                Stroke = Brushes.Blue,
                StrokeThickness = 1
            };

            return tick;
        }
    }
}
