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
        private static bool _hasPromptedToday = false; // 标记当天是否已经弹出过提醒
        private static DateTime _lastDate = DateTime.Today; // 记录上次检查日期，用于判断是否跨天
        private static System.Windows.Threading.DispatcherTimer? _timer; // 定时器对象

        /// <summary>
        /// 启动每日复盘提醒定时器，每分钟检查一次是否满足弹窗条件
        /// </summary>
        public static void Start(ITaskService taskService, INavigationService navService)
        {
            if (_timer != null) return; // 防止重复启动

            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // 每 1 分钟检查一次
            };

            _timer.Tick += async (_, __) => await TryShowReviewReminderAsync(taskService, navService);
            _timer.Start();
        }

        /// <summary>
        /// 判断是否应该显示复盘提醒弹窗，满足条件后执行跳转
        /// </summary>
        public static async Task TryShowReviewReminderAsync(ITaskService taskService, INavigationService navService)
        {
            var now = DateTime.Now;

            // 若日期已变更，重置提醒标记
            if (_lastDate != now.Date)
            {
                _lastDate = now.Date;
                _hasPromptedToday = false;
            }

            // 若今天已经提醒过，则跳过
            if (_hasPromptedToday) return;

            // 若未开启每日复盘提醒设置，则跳过
            if (!UserSettings.EnableDailyReviewPrompt) return;

            // 若当前时间未达到用户设定的提醒时间（小时），跳过
            if (now.Hour < UserSettings.DailyReviewPromptHour) return;

            // 获取今日任务列表
            var todayTasks = await taskService.GetTasksForDate(DateTime.Today);

            // 若今天没有任务，也不提醒
            if (todayTasks.Count == 0) return;

            // 如果所有任务都未完成，则提醒（用于避免干扰真正执行中用户）
            bool allUnfinished = todayTasks.TrueForAll(t => t.Status != MyTaskStatus.Completed);
            if (!allUnfinished) return;

            // 弹出复盘提示窗口
            var dialog = new ReviewReminderDialog
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();

            // 若用户点击跳转按钮，则导航到每日复盘页
            if (dialog.ShouldNavigate)
            {
                navService.NavigateTo(AppFrame.Instance!, "Everyday");
            }

            // 标记为已弹出，避免重复提醒
            _hasPromptedToday = true;
        }
    }
}
