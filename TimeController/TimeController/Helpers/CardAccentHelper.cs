using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TimeController.Helpers
{
    public static class CardAccentHelper
    {
        public static Brush GetAccentColor(string title)
        {
            if (title.Contains("完成率") || title.Contains("完成情况"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF3E0")); // 橙黄

            if (title.Contains("重复推迟") || title.Contains("放弃"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCDD2")); // 浅红

            if (title.Contains("鼓励") || title.Contains("不错"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")); // 浅绿

            if (title.Contains("策略") || title.Contains("分配"))
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BBDEFB")); // 浅蓝

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F5F5")); // 默认灰
        }
    }

}
