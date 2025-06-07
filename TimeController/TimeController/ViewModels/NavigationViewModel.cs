using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TimeController.Models;
using System.Windows.Controls;
using TimeController.Views.CasualMode;
using TimeController.Views.Review;
using TimeController.Views.StrongGoalWeek;
using TimeController.Views.StrongGoalMonth;
using TimeController.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TimeController.ViewModels
{
    public class NavigationViewModel
    {
        private readonly Frame _navigationFrame;
        private readonly INavigationService _navigationService;
        public ICommand NavigateCommand { get; private set; }

        public NavigationViewModel(Frame navigationFrame, INavigationService navigationService)
        {
            _navigationFrame = navigationFrame;
            _navigationService = navigationService;
            NavigateCommand = new RelayCommand(NavigateTo);
        }

        private void NavigateTo(object parameter)
        {
            if (parameter is string tag)
            {
                Page pageToLoad = tag switch
                {
                    "CasualMode" => new CasualModeView(),
                    "MonthView" => new MonthView(),
                    "WeekView" => new WeekView(),
                    "Review" => new ReviewView_everyday(
                        new ReviewViewModel_everyday(App.AppHost.Services.GetRequiredService<ITaskService>())
                        ),

                    //"settings" => 
                    //"about" => 
                    _ => new CasualModeView()
                };

                _navigationFrame.Navigate(pageToLoad);
            }
        }
    }

    public class NavigationItem
    {
        public string Title { get; set; }
        public string Tag { get; set; }

        public NavigationItem(string title, string tag)
        {
            Title = title;
            Tag = tag;
        }
    }
}
