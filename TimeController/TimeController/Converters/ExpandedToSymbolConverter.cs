using System;
using System.Globalization;
using System.Windows.Data;

namespace TimeController.Converters
{
    public class ExpandedToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isExpanded = (bool)value;
            return isExpanded ? "▲" : "▼";  // 展开显示向上箭头，折叠显示向下箭头
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
