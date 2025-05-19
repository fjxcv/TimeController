using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeController.Models
{
    public class ReviewCardModel
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        public ReviewCardModel(string icon, string title, string message)
        {
            Icon = icon;
            Title = title;
            Message = message;
        }
    }
}
