using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using HyImageShow.ImageShowWPF.Models;
using HyImageShow.ImageShowWPF.Services;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace HyImageShow.ImageShowWPF.Models
{
    /// <summary>
    /// ROI項目基類
    /// </summary>
    public class RoiItem : BaseItem
    {
        private object originalObject;
        private bool isSelected;

        public RoiItem()
        {
            IsDetailsVisible = true; // 預設展開詳細資訊
        }

        /// <summary>
        /// 原始ROI物件
        /// </summary>
        public object OriginalObject
        {
            get => originalObject;
            set
            {
                if (originalObject != value)
                {
                    // 取消訂閱舊物件的事件
                    UnsubscribeFromPropertyChanged(originalObject);
                    
                    originalObject = value;
                    
                    // 訂閱新物件的事件
                    SubscribeToPropertyChanged(originalObject);
                    
                    OnPropertyChanged(nameof(OriginalObject));
                }
            }
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        // 重寫 Text 屬性，提供更適合清單顯示的格式
        public override string Point1Text
        {
            get
            {
                if (OriginalObject is LineItem line) return line.Point1Text;
                if (OriginalObject is RulerItem ruler) return ruler.Point1Text;
                if (OriginalObject is EllipseRoiItem ellipse) return ellipse.Point1Text;
                if (OriginalObject is PolygonRoiItem poly) return poly.Point1Text;
                if (OriginalObject is RectRoiItem rect) return rect.Point1Text;
                if (OriginalObject is BezierArcRoiItem bezier) return bezier.Point1Text;
                if (OriginalObject is CircularArcRoiItem circular) return circular.Point1Text;
                return base.Point1Text;
            }
        }

        public override string Point2Text
        {
            get
            {
                if (OriginalObject is LineItem line) return line.Point2Text;
                if (OriginalObject is RulerItem ruler) return ruler.Point2Text;
                if (OriginalObject is EllipseRoiItem ellipse) return ellipse.Point2Text;
                if (OriginalObject is PolygonRoiItem poly) return poly.Point2Text;
                if (OriginalObject is RectRoiItem rect) return rect.Point2Text;
                if (OriginalObject is BezierArcRoiItem bezier) return bezier.Point2Text;
                if (OriginalObject is CircularArcRoiItem circular) return circular.Point2Text;
                return base.Point2Text;
            }
        }

        public override string Point3Text
        {
            get
            {
                if (OriginalObject is PolygonRoiItem poly) return poly.Point3Text;
                if (OriginalObject is BezierArcRoiItem bezier) return bezier.Point3Text;
                if (OriginalObject is CircularArcRoiItem circular) return circular.Point3Text;
                return base.Point3Text;
            }
        }

        public override string SizeText
        {
            get
            {
                if (OriginalObject is RulerItem ruler) return ruler.SizeText;
                if (OriginalObject is EllipseRoiItem ellipse) return ellipse.SizeText;
                if (OriginalObject is PolygonRoiItem poly) return poly.SizeText;
                if (OriginalObject is RectRoiItem rect) return rect.SizeText;
                if (OriginalObject is BezierArcRoiItem bezier) return bezier.SizeText;
                if (OriginalObject is CircularArcRoiItem circular) return circular.SizeText;
                return base.SizeText;
            }
        }

        public override string AngleText
        {
            get
            {
                if (OriginalObject is RectRoiItem rect) return $"角度: {rect.RotationAngle:F1}°";
                if (OriginalObject is PolygonRoiItem poly) return $"角度: {poly.RotationAngle:F1}°";
                if (OriginalObject is CircularArcRoiItem circularArc) return $"角度: {circularArc.StartAngle:F1}° - {circularArc.EndAngle:F1}°";
                return base.AngleText;
            }
        }

        public override string CenterText
        {
            get
            {
                if (OriginalObject is EllipseRoiItem ellipse) return ellipse.CenterText;
                if (OriginalObject is PolygonRoiItem poly) return poly.CenterText;
                if (OriginalObject is RectRoiItem rect) return rect.CenterText;
                if (OriginalObject is BezierArcRoiItem bezier) return bezier.CenterText;
                if (OriginalObject is CircularArcRoiItem circular) return circular.CenterText;
                return base.CenterText;
            }
        }

        public override void ClearUIElements()
        {
            // 清除UI元素的實作
        }

        public override void ResetDragState()
        {
            // 重置拖曳狀態的實作
        }

        public override void CalculateProperties()
        {
            // 計算屬性的實作
        }

        // 實現抽象屬性
        public override string ItemType
        {
            get
            {
                if (OriginalObject != null)
                {
                    var prop = OriginalObject.GetType().GetProperty("ItemType");
                    if (prop != null)
                    {
                        var value = prop.GetValue(OriginalObject)?.ToString();
                        if (!string.IsNullOrEmpty(value))
                            return value;
                    }
                    // 若沒有ItemType屬性，回傳型別名稱
                    return OriginalObject.GetType().Name;
                }
                return "ROI";
            }
        }
        public override Point Center => new Point(0, 0);
        public override Size Size => new Size(0, 0);
        public override double Angle => 0;
        public override string DisplayText => "ROI Item";
        public override string SimpleDisplayText => "ROI";

        #region 事件訂閱管理

        private void SubscribeToPropertyChanged(object obj)
        {
            if (obj is INotifyPropertyChanged notifyObj)
            {
                notifyObj.PropertyChanged += OnOriginalObjectPropertyChanged;
            }
        }

        private void UnsubscribeFromPropertyChanged(object obj)
        {
            if (obj is INotifyPropertyChanged notifyObj)
            {
                notifyObj.PropertyChanged -= OnOriginalObjectPropertyChanged;
            }
        }

        private void OnOriginalObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is CircularArcRoiItem circular)
            {
                // 根據變更的屬性更新對應的文字
                switch (e.PropertyName)
                {
                    case nameof(CircularArcRoiItem.StartPoint):
                        OnPropertyChanged(nameof(Point1Text));
                        break;
                    case nameof(CircularArcRoiItem.EndPoint):
                        OnPropertyChanged(nameof(Point2Text));
                        break;
                    case nameof(CircularArcRoiItem.UserMidPoint):
                        OnPropertyChanged(nameof(Point3Text));
                        break;
                    case nameof(CircularArcRoiItem.CenterPoint):
                        OnPropertyChanged(nameof(CenterText));
                        break;
                    case nameof(CircularArcRoiItem.Radius):
                    case nameof(CircularArcRoiItem.ArcLength):
                        OnPropertyChanged(nameof(SizeText));
                        break;
                }
            }
            else if (sender is PolygonRoiItem poly)
            {
                switch (e.PropertyName)
                {
                    case nameof(PolygonRoiItem.Points):
                    case nameof(PolygonRoiItem.RotationAngle):
                        OnPropertyChanged(nameof(Point1Text));
                        OnPropertyChanged(nameof(Point2Text));
                        OnPropertyChanged(nameof(Point3Text));
                        OnPropertyChanged(nameof(SizeText));
                        OnPropertyChanged(nameof(CenterText));
                        OnPropertyChanged(nameof(AngleText));
                        break;
                }
            }
            else if (sender is EllipseRoiItem ellipse)
            {
                switch (e.PropertyName)
                {
                    case nameof(EllipseRoiItem.CenterPoint):
                        OnPropertyChanged(nameof(Point1Text));
                        OnPropertyChanged(nameof(CenterText));
                        OnPropertyChanged(nameof(DisplayText));
                        OnPropertyChanged(nameof(SimpleDisplayText));
                        break;
                    case nameof(EllipseRoiItem.Radius):
                    case nameof(EllipseRoiItem.Size):
                        OnPropertyChanged(nameof(SizeText));
                        OnPropertyChanged(nameof(Point2Text));
                        OnPropertyChanged(nameof(DisplayText));
                        OnPropertyChanged(nameof(SimpleDisplayText));
                        break;
                }
            }
            else if (sender is LineItem line)
            {
                switch (e.PropertyName)
                {
                    case nameof(LineItem.P1):
                    case nameof(LineItem.P2):
                        OnPropertyChanged(nameof(Point1Text));
                        OnPropertyChanged(nameof(Point2Text));
                        OnPropertyChanged(nameof(SizeText));
                        OnPropertyChanged(nameof(DisplayText));
                        OnPropertyChanged(nameof(SimpleDisplayText));
                        break;
                }
            }
            else if (sender is RectRoiItem rect)
            {
                switch (e.PropertyName)
                {
                    case nameof(RectRoiItem.CenterPoint):
                    case nameof(RectRoiItem.Width):
                    case nameof(RectRoiItem.Height):
                    case nameof(RectRoiItem.RotationAngle):
                        OnPropertyChanged(nameof(Point1Text));
                        OnPropertyChanged(nameof(Point2Text));
                        OnPropertyChanged(nameof(SizeText));
                        OnPropertyChanged(nameof(AngleText));
                        OnPropertyChanged(nameof(DisplayText));
                        OnPropertyChanged(nameof(SimpleDisplayText));
                        break;
                }
            }
            else if (sender is RulerItem ruler)
            {
                switch (e.PropertyName)
                {
                    case nameof(RulerItem.Point1):
                    case nameof(RulerItem.Point2):
                    case nameof(RulerItem.Distance):
                        OnPropertyChanged(nameof(Point1Text));
                        OnPropertyChanged(nameof(Point2Text));
                        OnPropertyChanged(nameof(SizeText));
                        OnPropertyChanged(nameof(DisplayText));
                        OnPropertyChanged(nameof(SimpleDisplayText));
                        break;
                }
            }
            else if (sender is BezierArcRoiItem bezier)
            {
                switch (e.PropertyName)
                {
                    case nameof(BezierArcRoiItem.StartPoint):
                    case nameof(BezierArcRoiItem.EndPoint):
                    case nameof(BezierArcRoiItem.MiddlePoint):
                    case nameof(BezierArcRoiItem.ArcLength):
                    case nameof(BezierArcRoiItem.ChordLength):
                        OnPropertyChanged(nameof(Point1Text));
                        OnPropertyChanged(nameof(Point2Text));
                        OnPropertyChanged(nameof(Point3Text));
                        OnPropertyChanged(nameof(SizeText));
                        break;
                }
            }
        }

        #endregion
    }
} 