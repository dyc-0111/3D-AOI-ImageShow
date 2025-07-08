using System;
using System.Globalization;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace HyImageShow.ImageShowWPF.View.Local
{
    public class RoiPanelPlacementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is bool b && b;
            return isVisible ? PlacementMode.Left : PlacementMode.Right;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 