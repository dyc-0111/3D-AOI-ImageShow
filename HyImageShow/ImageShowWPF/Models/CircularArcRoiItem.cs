using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 圓弧ROI項目
    /// </summary>
    public class CircularArcRoiItem : BaseItem
    {
        private Point centerPoint;      // 圓心
        private Point startPoint;       // 起點
        private Point endPoint;         // 終點
        private double radius;          // 半徑
        private double startAngle;      // 起始角度
        private double endAngle;        // 結束角度
        private bool isLargeArc;        // 是否為大圓弧
        private bool sweepFlag;         // 掃描方向
        private double arcLength;       // 弧長
        private double chordLength;     // 弦長
        private bool isCompleted;       // 是否完成
        private bool isDraggingDot;     // 是否正在拖曳控制點
        private int draggingDotIndex;   // 正在拖曳的控制點索引
        private Point userMidPoint;     // 用戶指定的中點
        private SweepDirection sweepDirection;

        // UI元素
        public Path Path { get; set; }
        public Ellipse[] ControlDots { get; set; } = new Ellipse[3]; // 圓心、起點、終點
        public Border LabelBorder { get; set; }

        public CircularArcRoiItem()
        {
            centerPoint = new Point(0, 0);
            startPoint = new Point(0, 0);
            endPoint = new Point(0, 0);
            userMidPoint = new Point(0, 0);
            radius = 0;
            startAngle = 0;
            endAngle = 0;
            isLargeArc = false;
            sweepFlag = false;
            arcLength = 0;
            // ChordLength 現在是唯讀計算屬性，不需要手動賦值
            isCompleted = false;
            isDraggingDot = false;
            draggingDotIndex = -1;
        }

        /// <summary>
        /// 圓心
        /// </summary>
        public Point CenterPoint
        {
            get => centerPoint;
            set
            {
                if (centerPoint != value)
                {
                    centerPoint = value;
                    OnPropertyChanged(nameof(CenterPoint));
                    if (IsCompleted)
                    {
                        CalculateArcProperties();
                    }
                }
            }
        }

        /// <summary>
        /// 起點
        /// </summary>
        public Point StartPoint
        {
            get => startPoint;
            set
            {
                if (startPoint != value)
                {
                    startPoint = value;
                    OnPropertyChanged(nameof(StartPoint));
                    if (IsCompleted)
                    {
                        CalculateArcProperties();
                    }
                }
            }
        }

        /// <summary>
        /// 終點
        /// </summary>
        public Point EndPoint
        {
            get => endPoint;
            set
            {
                if (endPoint != value)
                {
                    endPoint = value;
                    OnPropertyChanged(nameof(EndPoint));
                    if (IsCompleted)
                    {
                        CalculateArcProperties();
                    }
                }
            }
        }

        /// <summary>
        /// 半徑
        /// </summary>
        public double Radius
        {
            get => radius;
            set
            {
                if (radius != value)
                {
                    radius = value;
                    OnPropertyChanged(nameof(Radius));
                }
            }
        }

        /// <summary>
        /// 起始角度（度）
        /// </summary>
        public double StartAngle
        {
            get => startAngle;
            set
            {
                if (startAngle != value)
                {
                    startAngle = value;
                    OnPropertyChanged(nameof(StartAngle));
                }
            }
        }

        /// <summary>
        /// 結束角度（度）
        /// </summary>
        public double EndAngle
        {
            get => endAngle;
            set
            {
                if (endAngle != value)
                {
                    endAngle = value;
                    OnPropertyChanged(nameof(EndAngle));
                }
            }
        }

        /// <summary>
        /// 是否為大弧
        /// </summary>
        public bool IsLargeArc
        {
            get => isLargeArc;
            set
            {
                if (isLargeArc != value)
                {
                    isLargeArc = value;
                    OnPropertyChanged(nameof(IsLargeArc));
                }
            }
        }

        /// <summary>
        /// 掃描方向
        /// </summary>
        public bool SweepFlag
        {
            get => sweepFlag;
            set
            {
                if (sweepFlag != value)
                {
                    sweepFlag = value;
                    OnPropertyChanged(nameof(SweepFlag));
                }
            }
        }

        /// <summary>
        /// 弧長
        /// </summary>
        public double ArcLength
        {
            get => arcLength;
            set
            {
                if (arcLength != value)
                {
                    arcLength = value;
                    OnPropertyChanged(nameof(ArcLength));
                }
            }
        }

        /// <summary>
        /// 弦長
        /// </summary>
        public double ChordLength => CalculateDistance(StartPoint, EndPoint);

        /// <summary>
        /// 是否已完成
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
        /// 是否正在拖曳控制點
        /// </summary>
        public bool IsDraggingDot
        {
            get => isDraggingDot;
            set
            {
                if (isDraggingDot != value)
                {
                    isDraggingDot = value;
                    OnPropertyChanged(nameof(IsDraggingDot));
                }
            }
        }

        /// <summary>
        /// 正在拖曳的控制點索引
        /// </summary>
        public int DraggingDotIndex
        {
            get => draggingDotIndex;
            set
            {
                if (draggingDotIndex != value)
                {
                    draggingDotIndex = value;
                    OnPropertyChanged(nameof(DraggingDotIndex));
                }
            }
        }

        /// <summary>
        /// 用戶指定的中點
        /// </summary>
        public Point UserMidPoint
        {
            get => userMidPoint;
            set
            {
                if (userMidPoint != value)
                {
                    userMidPoint = value;
                    OnPropertyChanged(nameof(UserMidPoint));
                    if (IsCompleted)
                    {
                        CalculateArcProperties();
                    }
                }
            }
        }

        /// <summary>
        /// 掃描方向
        /// </summary>
        public SweepDirection SweepDirection
        {
            get => sweepDirection;
            set
            {
                if (sweepDirection != value)
                {
                    sweepDirection = value;
                    OnPropertyChanged(nameof(SweepDirection));
                }
            }
        }

        // 額外的文字屬性（用於詳細顯示）
        public string ArcLengthText => $"弧長: {FormatValueText(ArcLength)}";
        public string ChordLengthText => $"弦長: {FormatValueText(ChordLength)}";
        public string RadiusText => $"半徑: {FormatValueText(Radius)}";

        // BaseItem 抽象成員實作
        public override string ItemType => "CircularArc";
        public override Point Center => CenterPoint;
        public override Size Size => new Size(2 * Radius, 2 * Radius);
        public override double Angle => StartAngle;
        public override string DisplayText => $"圓心: {FormatPointText(CenterPoint)} 半徑: {FormatValueText(Radius)} 弧長: {FormatValueText(ArcLength)}";
        public override string SimpleDisplayText => $"{FormatPointText(CenterPoint)} R{FormatValueText(Radius)} {FormatValueText(ArcLength)}";
        public override string Point1Text => $"起點: {FormatPointText(StartPoint)}";
        public override string Point2Text => $"終點: {FormatPointText(EndPoint)}";
        public override string Point3Text => $"中點: {FormatPointText(UserMidPoint)}";
        public override string CenterText => $"圓心: {FormatPointText(CenterPoint)}";
        public override string SizeText => $"弧長: {FormatValueText(ArcLength)} | 弦長: {FormatValueText(ChordLength)}";
        public override void ClearUIElements()
        {
            Path = null;
            ControlDots = new Ellipse[3];
            LabelBorder = null;
        }
        public override void ResetDragState()
        {
            IsDraggingDot = false;
            DraggingDotIndex = -1;
        }
        public override void CalculateProperties()
        {
            if (IsCompleted)
            {
                CalculateArcProperties();
            }
        }

        /// <summary>
        /// 計算圓弧屬性
        /// </summary>
        public void CalculateArcProperties()
        {
            if (IsCompleted)
            {
                // 根據三個點計算圓心（外心）
                Point center = CalculateCircumcenter(StartPoint, EndPoint, UserMidPoint);
                if (center != CenterPoint)
                {
                    centerPoint = center; // 直接設 field，避免 setter 遞迴
                    OnPropertyChanged(nameof(CenterPoint));
                }
                // 計算半徑
                Radius = CalculateDistance(center, StartPoint);
                // 計算三個點相對於圓心的角度
                double angle1 = CalculateAngle(center, StartPoint);
                double angle2 = CalculateAngle(center, EndPoint);
                double angle3 = CalculateAngle(center, UserMidPoint);
                OnPropertyChanged(nameof(StartPoint));
                OnPropertyChanged(nameof(EndPoint));
                OnPropertyChanged(nameof(UserMidPoint));
                // 保持用戶的點擊順序：StartPoint是起點，EndPoint是終點，UserMidPoint是弧上的一點
                StartAngle = angle1;
                EndAngle = angle2;
                // 判斷 UserMidPoint 是否在 Start->End 的順時針弧段上
                double sweep = ((angle2 - angle1 + 360) % 360);
                double midRel = ((angle3 - angle1 + 360) % 360);
                bool isClockwise = (midRel > 0 && midRel < sweep);
                // WPF 的 SweepDirection: Clockwise = 1, Counterclockwise = 0
                SweepDirection = isClockwise ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
                // 重新計算弧段角度（根據方向）
                double arcAngle = isClockwise ? sweep : ((angle1 - angle2 + 360) % 360);
                IsLargeArc = arcAngle > 180;
                // 計算弧長
                ArcLength = (arcAngle * Math.PI * Radius) / 180;
            }
        }

        /// <summary>
        /// 根據三個點計算圓心（外心）
        /// </summary>
        private Point CalculateCircumcenter(Point p1, Point p2, Point p3)
        {
            // 計算三條邊的中垂線交點
            // 邊1: p1到p2
            // 邊2: p2到p3
            
            // 邊1的中點
            Point mid1 = new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
            // 邊2的中點
            Point mid2 = new Point((p2.X + p3.X) / 2, (p2.Y + p3.Y) / 2);
            
            // 邊1的方向向量
            Vector dir1 = new Vector(p2.X - p1.X, p2.Y - p1.Y);
            // 邊2的方向向量
            Vector dir2 = new Vector(p3.X - p2.X, p3.Y - p2.Y);
            
            // 邊1的中垂線方向（垂直於邊1）
            Vector perp1 = new Vector(-dir1.Y, dir1.X);
            // 邊2的中垂線方向（垂直於邊2）
            Vector perp2 = new Vector(-dir2.Y, dir2.X);
            
            // 正規化方向向量
            perp1.Normalize();
            perp2.Normalize();
            
            // 計算中垂線的交點
            double det = perp1.X * perp2.Y - perp1.Y * perp2.X;
            
            if (Math.Abs(det) < 1e-10)
            {
                // 三點共線，無法形成圓
                // 返回三點的重心作為圓心
                return new Point((p1.X + p2.X + p3.X) / 3, (p1.Y + p2.Y + p3.Y) / 3);
            }
            
            double dx = mid2.X - mid1.X;
            double dy = mid2.Y - mid1.Y;
            
            double t1 = (dx * perp2.Y - dy * perp2.X) / det;
            
            // 計算圓心
            Point center = new Point(mid1.X + t1 * perp1.X, mid1.Y + t1 * perp1.Y);
            
            return center;
        }

        /// <summary>
        /// 計算點相對於圓心的角度（度）
        /// </summary>
        private double CalculateAngle(Point center, Point point)
        {
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            return angle < 0 ? angle + 360 : angle;
        }

        /// <summary>
        /// 根據三點計算圓弧
        /// </summary>
        public void CalculateFromThreePoints(Point center, Point start, Point end)
        {
            CenterPoint = center;
            StartPoint = start;
            EndPoint = end;

            // 計算半徑
            Radius = CalculateDistance(center, start);

            // 計算角度
            StartAngle = CalculateAngle(center, start);
            EndAngle = CalculateAngle(center, end);

            // 確保角度在正確範圍內
            if (EndAngle < StartAngle)
            {
                EndAngle += 360;
            }

            CalculateArcProperties();
        }

        /// <summary>
        /// 強制觸發所有相關屬性的變更通知
        /// </summary>
        public void ForceUpdateAllProperties()
        {
            OnPropertyChanged(nameof(StartPoint));
            OnPropertyChanged(nameof(EndPoint));
            OnPropertyChanged(nameof(UserMidPoint));
            OnPropertyChanged(nameof(CenterPoint));
            OnPropertyChanged(nameof(Radius));
            OnPropertyChanged(nameof(StartAngle));
            OnPropertyChanged(nameof(EndAngle));
            OnPropertyChanged(nameof(ArcLength));
            OnPropertyChanged(nameof(ChordLength));
            OnPropertyChanged(nameof(IsLargeArc));
            OnPropertyChanged(nameof(SweepFlag));
            OnPropertyChanged(nameof(IsCompleted));
        }

        /// <summary>
        /// 計算兩點之間的距離
        /// </summary>
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        /// <summary>
        /// 格式化點座標文字
        /// </summary>
        private string FormatPointText(Point point)
        {
            return $"({point.X:F1}, {point.Y:F1})";
        }

        /// <summary>
        /// 格式化數值文字
        /// </summary>
        private string FormatValueText(double value)
        {
            return $"{value:F1}";
        }
    }
} 