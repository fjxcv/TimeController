using System;
using System.Globalization;
using System.Windows.Data;
using TimeController.ViewModels;
using TimeController.Models;

namespace TimeController.Converters
{
    public class AbandonEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MyTaskStatus status)
            {
                return status == MyTaskStatus.Pending;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}