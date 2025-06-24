using System;
using System.Threading.Tasks;
using System.Windows;
using TimeController.Services;
using TimeController.Helpers;
using TimeController.Models;
using TimeController.Views.Review;
using System.Diagnostics;

namespace TimeController.Services
{
    public static class ReviewReminderService
    {
        private static bool _hasPromptedToday = false;
        private static DateTime _lastDate = DateTime.Today;
        private static System.Windows.Threading.DispatcherTimer? _timer;

        public static void Start(ITaskService taskService, INavigationService navService)
        {
            if (_timer != null) return;
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _timer.Tick += async (_, __) => await TryShowReviewReminderAsync(taskService, navService);
            _timer.Start();
        }

        public static async Task TryShowReviewReminderAsync(ITaskService taskService, INavigationService navService)
        {
            var now = DateTime.Now;

            if (_lastDate != now.Date)
            {
                _lastDate = now.Date;
                _hasPromptedToday = false;
            }

            if (_hasPromptedToday) return;

            if (!UserSettings.EnableDailyReviewPrompt) return;

            if (now.Hour < UserSettings.DailyReviewPromptHour) return;


            var todayTasks = await taskService.GetTasksForDate(DateTime.Today);
            if (todayTasks.Count == 0) return;

            bool allUnfinished = todayTasks.TrueForAll(t => t.Status != MyTaskStatus.Completed);
            if (!allUnfinished) return;

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
