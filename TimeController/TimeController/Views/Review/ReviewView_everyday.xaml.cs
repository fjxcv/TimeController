using System;
using System.Windows;
using System.Windows.Controls;
using TimeController.Services;
using TimeController.ViewModels;
using TimeController.Views.Dialogs;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace TimeController.Views.Review
{
    public partial class ReviewView_everyday : Page
    {
        public ReviewView_everyday(INavigationService navService)
        {
            InitializeComponent();
            this.DataContext = new ReviewViewModel_everyday(navService);
        }

        //推迟按钮
        private void PostponeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ReviewTaskItem task)
            {
                if (task.Status != MyTaskStatus.Pending)
                    return;

                var contextMenu = new ContextMenu();

                if (DataContext is ReviewViewModel_everyday vm)
                {
                    foreach (var reason in vm.ReviewReasons)
                    {
                        var item = new MenuItem
                        {
                            Header = reason,
                            DataContext = task
                        };
                        item.Click += PostponeReason_Click;
                        contextMenu.Items.Add(item);
                    }

                    contextMenu.PlacementTarget = btn;
                    contextMenu.IsOpen = true;
                }
            }
        }

        private void PostponeReason_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item &&
                item.Header is string reason &&
                item.DataContext is ReviewTaskItem task)
            {
                // 先设置原因
                task.Reason = reason;

                // 弹出选择日期对话框
                var dialog = new PostponeDateDialog();
                bool? result = dialog.ShowDialog();

                if (result == true && dialog.SelectedDate.HasValue)
                {
                    // 设置推迟日期
                    task.PostponeDate = dialog.SelectedDate;
                    // 最后更新状态
                    task.Status = MyTaskStatus.Postponed;
                }
                else
                {
                    // 如果用户取消选择日期，清除已设置的原因
                    task.Reason = null;
                }
            }
        }

        //放弃按钮
        private void AbandonButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ReviewTaskItem task)
            {
                if (task.Status != MyTaskStatus.Pending)
                    return;

                var contextMenu = new ContextMenu();

                if (DataContext is ReviewViewModel_everyday vm)
                {
                    foreach (var reason in vm.ReviewReasons)
                    {
                        var item = new MenuItem
                        {
                            Header = reason,
                            DataContext = task
                        };
                        item.Click += AbandonReason_Click;
                        contextMenu.Items.Add(item);
                    }

                    contextMenu.PlacementTarget = btn;
                    contextMenu.IsOpen = true;
                }
            }
        }

        private void AbandonReason_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item &&
                item.Header is string reason &&
                item.DataContext is ReviewTaskItem task)
            {
                task.Reason = reason;
                task.Status = MyTaskStatus.Abandoned;
            }
        }
    }
}