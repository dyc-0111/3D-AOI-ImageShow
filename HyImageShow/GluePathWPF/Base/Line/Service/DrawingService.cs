using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow
{
    public class DrawingService
    {
        public void DrawLine(
            Dictionary<LinePoint, Line> lineDictionary,
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            Dictionary<LinePoint, (TextBlock, Line, Line)> lineScaleDictionary,
            LinePoint linePoint,
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            MouseButtonEventHandler lineMouseLeftButtonDown,
            MouseEventHandler lineMouseMove,
            MouseButtonEventHandler lineMouseLeftButtonUp,
            MouseButtonEventHandler pointMouseLeftButtonDown,
            MouseEventHandler pointMouseMove,
            MouseButtonEventHandler pointMouseLeftButtonUp,
            Canvas canvas,
            ref (LinePoint Line, Ellipse StartPoint, Ellipse EndPoint) lastShowPoints)
        {
            if (!lineDictionary.ContainsKey(linePoint))
            {
                var line = DrawPath.CreateLine(
                    linePoint,
                    getSelectedSolidColorBrush.Invoke());

                line.MouseLeftButtonDown += lineMouseLeftButtonDown;
                line.MouseMove += lineMouseMove;
                line.MouseLeftButtonUp += lineMouseLeftButtonUp;
                canvas.Children.Add(line);
                lineDictionary[linePoint] = line;

                if (linePoint.LinePointType == ELinePointType.Point
                    || linePoint.LinePointType == ELinePointType.Align)
                {
                    ShowPoint(
                        pointDictionary,
                        linePoint,
                        getSelectedSolidColorBrush,
                        pointMouseLeftButtonDown,
                        pointMouseMove,
                        pointMouseLeftButtonUp,
                        false, 
                        canvas, 
                        ref lastShowPoints);
                }
            }
            else
            {
                UpdateLine(lineDictionary, pointDictionary, linePoint);
            }

            RefreshScale(lineScaleDictionary, linePoint, canvas);
        }

        public void UpdateLine(
            Dictionary<LinePoint, Line> lineDictionary,
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            LinePoint linePoint)
        {
            if (lineDictionary.TryGetValue(linePoint, out var line))
            {
                line.X1 = linePoint.StartX;
                line.Y1 = linePoint.StartY;
                line.X2 = linePoint.EndX;
                line.Y2 = linePoint.EndY;
            }

            if (pointDictionary.TryGetValue(linePoint, out var points))
            {
                var (startPoint, endPoint) = points;

                DrawPath.SetCanvasPosition(
                    startPoint,
                    linePoint.StartX - startPoint.Width / 2,
                    linePoint.StartY - startPoint.Height / 2);

                DrawPath.SetCanvasPosition(
                    endPoint,
                    linePoint.EndX - endPoint.Width / 2,
                    linePoint.EndY - endPoint.Height / 2);
            }
        }

        public void RemoveLine(
            Dictionary<LinePoint, Line> lineDictionary,
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            Dictionary<LinePoint, (TextBlock text, Line tick1, Line tick2)> lineScaleDictionary,
            LinePoint linePoint,
            Canvas canvas)
        {
            if (lineDictionary.TryGetValue(linePoint, out var line))
            {
                canvas.Children.Remove(line);
                lineDictionary.Remove(linePoint);
            }

            if (pointDictionary.TryGetValue(linePoint, out var points))
            {
                var (startPoint, endPoint) = points;
                canvas.Children.Remove(startPoint);
                canvas.Children.Remove(endPoint);
                pointDictionary.Remove(linePoint);
            }

            RemoveScale(lineScaleDictionary, linePoint, canvas);
        }

        public void UpdateRelativeLines(ObservableCollection<LinePoint> lines)
        {
            var allLines = lines
                .OrderBy(l => l.LineIndex) // 確保順序正確
                .ToList();

            LinePoint latestAlign = null;

            foreach (var _line in allLines)
            {
                if (_line.LinePointType == ELinePointType.Align)
                {
                    latestAlign = _line;
                }
                else if (_line.LinePointType == ELinePointType.Line
                    || _line.LinePointType == ELinePointType.Point)
                {
                    if (latestAlign != null)
                    {
                        _line.UpdateRelative(latestAlign);
                    }
                    else
                    {
                        _line.ResetRelative();
                    }
                }
            }
        }

        public void RefreshScale(
            Dictionary<LinePoint, (TextBlock, Line, Line)> lineScaleDictionary,
            LinePoint linePoint,
            Canvas canvas)
        {
            if (linePoint.LinePointType == ELinePointType.Line)
            {
                if (linePoint.ShowScale)
                {
                    DrawScale(lineScaleDictionary, linePoint, canvas);
                }
                else
                {
                    RemoveScale(lineScaleDictionary, linePoint, canvas);
                }
            }
        }


        private void DrawScale(
            Dictionary<LinePoint, (TextBlock, Line, Line)> lineScaleDictionary,
            LinePoint linePoint,
            Canvas canvas)
        {
            if (!lineScaleDictionary.ContainsKey(linePoint))
            {
                var (text, tick1, tick2) = DrawPath.DrawScale(linePoint, canvas);
                canvas.Children.Add(text);
                canvas.Children.Add(tick1);
                canvas.Children.Add(tick2);
                lineScaleDictionary[linePoint] = (text, tick1, tick2);
            }
            else
            {
                UpdateScale(lineScaleDictionary, linePoint, canvas);
            }
        }

        private void UpdateScale(
            Dictionary<LinePoint, (TextBlock, Line, Line)> lineScaleDictionary,
            LinePoint linePoint,
            Canvas canvas)
        {
            if (lineScaleDictionary.TryGetValue(linePoint, out var items))
            {
                canvas.Children.Remove(items.Item1);
                canvas.Children.Remove(items.Item2);
                canvas.Children.Remove(items.Item3);

                var (updateText, updateTick1, updateTick2) = DrawPath.DrawScale(linePoint, canvas);
                canvas.Children.Add(updateText);
                canvas.Children.Add(updateTick1);
                canvas.Children.Add(updateTick2);

                lineScaleDictionary[linePoint] = (updateText, updateTick1, updateTick2);
            }
        }

        private void RemoveScale(
            Dictionary<LinePoint, (TextBlock, Line, Line)> lineScaleDictionary,
            LinePoint linePoint,
            Canvas canvas)
        {
            if (lineScaleDictionary.TryGetValue(linePoint, out var items))
            {
                canvas.Children.Remove(items.Item1);
                canvas.Children.Remove(items.Item2);
                canvas.Children.Remove(items.Item3);

                lineScaleDictionary.Remove(linePoint);
            }
        }

        public void ShowPoint(
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            LinePoint linePoint,
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            MouseButtonEventHandler pointMouseLeftButtonDown,
            MouseEventHandler pointMouseMove,
            MouseButtonEventHandler pointMouseLeftButtonUp,
            bool isLine,
            Canvas canvas,
            ref (LinePoint Line, Ellipse StartPoint, Ellipse EndPoint) lastShowPoints)
        {
            var startPoint = DrawPath.CreatePoint(
                linePoint, EPointType.Start, getSelectedSolidColorBrush.Invoke());

            var endPoint = DrawPath.CreatePoint(
                linePoint, EPointType.End, getSelectedSolidColorBrush.Invoke());

            startPoint.MouseLeftButtonDown += pointMouseLeftButtonDown;
            startPoint.MouseMove += pointMouseMove;
            startPoint.MouseLeftButtonUp += pointMouseLeftButtonUp;

            endPoint.MouseLeftButtonDown += pointMouseLeftButtonDown;
            endPoint.MouseMove += pointMouseMove;
            endPoint.MouseLeftButtonUp += pointMouseLeftButtonUp;

            canvas.Children.Add(startPoint);
            canvas.Children.Add(endPoint);

            pointDictionary[linePoint] = (startPoint, endPoint);

            if (isLine)
                lastShowPoints = (linePoint, startPoint, endPoint);
        }

        private void ShowVisiblePoint(
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> visiblePointDictionary,
            LinePoint linePoint,
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            Canvas canvas)
        {
            var startPoint = DrawPath.CreatePoint(
                linePoint, EPointType.Start, getSelectedSolidColorBrush.Invoke());

            var endPoint = DrawPath.CreatePoint(
                linePoint, EPointType.End, getSelectedSolidColorBrush.Invoke());

            startPoint.Opacity = 0.2;
            endPoint.Opacity = 0.2;

            canvas.Children.Add(startPoint);
            canvas.Children.Add(endPoint);

            visiblePointDictionary[linePoint] = (startPoint, endPoint);
        }

        public void RemoveShowPoints(
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            (LinePoint Line, Ellipse StartPoint, Ellipse EndPoint) lastShowPoints,
            Canvas canvas)
        {
            if (lastShowPoints.Line != null
                && lastShowPoints.StartPoint != null
                && lastShowPoints.EndPoint != null)
            {
                canvas.Children.Remove(lastShowPoints.StartPoint);
                canvas.Children.Remove(lastShowPoints.EndPoint);
                pointDictionary.Remove(lastShowPoints.Line);
            }
        }

        public void DrawVisibleLine(
            Dictionary<LinePoint, Line> visibleLineDictionary,
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> visiblePointDictionary,
            LinePoint linePoint,
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            Canvas canvas)
        {
            if (!visibleLineDictionary.ContainsKey(linePoint))
            {
                var line = DrawPath.CreateLine(linePoint, getSelectedSolidColorBrush.Invoke());

                line.Opacity = 0.2;

                canvas.Children.Add(line);

                visibleLineDictionary[linePoint] = line;

                if (linePoint.LinePointType == ELinePointType.Point
                    || linePoint.LinePointType == ELinePointType.Align)
                {
                    ShowVisiblePoint(visiblePointDictionary, linePoint, getSelectedSolidColorBrush, canvas);
                }
            }
        }

        public void RemoveInvisibleLine(
            Dictionary<LinePoint, Line> visibleLineDictionary,
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> visiblePointDictionary,
            LinePoint linePoint,
            Canvas canvas)
        {
            if (visibleLineDictionary.TryGetValue(linePoint, out var line))
            {
                canvas.Children.Remove(line);
                visibleLineDictionary.Remove(linePoint);
            }

            if (visiblePointDictionary.TryGetValue(linePoint, out var points))
            {
                var (startPoint, endPoint) = points;
                canvas.Children.Remove(startPoint);
                canvas.Children.Remove(endPoint);
                visiblePointDictionary.Remove(linePoint);
            }
        }

        public void ResetDrawings(
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            Func<SolidColorBrush> getTempSelectedSolidColorBrush,
            Canvas canvas, bool isTempChange = false)
        {
            var selectedBrush = isTempChange ?
                getTempSelectedSolidColorBrush.Invoke() :
                getSelectedSolidColorBrush.Invoke();

            foreach (var child in canvas.Children)
            {
                if (child is Ellipse p)
                {
                    if ((p.Tag as LinePoint)?.LinePointType == ELinePointType.Align)
                    {
                        p.Fill = Brushes.Salmon;
                        p.StrokeThickness = DrawPath._alignPointSize;
                    }
                    else
                    {
                        p.Fill = selectedBrush;
                    }
                }

                if (child is Line l)
                {
                    if (!l.Tag.Equals("Tick"))
                    {
                        l.Stroke = selectedBrush;
                        l.StrokeThickness = DrawPath.lineStrokeThickness;
                    }
                }
            }
        }


        private void UpdatePointAppearance(Ellipse point, Brush stroke, Brush fill)
        {
            point.Stroke = stroke;
            point.Fill = fill;

            Panel.SetZIndex(point, fill.Equals(Brushes.Red) ? int.MaxValue : 0);
        }

        public void UpdateLineAppearance(Line line, Brush stroke)
        {
            line.Stroke = stroke;

            Panel.SetZIndex(line, stroke.Equals(Brushes.Red) ? int.MaxValue : 0);
        }

        public void SetDrawingAppearance(
            Dictionary<LinePoint, Line> lineDictionary,
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            LinePoint linePoint,
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            bool isSelected)
        {
            SetLineAppearance(lineDictionary, linePoint, getSelectedSolidColorBrush, isSelected);
            SetPointAppearance(pointDictionary, linePoint, getSelectedSolidColorBrush, isSelected);
        }

        public void SetPointAppearance(
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            LinePoint linePoint,
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            bool isSelected)
        {
            if (linePoint.LinePointType != ELinePointType.Point &&
                linePoint.LinePointType != ELinePointType.Align)
                return;

            if (!pointDictionary.TryGetValue(linePoint, out var points))
                return;

            Brush startStrokeBrush = Brushes.Red;
            Brush fillBrush = Brushes.Red;
            Brush endStrokeBrush = Brushes.Red;

            if (!isSelected)
            {
                startStrokeBrush = DrawPath.GetStartStrokeBrush(linePoint.LinePointType);
                fillBrush = DrawPath.GetFillBrush(linePoint.LinePointType, getSelectedSolidColorBrush.Invoke());
                endStrokeBrush = DrawPath.GetEndStrokeBrush(linePoint.LinePointType);
            }

            // StartPoint
            UpdatePointAppearance(points.StartPoint, startStrokeBrush, fillBrush);
            // EndPoint
            UpdatePointAppearance(points.EndPoint, endStrokeBrush, fillBrush);
        }

        private void SetLineAppearance(
            Dictionary<LinePoint, Line> lineDictionary,
            LinePoint linePoint,
            Func<SolidColorBrush> getSelectedSolidColorBrush,
            bool isSelected)
        {
            if (linePoint.LinePointType == ELinePointType.Line)
            {
                if (lineDictionary.TryGetValue(linePoint, out Line line))
                {
                    if (isSelected)
                        UpdateLineAppearance(line, Brushes.Red);
                    else
                        UpdateLineAppearance(line, getSelectedSolidColorBrush.Invoke());
                }
            }
        }

        public void AlignSelectedLines(
            Dictionary<LinePoint, Line> lineDictionary,
            Dictionary<LinePoint, (Ellipse StartPoint, Ellipse EndPoint)> pointDictionary,
            LineLayer layer,
            EDrawingAlignType alignType)
        {
            if (layer?.SelectedLines == null || !layer.SelectedLines.Any())
                return;

            // 計算基準位置
            double referencePos = 0;

            switch (alignType)
            {
                case EDrawingAlignType.Left:
                    referencePos = layer.SelectedLines.SelectMany(
                        line => new[] { line.StartX }).Min();
                    break;

                case EDrawingAlignType.Right:
                    referencePos = layer.SelectedLines.SelectMany(
                        line => new[] { line.EndX }).Max();
                    break;

                case EDrawingAlignType.Top:
                    referencePos = layer.SelectedLines.SelectMany(
                        line => new[] { line.StartY }).Min();
                    break;

                case EDrawingAlignType.Down:
                    referencePos = layer.SelectedLines.SelectMany(
                        line => new[] { line.EndY }).Max();
                    break;
            }

            double offsetX = 0;
            double offsetY = 0;

            // 更新選中項目的位置
            foreach (var linePoint in layer.SelectedLines)
            {
                switch (alignType)
                {
                    case EDrawingAlignType.Left:
                        offsetX = referencePos - linePoint.StartX;
                        break;

                    case EDrawingAlignType.Right:
                        offsetX = referencePos - linePoint.EndX;
                        break;

                    case EDrawingAlignType.Top:
                        offsetY = referencePos - linePoint.StartY;
                        break;

                    case EDrawingAlignType.Down:
                        offsetY = referencePos - linePoint.EndY;
                        break;
                }

                linePoint.StartX += offsetX;
                linePoint.EndX += offsetX;
                linePoint.StartY += offsetY;
                linePoint.EndY += offsetY;

                // 更新畫布上的線條
                UpdateLine(lineDictionary, pointDictionary, linePoint);
            }

            // 更新相對線條
            UpdateRelativeLines(layer.Lines);

        }

    }


    public enum EDrawingAlignType
    {
        Left,
        Right,
        Top,
        Down,
    }
}
