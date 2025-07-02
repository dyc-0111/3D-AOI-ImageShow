using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 旋轉矩形ROI項目類別
    /// </summary>
    public class RectRoiItem : BaseItem
    {
        private Point center;
        private double width;
        private double height;
        private double angle;
        private bool isCompleted;
        private bool isDraggingCenter;
        private bool isDraggingCorner;
        private bool isDraggingRotate;
        private int draggingCornerIndex;
        private Point lastDragPos;

        public RectRoiItem()
        {
            CenterPoint = new Point(0, 0);
            Width = 0;
            Height = 0;
            RotationAngle = 0;
            IsCompleted = false;
            IsDraggingCenter = false;
            IsDraggingCorner = false;
            IsDraggingRotate = false;
            DraggingCornerIndex = -1;
            LastDragPos = new Point(0, 0);
        }

        /// <summary>
        /// 中心點
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
                        nameof(Point1Text), nameof(SizeText), nameof(AngleText));
                }
            }
        }

        /// <summary>
        /// 寬度
        /// </summary>
        public double Width
        {
            get => width;
            set
            {
                if (width != value)
                {
                    width = value;
                    OnPropertyChanged(nameof(Width));
                    OnPropertiesChanged(nameof(Area), nameof(Perimeter), nameof(Diagonal), nameof(AspectRatio), nameof(DisplayText), nameof(SimpleDisplayText), nameof(SizeText), 
                        nameof(Point1Text), nameof(SizeText), nameof(AngleText));
                }
            }
        }

        /// <summary>
        /// 高度
        /// </summary>
        public double Height
        {
            get => height;
            set
            {
                if (height != value)
                {
                    height = value;
                    OnPropertyChanged(nameof(Height));
                    OnPropertiesChanged(nameof(Area), nameof(Perimeter), nameof(Diagonal), nameof(AspectRatio), nameof(DisplayText), nameof(SimpleDisplayText), nameof(SizeText), 
                        nameof(Point1Text), nameof(SizeText), nameof(AngleText));
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
                    OnPropertiesChanged(nameof(DisplayText), nameof(SimpleDisplayText), nameof(AngleText), 
                        nameof(Point1Text), nameof(SizeText));
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
        /// 是否正在拖曳中心
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
        /// 是否正在拖曳角落
        /// </summary>
        public bool IsDraggingCorner
        {
            get => isDraggingCorner;
            set
            {
                if (isDraggingCorner != value)
                {
                    isDraggingCorner = value;
                    OnPropertyChanged(nameof(IsDraggingCorner));
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
        /// 正在拖曳的角落索引
        /// </summary>
        public int DraggingCornerIndex
        {
            get => draggingCornerIndex;
            set
            {
                if (draggingCornerIndex != value)
                {
                    draggingCornerIndex = value;
                    OnPropertyChanged(nameof(DraggingCornerIndex));
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
        /// 多邊形元素
        /// </summary>
        public Polygon Polygon { get; set; }

        /// <summary>
        /// 角落控制點陣列
        /// </summary>
        public Ellipse[] CornerDots { get; set; } = new Ellipse[4];

        /// <summary>
        /// 旋轉控制點
        /// </summary>
        public Ellipse RotateDot { get; set; }

        /// <summary>
        /// 標籤邊框
        /// </summary>
        public Border LabelBorder { get; set; }

        /// <summary>
        /// 角落縮放變換陣列
        /// </summary>
        public ScaleTransform[] CornerScales { get; } = { new ScaleTransform(1, 1), new ScaleTransform(1, 1), new ScaleTransform(1, 1), new ScaleTransform(1, 1) };

        /// <summary>
        /// 旋轉縮放變換
        /// </summary>
        public ScaleTransform RotateScale { get; } = new ScaleTransform(1, 1);

        /// <summary>
        /// 中心點縮放變換
        /// </summary>
        public ScaleTransform CenterScale { get; } = new ScaleTransform(1, 1);

        /// <summary>
        /// 面積
        /// </summary>
        public double Area => Width * Height;

        /// <summary>
        /// 周長
        /// </summary>
        public double Perimeter => 2 * (Width + Height);

        /// <summary>
        /// 對角線長度
        /// </summary>
        public double Diagonal => Math.Sqrt(Width * Width + Height * Height);

        /// <summary>
        /// 長寬比
        /// </summary>
        public double AspectRatio => Width > 0 ? Height / Width : 0;


        // BaseItem 抽象成員實作
        public override string ItemType => "旋轉矩形";

        public override Point Center => center;

        public override Size Size => new Size(Width, Height);

        public override double Angle => angle;

        public override string DisplayText => $"中心: {FormatPointText(Center)} 尺寸: {FormatValueText(Width)}×{FormatValueText(Height)} 角度: {FormatValueText(Angle)}°";

        public override string SimpleDisplayText => $"{FormatPointText(Center)} {FormatValueText(Width)}×{FormatValueText(Height)} {FormatValueText(Angle)}°";

        // 覆寫 BaseItem 的 Text 屬性
        public override string Point1Text => $"中心: {FormatPointText(CenterPoint)}";
        public override string SizeText => $"尺寸: {FormatValueText(Width)}×{FormatValueText(Height)}";
        public override string AngleText => $"角度: {FormatValueText(RotationAngle)}°";

        public override void ClearUIElements()
        {
            Polygon = null;
            CornerDots = new Ellipse[4];
            RotateDot = null;
            LabelBorder = null;
        }

        public override void ResetDragState()
        {
            IsDraggingCenter = false;
            IsDraggingCorner = false;
            IsDraggingRotate = false;
            DraggingCornerIndex = -1;
        }

        public override void CalculateProperties()
        {
            // 旋轉矩形的屬性計算相對簡單，主要是在屬性變更時自動計算
            // 這裡可以添加額外的計算邏輯
        }
    }
} 