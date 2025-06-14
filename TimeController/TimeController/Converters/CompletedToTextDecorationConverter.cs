using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TimeController.Models;
using System;
using System.Windows;


namespace TimeController.Converters
{
    public class CompletedToTextDecorationConverter : IValueConverter
    {
        // 把 Completed 状态映射成删除线
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MyTaskStatus status && status == MyTaskStatus.Completed)
                return TextDecorations.Strikethrough;

            return null; // 其他状态不显示装饰
        }

        // 不需要双向绑定的话，ConvertBack 可以抛异常或返回 Binding.DoNothing
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}
