using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeController.Models
{
    public class TaskItem
    {
        public string Name { get; set; }
        public string Status { get; set; } // 推迟 / 放弃
    }

}
