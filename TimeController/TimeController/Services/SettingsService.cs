using System;
using Microsoft.Win32;

namespace TimeController.Services
{
    public class SettingsService : ISettingsService
    {
        private const string REG_KEY = @"HKEY_CURRENT_USER\Software\TimeController";

        public event Action<int>? WeeklyTargetChanged; // 每周任务目标变更事件

        public int LoadWeeklyTarget()
        {
            // 从注册表加载设置，默认值为 4
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