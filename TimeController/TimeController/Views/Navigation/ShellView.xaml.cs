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
using TimeController.Views.SettingsInfo;

namespace TimeController.Views.Navigation
{
    public partial class ShellView : System.Windows.Controls.UserControl
    {

        public ShellView()
        {
            InitializeComponent();

            AppFrame.Instance = this.ContentFrame;

            // 设置默认打开咸鱼模式
            NavigationView_Root.SelectedItem = NavigationViewItem_CasualMode;

        }


        //导航栏
        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {

            // —— 离开“设置”前，检查是否有未保存改动 —— 
            if (ContentFrame.Content is SettingsPage settingsPage
                && settingsPage.DataContext is SettingsPageViewModel vm
                && vm.IsDirty)
            {
                var res = MessageBox.Show(
                    "当前设置尚未保存，是否先保存？",
                    "未保存的设置",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning,
                    MessageBoxResult.Cancel);

                if (res == MessageBoxResult.Cancel)
                {
                    // 取消导航，保持选中“设置”
                    NavigationView_Root.SelectedItem = NavigationViewItem_Settings;
                    return;
                }
                else if (res == MessageBoxResult.Yes)
                {
                    // 执行保存
                    vm.SaveCommand.Execute(null);
                }
                // No：直接丢弃改动继续导航
            }

            // —— 真正的导航分支 —— 
            var item = sender.SelectedItem as NavigationViewItem;
            Page page = item?.Tag?.ToString() switch
            {
                "CasualMode" => GetPage<CasualModeView>(),
                "WeekView" => GetPage<WeekView>(),
                "MonthView" => GetPage<MonthView>(),
                "Review" => CreateReviewPage(),   // Review 需要特殊逻辑
                "settings" => GetPage<SettingsPage>(),
                "about" => GetPage<AboutPage>(),
                _ => GetPage<CasualModeView>()
            };

            NavigateTo(page);
        }

        // 简化一个方法：从 DI 容器里拿 Page 实例
        private T GetPage<T>() where T : Page
        {
            return App.AppHost.Services.GetRequiredService<T>();
        }

        // ReviewPage 有事件回调，所以单独写个创建方法
        private ReviewView_everyday CreateReviewPage()
        {
            var taskSvc = App.AppHost.Services.GetRequiredService<ITaskService>();
            var vm = new ReviewViewModel_everyday(taskSvc);
            vm.NavigateToEveryweekRequested += () =>
            {
                var nav = App.AppHost.Services.GetRequiredService<INavigationService>();
                nav.NavigateTo(AppFrame.Instance!, "Everyweek");
            };
            return new ReviewView_everyday(vm);
        }

        // 实际导航到页面，并更新标题
        private void NavigateTo(Page page)
        {
            ContentFrame.Navigate(page);
            NavigationView_Root.Header = page.Title;
        }

    }

}