using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace TimeController.Converters
{
    public class NoteToTeacherConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string note && !string.IsNullOrEmpty(note))
            {
                var match = Regex.Match(note, @"教师:\s*([^,]+)");
                if (match.Success)
                    return match.Groups[1].Value.Trim();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
