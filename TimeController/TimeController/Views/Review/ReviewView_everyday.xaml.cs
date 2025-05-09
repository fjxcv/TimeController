using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TimeController.Services;
using TimeController.ViewModels;
using TimeController.Views.Dialogs;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;


namespace TimeController.Views.Review
{
    /// <summary>
    /// ReviewView.xaml 的交互逻辑
    /// </summary>
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
            if (sender is Button btn && btn.DataContext is TaskItem task)
            {
                // 防止重复弹出
                if (task.Status != ReviewStatus.None)
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
                item.DataContext is TaskItem task)
            {
                // 弹出选择日期对话框
                var dialog = new PostponeDateDialog();
                bool? result = dialog.ShowDialog();

                if (result == true && dialog.SelectedDate.HasValue)
                {
                    task.Reason = reason;
                    task.Status = ReviewStatus.Postponed;
                    task.PostponeDate = dialog.SelectedDate;
                }
            }
        }



        //放弃按钮
        private void AbandonButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TaskItem task)
            {
                if (task.Status != ReviewStatus.None)
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
                item.DataContext is TaskItem task)
            {
                task.Reason = reason;
                task.Status = ReviewStatus.Abandoned;
            }
        }


    }
}
