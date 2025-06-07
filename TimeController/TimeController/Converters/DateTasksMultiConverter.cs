using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using TimeController.Models;

namespace TimeController.Converters
{
    public class DateTasksMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is Dictionary<DateTime, ObservableCollection<TaskModel>> map &&
                values[1] is DateTime date)
            {
                if (map.TryGetValue(date.Date, out var tasks))
                    return tasks;
            }
            return new ObservableCollection<TaskModel>();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}