using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TimeController.Services;
using TimeController.ViewModels;
using TimeController.Views;
using TimeController.Models;
using TimeController.Views.SettingsInfo;
using TimeController.Views.CasualMode;
using TimeController.Views.Review;
using TimeController.Views.StrongGoalMonth;
using TimeController.Views.StrongGoalWeek;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using System.Collections.ObjectModel;

namespace TimeController
{
    public partial class App : Application
    {
        /// <summary>
        /// 任务新增/更新后通知
        /// </summary>
        public static event Action<TaskModel>? TaskChanged;

        /// <summary>
        /// 外部调用此方法来触发刷新
        /// </summary>
        public static void NotifyTaskChanged(TaskModel task) => TaskChanged?.Invoke(task);

        /// <summary>
        /// 应用主机
        /// </summary>
        public static IHost AppHost { get; private set; } = null!;

        /// <summary>
        /// 全局服务提供器
        /// </summary>
        public static IServiceProvider Services => AppHost.Services;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 构建并启动 Host
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // EF Core DbContext
                    services.AddDbContext<TaskDbContext>(options =>
                        options.UseSqlite("Data Source=task.db"));

                    // 改MessageBox效果
                    MessageBox.DefaultBackdropType = BackdropType.Mica;

                    // Services
                    services.AddScoped<ITaskService, TaskService>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    services.AddScoped<IRewardService, RewardService>();
                    services.AddSingleton<ISettingsService, SettingsService>();

                    // ViewModels
                    services.AddTransient<WeekViewModel>();
                    services.AddScoped<ReviewViewModel_everyday>();
                    services.AddScoped<ReviewViewModel_everyweek>();
                    services.AddScoped<CasualModeViewModel>();

                    services.AddTransient<SettingsPageViewModel>();
                    services.AddTransient<AboutPageViewModel>();
                    services.AddSingleton<TodayTasksReminderViewModel>();

                    //views
                    services.AddTransient<CasualModeView>();
                    services.AddTransient<WeekView>();
                    services.AddTransient<MonthView>();
                    services.AddTransient<ReviewView_everyday>();
                    services.AddTransient<SettingsPage>();
                    services.AddTransient<AboutPage>();
                    services.AddSingleton<TodayTasksReminderDialog>();

                    // MainWindow
                    services.AddSingleton<MainWindow>();


                })
                .Build();
            AppHost.Start();

            // 确保数据库使用迁移初始化
            try
            {
                using var scope = AppHost.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
                db.Database.Migrate();
            }
            catch (Exception ex)
            {
                // TODO: 记录日志或提示错误，但不要阻塞程序
                Console.Error.WriteLine($"Database migration failed: {ex.Message}");
            }

            // 加载用户设置
            var settingsService = AppHost.Services.GetRequiredService<ISettingsService>();
            UserSettings.EnableDailyReviewPrompt = settingsService.LoadEnableDailyReviewPrompt();
            UserSettings.DailyReviewPromptHour = settingsService.LoadDailyReviewPromptHour();

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            Current.MainWindow = mainWindow;

            Dispatcher.BeginInvoke(async () =>
            {
                var taskService = AppHost.Services.GetRequiredService<ITaskService>();
                var navService = AppHost.Services.GetRequiredService<INavigationService>();

                // 启动复盘提醒计时器，确保不会被其他弹窗阻塞
                ReviewReminderService.Start(taskService, navService);

                var list = await taskService.GetTasksForDate(DateTime.Today);
                var todayTasks = new ObservableCollection<TaskModel>(list.Where(t => t.IsReminderEnabled));
                if (todayTasks.Any())
                {
                    var dlg = new TodayTasksReminderDialog(todayTasks)
                    {
                        Owner = mainWindow,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    dlg.ShowDialog();
                }
                // —— 复盘提醒 —— 
                // 立即尝试一次，不用等 Timer Tick
                await ReviewReminderService.TryShowReviewReminderAsync(taskService, navService);
            });


        }
        protected override void OnExit(ExitEventArgs e)
        {
            // 停止并释放 Host
            AppHost.StopAsync().Wait();
            AppHost.Dispose();
            // 保存应用程序数据
            if (Properties.Contains("SemesterStartDate"))
            {
                Console.WriteLine($"保存学期开始日期: {Properties["SemesterStartDate"]}");
            }

            base.OnExit(e);
            AppHost.Start();
        }

    }
}
