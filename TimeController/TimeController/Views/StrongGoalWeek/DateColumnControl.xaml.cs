using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using TimeController.ViewModels;
using static TimeController.ViewModels.WeekViewModel;

namespace TimeController.Views.StrongGoalWeek
{
    public partial class DateColumnControl : UserControl
    {
        public DateColumnControl()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += UserControl_MouseDown;
        }

        // 全天任务鼠标进入事件
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid
             && grid.FindName("ActionButtons") is UIElement btns)
            {
                btns.Visibility = Visibility.Visible;
            }
        }

        // 全天任务鼠标离开事件
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid
             && grid.FindName("ActionButtons") is UIElement btns)
            {
                btns.Visibility = Visibility.Collapsed;
            }
        }

        // 左击任务
        private void TaskBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 从 Element.Tag 获取 TaskBlock
            var element = sender as FrameworkElement;
            var block = element?.Tag as TaskBlock;
            if (block == null) return;

            // 获取WeekView并将任务块点击事件转发给WeekView处理
            var weekView = this.FindAncestor<WeekView>();
            if (weekView != null)
            {
                weekView.OnTaskBlockClicked(element, e);
            }

            // 阻止事件冒泡
            e.Handled = true;
        }

        // 点击页面其他区域关闭卡片的处理
        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 获取点击的元素
            var clickedElement = e.OriginalSource as DependencyObject;

            // 获取WeekView并检查卡片状态
            var weekView = this.FindAncestor<WeekView>();
            if (weekView != null)
            {
                // 如果点击不在卡片内，关闭卡片
                // 注意：卡片现在在WeekView中，所以我们委托给WeekView来处理
                weekView.CloseTaskDetailsCardIfOutside(clickedElement);
            }

            e.Handled = true;
        }

        // 删除全天任务

        private void DeleteAllDayButton_Click(object sender, RoutedEventArgs e)
        {
            // 将事件转发给父视图
            var weekView = this.FindAncestor<WeekView>();
            if (weekView != null)
            {
                weekView.OnDeleteAllDayButtonClicked(sender, e);
            }

            e.Handled = true;
        }

        //任务左击
        private void ColumnBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DateColumnViewModel vm)
            {
                var weekView = this.FindAncestor<WeekView>();
                weekView?.OnDateColumnClicked(vm.Index, e);
            }
            e.Handled = true;
        }

        // 展开/收起按钮
        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is DateColumnViewModel vm)
            {
                var weekView = this.FindAncestor<WeekView>();
                if (weekView?.DataContext is WeekViewModel wvm)
                {
                    wvm.ToggleColumnExpandCommand.Execute(vm.Index);
                }


            }
        }

        // 任务弹窗关闭事件
        private void TaskPopup_Closed(object? sender, EventArgs e)
        {
            if (DataContext is DateColumnViewModel vm)
            {
                vm.IsExpanded = false;
            }
        }

        // 辅助方法，用于查找父控件
        private T FindAncestor<T>() where T : DependencyObject
        {
            DependencyObject parent = this;
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        // 辅助方法来查找子控件
        private T FindVisualChild<T>(DependencyObject parent, Func<T, bool> condition) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && condition(typedChild))
                    return typedChild;

                var result = FindVisualChild(child, condition);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
