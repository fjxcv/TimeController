using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeController.Models
{
    /// <summary>
    /// Global user settings loaded at startup
    /// </summary>
    public static class UserSettings
    {
        public static bool EnableDailyReviewPrompt { get; set; } = false;
        public static int DailyReviewPromptHour { get; set; } = 18;
    }
}