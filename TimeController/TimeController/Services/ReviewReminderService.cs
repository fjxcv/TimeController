

//跳出复盘弹窗的逻辑：1.时间在晚6点后 2.当天有已完成的任务 （可选允许or不允许弹出，还没做）


using System;
using System.Threading.Tasks;
using System.Windows;
using TimeController.Services;
using TimeController.Helpers;
using TimeController.Models;
using TimeController.Views.Review;

namespace TimeController.Services
{
    public static class ReviewReminderService
    {
        private static bool _hasPromptedToday = false;

        public static async Task TryShowReviewReminderAsync(ITaskService taskService, INavigationService navService)
        {
            if (_hasPromptedToday) return;

            var now = DateTime.Now;

            // 晚上18:00后再提醒
            if (now.Hour < 18) return;

            // 获取今日任务
            var todayTasks = await taskService.GetTasksForDate(DateTime.Today);
            if (todayTasks.Count == 0) return;

            // 如果今日任务都没完成，不提醒
            bool allUnfinished = todayTasks.TrueForAll(t => t.Status != MyTaskStatus.Completed);
            if (allUnfinished) return;

            //// 用户设置允许提示（设置还未完成）
            //if (!UserSettings.EnableDailyReviewPrompt)
            //    return;

            // 弹窗提示
            var dialog = new ReviewReminderDialog
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();

            if (dialog.ShouldNavigate)
            {
                navService.NavigateTo(AppFrame.Instance!, "Everyday");
            }

            _hasPromptedToday = true;
        }
    }
}
