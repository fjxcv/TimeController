using System;
using System.Globalization;
using System.Windows.Data;
using TimeController.ViewModels;

namespace TimeController.Converters
{ 
public class AbandonEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (ReviewStatus)value == ReviewStatus.None;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

}