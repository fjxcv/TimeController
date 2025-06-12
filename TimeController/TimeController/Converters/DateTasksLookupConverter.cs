using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using TimeController.Models;

namespace TimeController.Converters
{
    public class DateTasksLookupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dictionary<DateTime, ObservableCollection<TaskModel>> tasksByDate && 
                parameter is DateTime date)
            {
                if (tasksByDate.TryGetValue(date.Date, out var tasks))
                {
                    return tasks;
                }
            }
            return new ObservableCollection<TaskModel>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}