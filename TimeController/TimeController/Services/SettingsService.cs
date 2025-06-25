using System;
using Microsoft.Win32;

namespace TimeController.Services
{
    public class SettingsService : ISettingsService
    {
        private const string REG_KEY = @"HKEY_CURRENT_USER\Software\TimeController";

        // 当每周目标被保存时触发
        public event Action<int>? WeeklyTargetChanged;

        public int LoadWeeklyTarget()
        {
            // 默认阈值 4
            return (int)(Registry.GetValue(REG_KEY, nameof(LoadWeeklyTarget), 4) ?? 4);
        }

        public void SaveWeeklyTarget(int value)
        {
            Registry.SetValue(REG_KEY, nameof(LoadWeeklyTarget), value, RegistryValueKind.DWord);
            WeeklyTargetChanged?.Invoke(value);
        }

        public bool LoadEnableDailyReviewPrompt()
        {
            return ((int)(Registry.GetValue(REG_KEY, nameof(LoadEnableDailyReviewPrompt), 0) ?? 0)) == 1;
        }

        public void SaveEnableDailyReviewPrompt(bool value)
        {
            Registry.SetValue(REG_KEY, nameof(LoadEnableDailyReviewPrompt), value ? 1 : 0, RegistryValueKind.DWord);
        }

        public int LoadDailyReviewPromptHour()
        {
            return (int)(Registry.GetValue(REG_KEY, nameof(LoadDailyReviewPromptHour), 18) ?? 18);
        }

        public void SaveDailyReviewPromptHour(int hour)
        {
            Registry.SetValue(REG_KEY, nameof(LoadDailyReviewPromptHour), hour, RegistryValueKind.DWord);
        }
    }
}
