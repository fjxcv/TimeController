using System;
using System.Globalization;
using System.Windows.Data;
using TimeController.ViewModels;

namespace TimeController.Converters
{
    public class PostponeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MyTaskStatus status)
            {
                if (status == MyTaskStatus.Postponed)
                {
                    return "已推迟";
                }
                return "推迟";
            }
            return "推迟";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}