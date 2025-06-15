using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TimeController.Converters
{
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ContentPresenter cp && cp.Parent is Panel panel)
            {
                int index = panel.Children.IndexOf(cp);

                // 如果参数是gt1，检查索引是否大于1
                if (parameter?.ToString() == "gt1")
                {
                    return index > 1;
                }

                return index;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
