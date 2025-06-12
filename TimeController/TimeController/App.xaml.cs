using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TimeController.Services;
using TimeController.ViewModels;
using TimeController.Models;

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

                    // Services
                    services.AddScoped<ITaskService, TaskService>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    // ViewModels
                    services.AddTransient<WeekViewModel>();
                    services.AddScoped<ReviewViewModel_everyday>();
                    services.AddScoped<ReviewViewModel_everyweek>();

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

            // 重置开发数据（仅示例，生产环境可移除）
            var taskService = AppHost.Services.GetRequiredService<ITaskService>();
            _ = ((TaskService)taskService).ResetTaskDataAsync();

            // 打开主窗口
            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();

            // 异步延迟后提醒复盘
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ReviewReminderService.TryShowReviewReminderAsync(
                        taskService,
                        AppHost.Services.GetRequiredService<INavigationService>());
                });
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 停止并释放 Host
            AppHost.StopAsync().Wait();
            AppHost.Dispose();
            base.OnExit(e);
        }
    }
}
