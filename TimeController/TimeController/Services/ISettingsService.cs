using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeController.ViewModels;

namespace TimeController.Services
{
    /// <summary>
    /// 应用设置服务接口
    /// </summary>
    public interface ISettingsService
    {
        int LoadWeeklyTarget();
        void SaveWeeklyTarget(int value);

        bool LoadEnableDailyReviewPrompt();
        void SaveEnableDailyReviewPrompt(bool value);

        int LoadDailyReviewPromptHour();
        void SaveDailyReviewPromptHour(int hour);

        event Action<int>? WeeklyTargetChanged;
    }

}
