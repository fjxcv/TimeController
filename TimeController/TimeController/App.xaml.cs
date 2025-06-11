using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TimeController.Services;
using TimeController.ViewModels;

namespace TimeController
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }
        public static IServiceProvider Services { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 直接使用 AppHost.Services
            Services = AppHost.Services;

            var taskService = Services.GetRequiredService<ITaskService>();
            var navService = Services.GetRequiredService<INavigationService>();

            //获取 DbContext 实例，确保数据库使用迁移初始化
            var db = Services.GetRequiredService<TaskDbContext>();
            db.Database.Migrate();

            //重置开发数据
            await ((TaskService)taskService).ResetTaskDataAsync();

            // 打开主窗口
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // 提醒复盘
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000); // 延迟 1 秒
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ReviewReminderService.TryShowReviewReminderAsync(taskService, navService);
                });
            });
        }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<TaskDbContext>(options =>
                    {
                        options.UseSqlite("Data Source=task.db");
                    });

                    services.AddTransient<WeekViewModel>();
                    services.AddScoped<ITaskService, TaskService>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    // 注册ViewModel
                    services.AddScoped<ReviewViewModel_everyday>();
                    services.AddScoped<ReviewViewModel_everyweek>();

                })
                .Build();

            AppHost.Start();
        }
    }
}
