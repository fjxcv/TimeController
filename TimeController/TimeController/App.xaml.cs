using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using TimeController.Services;

namespace TimeController
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var taskService = AppHost.Services.GetRequiredService<ITaskService>();
            await ((TaskService)taskService).SeedTestDataAsync();

            // 打开主窗口
            var mainWindow = new MainWindow();

        }


        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddDbContext<TaskDbContext>(options =>
                    {
                        options.UseSqlite("Data Source=tasks.db");
                    });

                    services.AddScoped<ITaskService, TaskService>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    // 注册ViewModel
                    services.AddSingleton<ViewModels.ReviewViewModel_everyday>();
                    services.AddSingleton<ViewModels.ReviewViewModel_everyweek>();
                })
                .Build();

            AppHost.Start();
        }
    }
}
