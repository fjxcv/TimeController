using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TimeController.Converters
{
    public class TaskTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isAllDay = (bool)values[0];
            TimeSpan? startTime = values[1] as TimeSpan?;
            TimeSpan? endTime = values[2] as TimeSpan?;

            if (isAllDay)
                return "全天";

            if (startTime.HasValue && endTime.HasValue)
                return $"{startTime.Value:hh\\:mm}-{endTime.Value:hh\\:mm}";

            if (startTime.HasValue)
                return $"{startTime.Value:hh\\:mm}";

            return string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
