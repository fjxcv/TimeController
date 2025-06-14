using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TimeController.ViewModels;

namespace TimeController.Views.StrongGoalWeek
{
    public partial class DateColumnControl : UserControl
    {
        public DateColumnControl()
        {
            InitializeComponent();
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid
             && grid.FindName("ActionButtons") is UIElement btns)
            {
                btns.Visibility = Visibility.Visible;
            }
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid
             && grid.FindName("ActionButtons") is UIElement btns)
            {
                btns.Visibility = Visibility.Collapsed;
            }
        }

        private void TaskBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 将事件转发给父视图
            var weekView = this.FindAncestor<WeekView>();
            if (weekView != null)
            {
                weekView.OnTaskBlockClicked(sender, e);
            }
        }

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

        // 添加辅助方法来查找子控件，替代 Utilities.VisualTreeHelper.FindVisualChild
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
