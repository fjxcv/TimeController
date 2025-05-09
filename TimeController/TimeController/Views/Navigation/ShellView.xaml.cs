using iNKORE.UI.WPF.Modern.Controls;
using TimeController.Views.CasualMode;
using TimeController.Views.StrongGoalWeek;
using TimeController.Views.StrongGoalMonth;
using TimeController.Views.Review;
using System;
using System.Windows;
using TimeController.Services;

namespace TimeController.Views.Navigation
{
    public partial class ShellView : System.Windows.Controls.UserControl, INavigationService
    {

        private readonly INavigationService _navigationService;

        // 预先创建页面实例
        public CasualModeView Page_CasualMode = new CasualModeView();
        public WeekView Page_WeekView = new WeekView();
        public MonthView Page_MonthView = new MonthView();
        public ReviewView_everyday Page_Review;
        //public SettingsView Page_Settings = new SettingsView();
        //public AboutView Page_About = new AboutView();

        public ShellView()
        {
            InitializeComponent();
            _navigationService = new NavigationService(ContentFrame);
            Page_Review = new ReviewView_everyday(_navigationService);
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
                page = Page_Review;
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

        public void NavigateTo(string viewKey)
        {
            switch (viewKey)
            {
                case "Everyday":
                    ContentFrame.Navigate(new ReviewView_everyday(this));
                    break;
                case "Everyweek":
                    ContentFrame.Navigate(new ReviewView_everyweek(this));
                    break;
            }
        }



    }
}