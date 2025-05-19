using iNKORE.UI.WPF.Modern.Controls;
using TimeController.Views.CasualMode;
using TimeController.Views.StrongGoalWeek;
using TimeController.Views.StrongGoalMonth;
using TimeController.Views.Review;
using System;
using System.Windows;
using TimeController.Services;
using TimeController.Helpers;
using Microsoft.Extensions.DependencyInjection;
using TimeController.ViewModels;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.Views.Navigation
{
    public partial class ShellView : System.Windows.Controls.UserControl
    {

        //private readonly INavigationService _navigationService;

        // 预先创建页面实例
        public CasualModeView Page_CasualMode = new CasualModeView();
        public WeekView Page_WeekView = new WeekView();
        public MonthView Page_MonthView = new MonthView();
        //public ReviewView_everyday Page_Review;
        //public SettingsView Page_Settings = new SettingsView();
        //public AboutView Page_About = new AboutView();

        public ShellView()
        {
            InitializeComponent();

            AppFrame.Instance = this.ContentFrame;

            //_navigationService = new NavigationService(ContentFrame);

            // 设置默认选中项
            NavigationView_Root.SelectedItem = NavigationViewItem_CasualMode;

        }


        //导航栏
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = sender.SelectedItem as NavigationViewItem;
            Page page = null;

            if (item == NavigationViewItem_CasualMode)
            {
                page = Page_CasualMode;
            }
            else if (item == NavigationViewItem_WeekView)
            {
                page = Page_WeekView;
            }
            else if (item == NavigationViewItem_MonthView)
            {
                page = Page_MonthView;
            }
            else if (item == NavigationViewItem_Review_everyday)
            {
                var taskService = App.AppHost.Services.GetRequiredService<ITaskService>();

                var vm = new ReviewViewModel_everyday(taskService);

                vm.NavigateToEveryweekRequested += () =>
                {
                    var nav = App.AppHost.Services.GetRequiredService<INavigationService>();
                    nav.NavigateTo(AppFrame.Instance!, "Everyweek");
                };

                page = new ReviewView_everyday(vm);

                ContentFrame.Navigate(page);
                NavigationView_Root.Header = page.Title;
                return;
            }
            else if (item == NavigationViewItem_Settings)
            {
                //page = Page_Settings;
            }
            else if (item == NavigationViewItem_About)
            {
                //page = Page_About;
            }

            if (page != null)
            {
                NavigationView_Root.Header = page.Title;
                ContentFrame.Navigate(page);
            }
        }

        //public void NavigateTo(string viewKey)
        //{
        //    switch (viewKey)
        //    {
        //        case "Everyday":
        //            {
        //                var taskService = App.AppHost.Services.GetRequiredService<ITaskService>();
        //                var page = new ReviewView_everyday(taskService);
        //                var vm = App.AppHost.Services.GetRequiredService<ReviewViewModel_everyday>();

        //                vm.IsEverydayPage = true; //默认每日复盘按钮亮
        //                vm.NavigateToEveryweekRequested += () =>
        //                {
        //                    NavigateTo("Everyweek");
        //                };

        //                page.DataContext = vm;
        //                ContentFrame.Navigate(page);
        //                break;
        //            }

        //        case "Everyweek":
        //            {
        //                var vm = App.AppHost.Services.GetRequiredService<ReviewViewModel_everyweek>();
        //                vm.IsEverydayPage = false;

        //                vm.NavigateToEverydayRequested += () =>
        //                {
        //                    NavigateTo("Everyday");
        //                };

        //                var page = new ReviewView_everyweek(vm);
        //                ContentFrame.Navigate(page);
        //                break;
        //            }

        //        default:
        //            throw new ArgumentException($"Unknown view key: {viewKey}");
        //    }
        //}



    }
}