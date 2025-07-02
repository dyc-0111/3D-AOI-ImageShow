using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 橢圓ROI項目類別
    /// </summary>
    public class EllipseRoiItem : BaseItem
    {
        private Point center;
        private double radius;
        private bool isCompleted;
        private bool isDraggingCenter;
        private bool isDraggingRadius;
        private Point lastDragPos;

        public EllipseRoiItem()
        {
            CenterPoint = new Point(0, 0);
            Radius = 0;
            IsCompleted = false;
            IsDraggingCenter = false;
            IsDraggingRadius = false;
            LastDragPos = new Point(0, 0);
        }

        /// <summary>
        /// 圓心
        /// </summary>
        public Point CenterPoint
        {
            get => center;
            set
            {
                if (center != value)
                {
                    center = value;
                    OnPropertyChanged(nameof(CenterPoint));
                    OnPropertiesChanged(nameof(DisplayText), nameof(SimpleDisplayText), 
                        nameof(Point1Text), nameof(Point2Text));
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
                    OnPropertiesChanged(nameof(Area), nameof(Circumference), nameof(Diameter), nameof(DisplayText), nameof(SimpleDisplayText), 
                        nameof(Point1Text), nameof(Point2Text));
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
        /// 是否正在拖曳圓心
        /// </summary>
        public bool IsDraggingCenter
        {
            get => isDraggingCenter;
            set
            {
                if (isDraggingCenter != value)
                {
                    isDraggingCenter = value;
                    OnPropertyChanged(nameof(IsDraggingCenter));
                }
            }
        }

        /// <summary>
        /// 是否正在拖曳半徑
        /// </summary>
        public bool IsDraggingRadius
        {
            get => isDraggingRadius;
            set
            {
                if (isDraggingRadius != value)
                {
                    isDraggingRadius = value;
                    OnPropertyChanged(nameof(IsDraggingRadius));
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
        /// 橢圓元素
        /// </summary>
        public Ellipse Ellipse { get; set; }

        /// <summary>
        /// 圓心控制點
        /// </summary>
        public Ellipse CenterDot { get; set; }

        /// <summary>
        /// 半徑控制點
        /// </summary>
        public Ellipse RadiusDot { get; set; }

        /// <summary>
        /// 標籤邊框
        /// </summary>
        public Border LabelBorder { get; set; }

        /// <summary>
        /// 圓心縮放變換
        /// </summary>
        public ScaleTransform CenterScale { get; } = new ScaleTransform(1, 1);

        /// <summary>
        /// 半徑縮放變換
        /// </summary>
        public ScaleTransform RadiusScale { get; } = new ScaleTransform(1, 1);

        /// <summary>
        /// 面積
        /// </summary>
        public double Area => Math.PI * Radius * Radius;

        /// <summary>
        /// 周長
        /// </summary>
        public double Circumference => 2 * Math.PI * Radius;

        /// <summary>
        /// 直徑
        /// </summary>
        public double Diameter => 2 * Radius;

        /// <summary>
        /// 中心點文字
        /// </summary>
        public override string Point1Text => $"中心: {FormatPointText(CenterPoint)}";

        /// <summary>
        /// 半徑文字
        /// </summary>
        public override string Point2Text => $"半徑: {FormatValueText(Radius)}";

        /// <summary>
        /// 顯示文字（用於ROI清單）
        /// </summary>
        public override string DisplayText => $"中心: {FormatPointText(CenterPoint)} 半徑: {FormatValueText(Radius)}";

        /// <summary>
        /// 簡化顯示文字（用於ROI清單）
        /// </summary>
        public override string SimpleDisplayText => $"{FormatPointText(CenterPoint)} R:{FormatValueText(Radius)}";

        // BaseItem 抽象成員實作
        public override string ItemType => "橢圓";

        public override Point Center => center;

        public override Size Size => new Size(Diameter, Diameter);

        public override double Angle => 0; // 橢圓沒有旋轉角度

        public override void ClearUIElements()
        {
            Ellipse = null;
            CenterDot = null;
            RadiusDot = null;
            LabelBorder = null;
        }

        public override void ResetDragState()
        {
            IsDraggingCenter = false;
            IsDraggingRadius = false;
        }

        public override void CalculateProperties()
        {
            // 橢圓的屬性計算相對簡單，主要是在屬性變更時自動計算
            // 這裡可以添加額外的計算邏輯
        }
    }
} 