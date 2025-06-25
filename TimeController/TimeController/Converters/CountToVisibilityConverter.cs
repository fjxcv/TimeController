using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Collections;

namespace TimeController.Converters
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = 0;

            // 支持 int
            if (value is int i)
            {
                count = i;
            }
            // 支持 ICollection
            else if (value is ICollection collection)
            {
                count = collection.Count;
            }
            // 其它类型一律当 0 处理

            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
