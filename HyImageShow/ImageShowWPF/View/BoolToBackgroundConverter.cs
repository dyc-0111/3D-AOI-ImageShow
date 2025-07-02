using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace HyImageShow.ImageShowWPF.View
{
    public class BoolToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected)
            {
                return isSelected ? new SolidColorBrush(Color.FromRgb(208, 232, 255)) : new SolidColorBrush(Color.FromRgb(247, 247, 247));
            }
            return new SolidColorBrush(Color.FromRgb(247, 247, 247));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 