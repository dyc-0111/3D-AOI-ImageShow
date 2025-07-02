using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 多邊形ROI項目類別
    /// </summary>
    public class PolygonRoiItem : BaseItem
    {
        private List<Point> points;
        private double angle;
        private bool isCompleted;
        private bool isDraggingBody;
        private bool isDraggingRotate;
        private bool isDraggingPoint;
        private int draggingPointIndex;
        private Point lastDragPos;
        private Point dragStart;
        private List<Point> dragStartPoints;

        public PolygonRoiItem()
        {
            Points = new List<Point>();
            RotationAngle = 0;
            IsCompleted = false;
            IsDraggingBody = false;
            IsDraggingRotate = false;
            IsDraggingPoint = false;
            DraggingPointIndex = -1;
            LastDragPos = new Point(0, 0);
            DragStart = new Point(0, 0);
            DragStartPoints = new List<Point>();
        }

        /// <summary>
        /// 多邊形頂點列表
        /// </summary>
        public List<Point> Points
        {
            get => points;
            set
            {
                if (points != value)
                {
                    points = value;
                    OnPropertyChanged(nameof(Points));
                    OnPropertiesChanged(nameof(CenterPoint), nameof(SizeValue), nameof(Area), nameof(Perimeter), nameof(VertexCount), nameof(DisplayText), nameof(SimpleDisplayText), 
                        nameof(Point1Text), nameof(Point2Text), nameof(Point3Text));
                }
            }
        }

        /// <summary>
        /// 旋轉角度（度）
        /// </summary>
        public double RotationAngle
        {
            get => angle;
            set
            {
                if (angle != value)
                {
                    angle = value;
                    OnPropertyChanged(nameof(RotationAngle));
                    OnPropertiesChanged(nameof(DisplayText), nameof(SimpleDisplayText), nameof(Point3Text));
                }
            }
        }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsCompleted
        {
            get => isCompleted;
            set
            {
                if (isCompleted != value)
                {
                    isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        /// <summary>
        /// 是否正在拖曳整體
        /// </summary>
        public bool IsDraggingBody
        {
            get => isDraggingBody;
            set
            {
                if (isDraggingBody != value)
                {
                    isDraggingBody = value;
                    OnPropertyChanged(nameof(IsDraggingBody));
                }
            }
        }

        /// <summary>
        /// 是否正在拖曳旋轉
        /// </summary>
        public bool IsDraggingRotate
        {
            get => isDraggingRotate;
            set
            {
                if (isDraggingRotate != value)
                {
                    isDraggingRotate = value;
                    OnPropertyChanged(nameof(IsDraggingRotate));
                }
            }
        }

        /// <summary>
        /// 是否正在拖曳頂點
        /// </summary>
        public bool IsDraggingPoint
        {
            get => isDraggingPoint;
            set
            {
                if (isDraggingPoint != value)
                {
                    isDraggingPoint = value;
                    OnPropertyChanged(nameof(IsDraggingPoint));
                }
            }
        }

        /// <summary>
        /// 正在拖曳的頂點索引
        /// </summary>
        public int DraggingPointIndex
        {
            get => draggingPointIndex;
            set
            {
                if (draggingPointIndex != value)
                {
                    draggingPointIndex = value;
                    OnPropertyChanged(nameof(DraggingPointIndex));
                }
            }
        }

        /// <summary>
        /// 最後拖曳位置
        /// </summary>
        public Point LastDragPos
        {
            get => lastDragPos;
            set
            {
                if (lastDragPos != value)
                {
                    lastDragPos = value;
                    OnPropertyChanged(nameof(LastDragPos));
                }
            }
        }

        /// <summary>
        /// 拖曳開始位置
        /// </summary>
        public Point DragStart
        {
            get => dragStart;
            set
            {
                if (dragStart != value)
                {
                    dragStart = value;
                    OnPropertyChanged(nameof(DragStart));
                }
            }
        }

        /// <summary>
        /// 拖曳開始時的頂點列表
        /// </summary>
        public List<Point> DragStartPoints
        {
            get => dragStartPoints;
            set
            {
                if (dragStartPoints != value)
                {
                    dragStartPoints = value;
                    OnPropertyChanged(nameof(DragStartPoints));
                }
            }
        }

        /// <summary>
        /// 多邊形元素
        /// </summary>
        public Polyline Polygon { get; set; }

        /// <summary>
        /// 頂點控制點列表
        /// </summary>
        public List<Ellipse> PointDots { get; set; } = new List<Ellipse>();

        /// <summary>
        /// 旋轉控制點
        /// </summary>
        public Ellipse RotateDot { get; set; }

        /// <summary>
        /// 主標籤邊框
        /// </summary>
        public Border MainLabelBorder { get; set; }

        /// <summary>
        /// 頂點標籤邊框列表
        /// </summary>
        public List<Border> VertexLabelBorders { get; set; } = new List<Border>();

        /// <summary>
        /// 頂點縮放變換列表
        /// </summary>
        public List<ScaleTransform> PointScales { get; set; } = new List<ScaleTransform>();

        /// <summary>
        /// 旋轉縮放變換
        /// </summary>
        public ScaleTransform RotateScale { get; } = new ScaleTransform(1, 1);

        /// <summary>
        /// 中心點
        /// </summary>
        public Point CenterPoint
        {
            get
            {
                if (Points == null || Points.Count == 0)
                    return new Point(0, 0);

                double sumX = 0, sumY = 0;
                foreach (var point in Points)
                {
                    sumX += point.X;
                    sumY += point.Y;
                }
                return new Point(sumX / Points.Count, sumY / Points.Count);
            }
        }

        /// <summary>
        /// 大小
        /// </summary>
        public Size SizeValue
        {
            get
            {
                if (Points == null || Points.Count == 0)
                    return new Size(0, 0);

                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = double.MinValue, maxY = double.MinValue;

                foreach (var point in Points)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                }

                return new Size(maxX - minX, maxY - minY);
            }
        }

        /// <summary>
        /// 面積
        /// </summary>
        public double Area
        {
            get
            {
                if (Points == null || Points.Count < 3)
                    return 0;

                double area = 0;
                for (int i = 0; i < Points.Count; i++)
                {
                    int j = (i + 1) % Points.Count;
                    area += Points[i].X * Points[j].Y;
                    area -= Points[j].X * Points[i].Y;
                }
                return Math.Abs(area) / 2;
            }
        }

        /// <summary>
        /// 周長
        /// </summary>
        public double Perimeter => CalculatePerimeter();

        /// <summary>
        /// 頂點數量
        /// </summary>
        public int VertexCount => Points?.Count ?? 0;

        /// <summary>
        /// 中心點文字
        /// </summary>
        public string Point1Text => $"中心: {FormatPointText(CenterPoint)}";

        /// <summary>
        /// 角度文字
        /// </summary>
        public string Point2Text => $"角度: {FormatValueText(Angle)}°";

        /// <summary>
        /// 頂點座標文字
        /// </summary>
        public string Point3Text
        {
            get
            {
                if (Points == null || Points.Count == 0)
                    return "";
                
                // 計算旋轉後的頂點座標
                Point center = CenterPoint;
                var rotatedVertexTexts = Points.Select((p, i) => 
                {
                    Point rotatedPoint = RotatePoint(p, center, RotationAngle);
                    return $"P{i + 1}({rotatedPoint.X:F0},{rotatedPoint.Y:F0})";
                });
                return string.Join(", ", rotatedVertexTexts);
            }
        }

        /// <summary>
        /// 旋轉點座標
        /// </summary>
        private Point RotatePoint(Point point, Point center, double angle)
        {
            double angleRad = angle * Math.PI / 180;
            double cosAngle = Math.Cos(angleRad);
            double sinAngle = Math.Sin(angleRad);

            double dx = point.X - center.X;
            double dy = point.Y - center.Y;

            double rotatedX = center.X + (dx * cosAngle - dy * sinAngle);
            double rotatedY = center.Y + (dx * sinAngle + dy * cosAngle);

            return new Point(rotatedX, rotatedY);
        }

        public string PointsText => Point3Text;

        // BaseItem 抽象成員實作
        public override string ItemType => "多邊形";

        public override Point Center => CenterPoint;

        public override Size Size => SizeValue;

        public override double Angle => RotationAngle;

        public override string DisplayText => $"中心: {FormatPointText(CenterPoint)} 頂點: {VertexCount} 面積: {FormatValueText(Area)}";

        public override string SimpleDisplayText => $"{FormatPointText(CenterPoint)} {VertexCount}點 {FormatValueText(Area)}";

        public override void ClearUIElements()
        {
            Polygon = null;
            PointDots.Clear();
            RotateDot = null;
            MainLabelBorder = null;
            VertexLabelBorders.Clear();
            PointScales.Clear();
        }

        public override void ResetDragState()
        {
            IsDraggingBody = false;
            IsDraggingRotate = false;
            IsDraggingPoint = false;
            DraggingPointIndex = -1;
        }

        public override void CalculateProperties()
        {
            // 多邊形的屬性計算相對複雜，主要是在屬性變更時自動計算
            // 這裡可以添加額外的計算邏輯
        }

        /// <summary>
        /// 添加頂點
        /// </summary>
        /// <param name="point">頂點</param>
        public void AddPoint(Point point)
        {
            Points.Add(point);
            PointScales.Add(new ScaleTransform(1, 1));
            
            // 觸發屬性變更事件
            OnPropertyChanged(nameof(Points));
            OnPropertiesChanged(nameof(CenterPoint), nameof(SizeValue), nameof(Area), nameof(Perimeter), nameof(VertexCount), nameof(DisplayText), nameof(SimpleDisplayText), nameof(Point3Text));
        }

        /// <summary>
        /// 移除頂點
        /// </summary>
        /// <param name="index">頂點索引</param>
        public void RemovePoint(int index)
        {
            if (index >= 0 && index < Points.Count)
            {
                Points.RemoveAt(index);
                if (index < PointScales.Count)
                    PointScales.RemoveAt(index);
            }
        }

        /// <summary>
        /// 批次更新頂點
        /// </summary>
        /// <param name="newPoints">新的頂點列表</param>
        public void UpdatePointsBatch(List<Point> newPoints)
        {
            Points = newPoints;
            OnPropertyChanged(nameof(Points));
            OnPropertiesChanged(nameof(CenterPoint), nameof(SizeValue), nameof(Area), nameof(Perimeter), nameof(VertexCount), nameof(DisplayText), nameof(SimpleDisplayText), nameof(Point3Text));
        }

        /// <summary>
        /// 更新單個頂點
        /// </summary>
        /// <param name="index">頂點索引</param>
        /// <param name="newPoint">新的頂點位置</param>
        public void UpdatePoint(int index, Point newPoint)
        {
            if (index >= 0 && index < Points.Count && points[index] != newPoint)
            {
                points[index] = newPoint;
                OnPropertyChanged(nameof(Points));
                OnPropertiesChanged(nameof(CenterPoint), nameof(SizeValue), nameof(Area), nameof(Perimeter), nameof(DisplayText), nameof(SimpleDisplayText), nameof(Point3Text));
            }
        }

        private double CalculatePerimeter()
        {
            if (Points == null || Points.Count < 2)
                return 0;

            double perimeter = 0;
            for (int i = 0; i < Points.Count; i++)
            {
                int j = (i + 1) % Points.Count;
                perimeter += CalculateDistance(Points[i], Points[j]);
            }
            return perimeter;
        }

        // 旋轉互動用欄位
        public double RotateStartAngle { get; set; }
        public double RotateStartVectorAngle { get; set; }
    }

} 