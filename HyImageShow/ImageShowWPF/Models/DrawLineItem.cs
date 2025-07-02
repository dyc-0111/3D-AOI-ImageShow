using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 畫線項目類
    /// </summary>
    public class LineItem : BaseItem
    {
        private static int _nextId = 1;
        private Point p1;
        private Point p2;
        private bool isDraggingPoint1;
        private bool isDraggingPoint2;
        private bool isDraggingLine;

        public LineItem()
        {
            Id = _nextId++;
            Point1Scale = new ScaleTransform(1, 1);
            Point2Scale = new ScaleTransform(1, 1);
        }

        /// <summary>
        /// 起點
        /// </summary>
        public Point P1
        {
            get => p1;
            set
            {
                if (p1 != value)
                {
                    p1 = value;
                    OnPropertyChanged(nameof(P1));
                    OnPropertiesChanged(nameof(Center), nameof(Size), nameof(Angle), nameof(DisplayText), nameof(SimpleDisplayText), 
                        nameof(Length), nameof(Point1Text), nameof(Point2Text));
                }
            }
        }

        /// <summary>
        /// 終點
        /// </summary>
        public Point P2
        {
            get => p2;
            set
            {
                if (p2 != value)
                {
                    p2 = value;
                    OnPropertyChanged(nameof(P2));
                    OnPropertiesChanged(nameof(Center), nameof(Size), nameof(Angle), nameof(DisplayText), nameof(SimpleDisplayText), 
                        nameof(Length), nameof(Point1Text), nameof(Point2Text));
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
        /// 是否正在拖曳線條
        /// </summary>
        public bool IsDraggingLine
        {
            get => isDraggingLine;
            set
            {
                if (isDraggingLine != value)
                {
                    isDraggingLine = value;
                    OnPropertyChanged(nameof(IsDraggingLine));
                }
            }
        }

        /// <summary>
        /// 線條元素
        /// </summary>
        public Line Line { get; set; }

        /// <summary>
        /// 起點元素
        /// </summary>
        public Ellipse Point1 { get; set; }

        /// <summary>
        /// 終點元素
        /// </summary>
        public Ellipse Point2 { get; set; }

        /// <summary>
        /// 起點標籤
        /// </summary>
        public Border Point1Label { get; set; }

        /// <summary>
        /// 終點標籤
        /// </summary>
        public Border Point2Label { get; set; }

        public Border Label { get; set; }
        /// <summary>
        /// 起點縮放變換
        /// </summary>
        public ScaleTransform Point1Scale { get; }

        /// <summary>
        /// 終點縮放變換
        /// </summary>
        public ScaleTransform Point2Scale { get; }

        /// <summary>
        /// 起點圓點（用於新的繪製服務）
        /// </summary>
        public Ellipse StartDot { get; set; }

        /// <summary>
        /// 終點圓點（用於新的繪製服務）
        /// </summary>
        public Ellipse EndDot { get; set; }

        /// <summary>
        /// 起點縮放變換（用於新的繪製服務）
        /// </summary>
        public ScaleTransform StartScale => Point1Scale;

        /// <summary>
        /// 終點縮放變換（用於新的繪製服務）
        /// </summary>
        public ScaleTransform EndScale => Point2Scale;

        /// <summary>
        /// 標籤邊框（用於新的繪製服務）
        /// </summary>
        public Border LabelBorder { get; set; }

        /// <summary>
        /// 中點
        /// </summary>
        public Point MidPoint => CalculateMidPoint(P1, P2);

        /// <summary>
        /// 線條長度
        /// </summary>
        public double Length => CalculateDistance(P1, P2);

        // BaseItem 抽象成員實作
        public override string ItemType => "線條";

        public override Point Center => MidPoint;

        public override Size Size => new Size(Math.Abs(P2.X - P1.X), Math.Abs(P2.Y - P1.Y));

        public override double Angle => CalculateAngle(P1, P2);

        public override string DisplayText => $"起點: {FormatPointText(P1)} 終點: {FormatPointText(P2)} 長度: {FormatValueText(Length)}";

        public override string SimpleDisplayText => $"{FormatPointText(P1)} → {FormatPointText(P2)} ({FormatValueText(Length)})";

        // 覆寫 BaseItem 的 Text 屬性
        public override string Point1Text => $"起點: {FormatPointText(P1)}";
        public override string Point2Text => $"終點: {FormatPointText(P2)}";

        public override void ClearUIElements()
        {
            Line = null;
            Point1 = null;
            Point2 = null;
            Point1Label = null;
            Point2Label = null;
            StartDot = null;
            EndDot = null;
            LabelBorder = null;
        }

        public override void ResetDragState()
        {
            IsDraggingPoint1 = false;
            IsDraggingPoint2 = false;
            IsDraggingLine = false;
        }

        public override void CalculateProperties()
        {
            // 線條的屬性計算相對簡單，主要是在屬性變更時自動計算
            // 這裡可以添加額外的計算邏輯
        }
    }
} 