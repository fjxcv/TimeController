// 在 TimeController/Converters/BoolToVisibilityConverter.cs 文件中
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TimeController.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果传进来不是 bool，就直接 Collapse
            if (value is not bool b)
                return Visibility.Collapsed;

            // 检查ConverterParameter，若它等于False，就把b取反
            if (parameter?.ToString()?.Equals("False", StringComparison.OrdinalIgnoreCase) == true)
                b = !b;

            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
