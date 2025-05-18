

using System;
using System.Windows.Controls;
using TimeController.Views.Review;
using Microsoft.Extensions.DependencyInjection;

namespace TimeController.Services
{
    public class NavigationService : INavigationService
    {
        public void NavigateTo(Frame frame, string viewKey)
        {
            switch (viewKey)
            {
                case "Everyday":
                    var taskService = App.AppHost.Services.GetRequiredService<ITaskService>();
                    frame.Navigate(new ReviewView_everyday(taskService));
                    break;
                case "Everyweek":
                    frame.Navigate(new ReviewView_everyweek()); 
                    break;
                default:
                    throw new ArgumentException($"Unknown view key: {viewKey}");
            }
        }
    }
}
