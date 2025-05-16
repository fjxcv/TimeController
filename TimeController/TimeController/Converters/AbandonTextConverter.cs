using System;
using System.Globalization;
using System.Windows.Data;
using TimeController.ViewModels;
using TimeController.Models;

namespace TimeController.Converters
{
    public class AbandonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MyTaskStatus status)
            {
                if (status == MyTaskStatus.Abandoned)
                {
                    return "已放弃";
                }
                return "放弃";
            }
            return "放弃";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
