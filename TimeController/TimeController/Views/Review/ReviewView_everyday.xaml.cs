using System;
using System.Windows;
using System.Windows.Controls;
using TimeController.Services;
using TimeController.ViewModels;
using TimeController.Views.Dialogs;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using TimeController.Models;
using Microsoft.Extensions.DependencyInjection;

namespace TimeController.Views.Review
{
    public partial class ReviewView_everyday : Page
    {
        public ReviewView_everyday(ITaskService taskService)
        {
            InitializeComponent();

            //var vm = App.AppHost.Services.GetRequiredService<ReviewViewModel_everyday>();
            //var navService = App.AppHost.Services.GetRequiredService<INavigationService>();

            //// 绑定导航事件
            //vm.NavigateToEveryweekRequested += () =>
            //{
            //    navService.NavigateTo(AppFrame.Instance, "Everyweek");
            //};

            //this.DataContext = vm;
        }

    }
}