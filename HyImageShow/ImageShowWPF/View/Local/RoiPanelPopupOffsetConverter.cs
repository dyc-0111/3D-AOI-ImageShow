using System;
using System.Globalization;
using System.Windows.Data;

namespace HyImageShow.ImageShowWPF.View.Local
{
    public class RoiPanelPopupOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double roiPanelHeight = value is double d ? d : 0;
            return (roiPanelHeight - 80) / 2; // 80 是按鈕高度
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}