using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace TimeController.Models
{
    public class ReviewCardModel
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        public Brush AccentColor { get; set; }  // 左侧色条颜色

        public ReviewCardModel(string icon, string title, string message, Brush accentColor = null)
        {
            Icon = icon;
            Title = title;
            Message = message;
            AccentColor = accentColor ?? Brushes.LightGray; //默认灰色
        }
    }
}
