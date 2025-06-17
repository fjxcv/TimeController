using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TimeController.Converters
{
    public class TaskTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. 验证输入基础条件
            if (values == null || values.Length < 3)
                return string.Empty;

            // 2. 处理特殊绑定值（使用GetType().Name判断替代直接类型引用）
            if (values.Any(v =>
                v == DependencyProperty.UnsetValue ||
                (v != null && v.GetType().Name == "NamedObject")))
            {
                return string.Empty;
            }

            // 3. 安全解析isAllDay
            bool isAllDay;
            try
            {
                isAllDay = System.Convert.ToBoolean(values[0]);
            }
            catch
            {
                isAllDay = false;
            }

            // 4. 处理全天任务
            if (isAllDay)
                return "全天";

            // 5. 安全解析时间
            var startTime = values[1] as TimeSpan?;
            var endTime = values[2] as TimeSpan?;

            // 6. 生成时间范围文本
            return (startTime, endTime) switch
            {
                (null, _) => string.Empty,
                (_, null) => $"{startTime:hh\\:mm} 开始",
                _ => $"{startTime:hh\\:mm}-{endTime:hh\\:mm}"
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("单向绑定不支持逆向转换");
        }
    }
}