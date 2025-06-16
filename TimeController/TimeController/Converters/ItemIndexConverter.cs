using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace TimeController.Converters
{
    public class ItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DependencyObject item)
            {
                ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(item);
                if (itemsControl != null)
                {
                    int index = itemsControl.ItemContainerGenerator.IndexFromContainer(item);

                    // 若未生成容器，尝试根据数据项查找索引
                    if (index < 0 && item is FrameworkElement fe && fe.DataContext != null)
                    {
                        index = itemsControl.Items.IndexOf(fe.DataContext);
                    }

                    // 判断是否大于2的特殊处理
                    if (parameter?.ToString() == "gt2")
                        return index > 2;

                    return index;
                }
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
