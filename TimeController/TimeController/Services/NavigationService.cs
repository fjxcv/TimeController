using System;
using System.Windows.Controls;
using TimeController.Views.Review;

namespace TimeController.Services
{
    public class NavigationService : INavigationService
    {
        private readonly Frame _frame;

        public NavigationService(Frame frame)
        {
            _frame = frame;
        }

        public void NavigateTo(string viewKey)
        {
            // 根据 viewKey 导航到相应的页面
            switch (viewKey)
            {
                case "Everyday":
                    _frame.Navigate(new ReviewView_everyday(this));
                    break;
                case "Everyweek":
                    _frame.Navigate(new ReviewView_everyweek(this));
                    break;
                default:
                    throw new ArgumentException($"Unknown view key: {viewKey}");
            }
        }
    }
} 