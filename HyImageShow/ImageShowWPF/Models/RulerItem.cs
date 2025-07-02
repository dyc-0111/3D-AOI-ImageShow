using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 尺規項目類別
    /// </summary>
    public class RulerItem : BaseItem
    {
        private Point point1;
        private Point point2;
        private bool isDraggingPoint1;
        private bool isDraggingPoint2;
        private bool isDraggingRuler;

        public RulerItem()
        {
            Id = GetNextId();
            Point1 = new Point(0, 0);
            Point2 = new Point(0, 0);
            IsCompleted = false;
            IsDraggingPoint1 = false;
            IsDraggingPoint2 = false;
            IsDraggingRuler = false;
            LastDragPos = new Point(0, 0);
        }

        /// <summary>
        /// 起點
        /// </summary>
        public Point Point1
        {
            get => point1;
            set
            {
                if (point1 != value)
                {
                    point1 = value;
                    OnPropertyChanged(nameof(Point1));
                    OnPropertiesChanged(nameof(Distance), nameof(DeltaX), nameof(DeltaY), nameof(MidPoint), nameof(RulerAngle), nameof(DisplayText), nameof(SimpleDisplayText), 
                        nameof(Point1Text), nameof(Point2Text), nameof(AngleText), nameof(SizeText), nameof(DistanceText));
                }
            }
        }

        /// <summary>
        /// 終點
        /// </summary>
        public Point Point2
        {
            get => point2;
            set
            {
                if (point2 != value)
                {
                    point2 = value;
                    OnPropertyChanged(nameof(Point2));
                    OnPropertiesChanged(nameof(Distance), nameof(DeltaX), nameof(DeltaY), nameof(MidPoint), nameof(RulerAngle), nameof(DisplayText), nameof(SimpleDisplayText), 
                        nameof(Point1Text), nameof(Point2Text), nameof(AngleText), nameof(SizeText), nameof(DistanceText));
                }
            }
        }

        /// <summary>
        /// 是否正在拖曳起點
        /// </summary>
        public bool IsDraggingPoint1
        {
            get => isDraggingPoint1;
            set
            {
                if (isDraggingPoint1 != value)
                {
                    isDraggingPoint1 = value;
                    OnPropertyChanged(nameof(IsDraggingPoint1));
                }
            }
        }

        /// <summary>
        /// 是否正在拖曳終點
        /// </summary>
        public bool IsDraggingPoint2
        {
            get => isDraggingPoint2;
            set
            {
                if (isDraggingPoint2 != value)
                {
                    isDraggingPoint2 = value;
                    OnPropertyChanged(nameof(IsDraggingPoint2));
                }
            }
        }

        /// <summary>
        /// 是否正在拖曳尺規
        /// </summary>
        public bool IsDraggingRuler
        {
            get => isDraggingRuler;
            set
            {
                if (isDraggingRuler != value)
                {
                    isDraggingRuler = value;
                    OnPropertyChanged(nameof(IsDraggingRuler));
                }
            }
        }

        /// <summary>
        /// 距離
        /// </summary>
        public double Distance => CalculateDistance(Point1, Point2);

        /// <summary>
        /// X方向差值
        /// </summary>
        public double DeltaX => Point2.X - Point1.X;

        /// <summary>
        /// Y方向差值
        /// </summary>
        public double DeltaY => Point2.Y - Point1.Y;

        /// <summary>
        /// 中點
        /// </summary>
        public Point MidPoint => CalculateMidPoint(Point1, Point2);

        /// <summary>
        /// 角度（度）
        /// </summary>
        public double RulerAngle
        {
            get => CalculateAngle(Point1, Point2);
        }

        /// <summary>
        /// 距離文字（詳細資訊）
        /// </summary>
        public string DistanceText => $"長度: {FormatValueText(Distance)} (ΔX: {FormatValueText(DeltaX, "F0")}, ΔY: {FormatValueText(DeltaY, "F0")})";

        /// <summary>
        /// 起點文字
        /// </summary>
        public string Point1Text => FormatPointText(Point1);

        /// <summary>
        /// 終點文字
        /// </summary>
        public string Point2Text => FormatPointText(Point2);

        /// <summary>
        /// 角度文字
        /// </summary>
        public string AngleText => $"角度: {FormatValueText(RulerAngle)}°";

        /// <summary>
        /// 距離文字（用於 SizeText 綁定）
        /// </summary>
        public string SizeText => $"距離: {FormatValueText(Distance)}";

        // UI元素
        public Line Line { get; set; }
        public Ellipse Point1Dot { get; set; }
        public Ellipse Point2Dot { get; set; }
        public Border LabelBorder { get; set; }
        public Border Point1Label { get; set; }
        public Border Point2Label { get; set; }
        public Polygon Arrow { get; set; }
        public Ellipse ArrowDot { get; set; }
        public ScaleTransform Point1Scale { get; } = new ScaleTransform(1, 1);
        public ScaleTransform Point2Scale { get; } = new ScaleTransform(1, 1);

        // BaseItem 抽象成員實作
        public override string ItemType => "量尺";

        public override Point Center => CalculateMidPoint(Point1, Point2);

        public override Size Size => new Size(Distance, 0);

        public override double Angle => RulerAngle;

        public override string DisplayText => $"距離: {FormatValueText(Distance)} 角度: {FormatValueText(RulerAngle)}°";

        public override string SimpleDisplayText => $"{FormatValueText(Distance)} {FormatValueText(RulerAngle)}°";

        public override void ClearUIElements()
        {
            Line = null;
            Point1Dot = null;
            Point2Dot = null;
            LabelBorder = null;
            Point1Label = null;
            Point2Label = null;
            Arrow = null;
            ArrowDot = null;
        }

        public override void ResetDragState()
        {
            IsDraggingPoint1 = false;
            IsDraggingPoint2 = false;
            IsDraggingRuler = false;
        }

        public override void CalculateProperties()
        {
            // 尺規的屬性計算相對簡單，主要是在屬性變更時自動計算
            // 這裡可以添加額外的計算邏輯
        }

        private static int nextId = 1;
        private static int GetNextId()
        {
            return nextId++;
        }
    }
} 