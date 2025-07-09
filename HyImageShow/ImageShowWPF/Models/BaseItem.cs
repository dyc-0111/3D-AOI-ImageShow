using System;
using System.ComponentModel;
using System.Windows;
using HyImageShow.ImageShowWPF.View;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// 基礎項目類別，提供所有 Item 類別的共同功能
    /// </summary>
    public abstract class BaseItem : INotifyPropertyChanged
    {
        private int id;
        private bool isCompleted;
        private Point lastDragPos;
        private bool isDetailsVisible;
        private int zIndex;
        private bool selected;

        // 新增：靜態的座標轉換委託
        public static Func<Point, Point> CoordinateConverter { get; set; }
        
        /// <summary>
        /// 設置當前活動的座標轉換器（避免多實例覆蓋問題）
        /// </summary>
        public static void SetActiveCoordinateConverter(MainImageShow mainImageShow)
        {
            if (CoordinateConverter != null && CoordinateConverter.Target != null && mainImageShow != null && CoordinateConverter.Target != mainImageShow)
            {
                return;
            }
            if (mainImageShow != null)
            {
                CoordinateConverter = mainImageShow.ConvertDisplayToOriginalCoordinates;
            }
            else
            {
                CoordinateConverter = null;
            }
        }

        protected BaseItem()
        {
            IsDetailsVisible = true; // 預設展開詳細資訊
        }

        /// <summary>
        /// 唯一識別碼
        /// </summary>
        public int Id
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged(nameof(Id));
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
        /// 詳細資訊是否可見
        /// </summary>
        public bool IsDetailsVisible
        {
            get => isDetailsVisible;
            set
            {
                if (isDetailsVisible != value)
                {
                    isDetailsVisible = value;
                    OnPropertyChanged(nameof(IsDetailsVisible));
                }
            }
        }

        /// <summary>
        /// Z-Index，決定項目的堆疊順序
        /// </summary>
        public int ZIndex
        {
            get => zIndex;
            set
            {
                if (zIndex != value)
                {
                    zIndex = value;
                    OnPropertyChanged(nameof(ZIndex));
                }
            }
        }

        /// <summary>
        /// 是否被選取（支援多選拖曳）
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                if (selected != value)
                {
                    selected = value;
                    OnPropertyChanged(nameof(Selected));
                }
            }
        }

        /// <summary>
        /// 項目類型（用於顯示）
        /// </summary>
        public abstract string ItemType { get; }

        /// <summary>
        /// 中心點（用於定位）
        /// </summary>
        public abstract Point Center { get; }

        /// <summary>
        /// 大小（用於顯示）
        /// </summary>
        public abstract Size Size { get; }

        /// <summary>
        /// 角度（用於旋轉）
        /// </summary>
        public abstract double Angle { get; }

        /// <summary>
        /// 顯示文字（用於清單顯示）
        /// </summary>
        public abstract string DisplayText { get; }

        /// <summary>
        /// 簡化顯示文字（用於清單顯示）
        /// </summary>
        public abstract string SimpleDisplayText { get; }

        /// <summary>
        /// 點1文字（用於UI顯示）
        /// </summary>
        public virtual string Point1Text => $"點1: {FormatPointText(Center)}";

        /// <summary>
        /// 點2文字（用於UI顯示）
        /// </summary>
        public virtual string Point2Text => $"點2: {FormatValueText(Angle)}°";

        /// <summary>
        /// 點3文字（用於UI顯示）
        /// </summary>
        public virtual string Point3Text => $"點3: {FormatValueText(Size.Width)}×{FormatValueText(Size.Height)}";

        /// <summary>
        /// 中心文字（用於UI顯示）
        /// </summary>
        public virtual string CenterText => $"中心: {FormatPointText(Center)}";

        /// <summary>
        /// 尺寸文字（用於UI顯示）
        /// </summary>
        public virtual string SizeText => $"尺寸: {FormatValueText(Size.Width)}×{FormatValueText(Size.Height)}";

        /// <summary>
        /// 角度文字（用於UI顯示）
        /// </summary>
        public virtual string AngleText => $"角度: {FormatValueText(Angle)}°";

        /// <summary>
        /// 清除UI元素
        /// </summary>
        public abstract void ClearUIElements();

        /// <summary>
        /// 重置拖曳狀態
        /// </summary>
        public abstract void ResetDragState();

        /// <summary>
        /// 計算項目屬性（面積、周長等）
        /// </summary>
        public abstract void CalculateProperties();

        /// <summary>
        /// 屬性變更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 觸發屬性變更事件
        /// </summary>
        /// <param name="propertyName">屬性名稱</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 觸發多個屬性變更事件
        /// </summary>
        /// <param name="propertyNames">屬性名稱陣列</param>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// 計算兩點之間的距離
        /// </summary>
        /// <param name="p1">點1</param>
        /// <param name="p2">點2</param>
        /// <returns>距離</returns>
        protected static double CalculateDistance(Point p1, Point p2)
        {
            return (p2 - p1).Length;
        }

        /// <summary>
        /// 計算兩點之間的角度（度）
        /// </summary>
        /// <param name="p1">起點</param>
        /// <param name="p2">終點</param>
        /// <returns>角度（度）</returns>
        protected static double CalculateAngle(Point p1, Point p2)
        {
            return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X) * 180 / Math.PI;
        }

        /// <summary>
        /// 計算兩點的中點
        /// </summary>
        /// <param name="p1">點1</param>
        /// <param name="p2">點2</param>
        /// <returns>中點</returns>
        protected static Point CalculateMidPoint(Point p1, Point p2)
        {
            return new Point((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
        }

        /// <summary>
        /// 格式化點座標文字
        /// </summary>
        /// <param name="point">點</param>
        /// <param name="format">格式</param>
        /// <returns>格式化文字</returns>
        protected static string FormatPointText(Point point, string format = "F0")
        {
            if (double.IsNaN(point.X) || double.IsNaN(point.Y))
            {
                return "(N/A, N/A)";
            }
            if (CoordinateConverter == null)
            {
                return "(N/A, N/A)";
            }
            Point originalPoint = CoordinateConverter(point);
            
            // 檢查是否真的轉換了
            if (Math.Abs(originalPoint.X - point.X) < 1 && Math.Abs(originalPoint.Y - point.Y) < 1)
            {
                System.Diagnostics.Debug.WriteLine($"[FormatPointText] ⚠️ 警告：座標沒有實際轉換！");
            }
            
            return $"({originalPoint.X.ToString(format)}, {originalPoint.Y.ToString(format)})";
        }

        /// <summary>
        /// 格式化數值文字
        /// </summary>
        /// <param name="value">數值</param>
        /// <param name="format">格式</param>
        /// <returns>格式化文字</returns>
        protected static string FormatValueText(double value, string format = "F1")
        {
            return value.ToString(format);
        }
    }
} 