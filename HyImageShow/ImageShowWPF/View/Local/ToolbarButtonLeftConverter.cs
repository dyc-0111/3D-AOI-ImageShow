using System;
using System.Globalization;
using System.Windows.Data;

namespace HyImageShow.ImageShowWPF.View.Local
{
    public class ToolbarButtonLeftConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = value is double d ? d : 0;
            return width / 2 - 20; // 20 是按鈕寬度一半
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 