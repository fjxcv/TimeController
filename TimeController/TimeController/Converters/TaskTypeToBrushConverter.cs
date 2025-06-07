using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TimeController.Models;

namespace TimeController.Converters
{
    public class TaskTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskType type)
            {
                return type switch
                {
                    TaskType.学习学业 => new SolidColorBrush(Colors.LightBlue),
                    TaskType.自我提升 => new SolidColorBrush(Colors.LightGreen),
                    TaskType.项目实践任务 => new SolidColorBrush(Colors.LightPink),
                    TaskType.日常任务 => new SolidColorBrush(Colors.LightYellow),
                    TaskType.其它 => new SolidColorBrush(Colors.LightGray),
                    _ => new SolidColorBrush(Colors.MediumPurple)
                };
            }
            return new SolidColorBrush(Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 