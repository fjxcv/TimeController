using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using TimeController.Models;
using TimeController.Services;
using TimeController.ViewModels;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace TimeController.Views.StrongGoalWeek
{
    /// <summary>
    /// WeekView.xaml 的交互逻辑
    /// </summary>
    public partial class WeekView : Page
    {
        private readonly WeekViewModel _viewModel;
        private DateTime? _clickedDate;

        public WeekView()
        {
            InitializeComponent();

            _viewModel = new WeekViewModel();
            DataContext = _viewModel;

            // 初次加载时更新页面日期块
            UpdateDateDisplay(_viewModel.CurrentDate);

            // 当 CurrentDate 改变时更新日期块
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CurrentDate))
                {
                    UpdateDateDisplay(_viewModel.CurrentDate);
                }
            };

        }

        private void ImportSchedule_Click(object sender, RoutedEventArgs e)
        {
            var importWindow = new ImportScheduleWindow
            {
                Owner = Window.GetWindow(this)
            };
            importWindow.ShowDialog();
        }


        private async void WeekContentGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {

            // 如果点击的是任务块（Border 或其子元素），则不显示加号
            var originalSource = e.OriginalSource as DependencyObject;
            while (originalSource != null)
            {
                if (originalSource is Border border && border.DataContext is TimeController.ViewModels.WeekViewModel.TaskBlock)
                {
                    // 点击的是任务块，直接返回，不显示加号
                    return;
                }
                originalSource = VisualTreeHelper.GetParent(originalSource);
            }
            double timeAxisWidth = 75; // 时间轴宽度
            double dateHeaderHeight = 100; // 日期栏高度

            Point pos = e.GetPosition(WeekContentGrid);

            if (pos.Y < dateHeaderHeight) return;

            double contentX = pos.X - timeAxisWidth;
            if (contentX < 0) return;

            double colWidth = (WeekContentGrid.ActualWidth - timeAxisWidth) / 7;
            int colIndex = (int)(contentX / colWidth);

            // 1. 获取当前周的周一
            var vm = DataContext as WeekViewModel;
            var currentDate = vm?.CurrentDate ?? DateTime.Today;

            DateTime monday = currentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
            {
                monday = monday.AddDays(-1);
            }

            // 2. 推算当前点击的是哪一天
            DateTime clickedDate = monday.AddDays(colIndex); // 保存到字段中以便打开任务窗口时使用

            //如果再次点击已选中的列，取消
            if (_clickedDate.HasValue && _clickedDate.Value.Date == clickedDate.Date)
            {
                ClearSelection();
                return;
            }


            _clickedDate = clickedDate;
            HighlightColumn(colIndex);

            // 3. 把加号移动到点击点附近
            Point canvasPoint = WeekContentGrid.TranslatePoint(pos, RootCanvas);
            Canvas.SetLeft(AddTaskButton, canvasPoint.X - AddTaskButton.Width / 2);
            Canvas.SetTop(AddTaskButton, canvasPoint.Y - AddTaskButton.Height / 2);

            AddTaskButton.Visibility = Visibility.Visible;

            //点击时显示日期的悬浮提示
            ToolTip toolTip = new ToolTip
            {
                Content = _clickedDate?.ToString("M月d日（dddd）") ?? "",
                Background = Brushes.LightYellow,
                Foreground = Brushes.DarkSlateGray,
                FontSize = 14,
                Padding = new Thickness(8),
                Placement = PlacementMode.Top,
                PlacementTarget = AddTaskButton,
                IsOpen = true
            };


            // 设置 ToolTip 到按钮
            AddTaskButton.ToolTip = toolTip;

            //自动关闭提示
            await Task.Delay(1000);
            toolTip.IsOpen = false;


        }



        //取消选中的方法
        private void ClearSelection()
        {
            for (int i = 0; i < 7; i++)
            {
                var dateCol = FindName($"DateColumn{i}") as Border;
                if (dateCol != null)
                    dateCol.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // 顶部还原

                var taskCol = FindName($"TaskColumn{i}") as Border;
                if (taskCol != null)
                    taskCol.Background = Brushes.Transparent; // 任务区还原
            }

            AddTaskButton.Visibility = Visibility.Collapsed;
            _clickedDate = null; // 清除选择的日期（你要先把 _clickedDate 声明为 Nullable）
        }

        //高亮
        private void HighlightColumn(int index)
        {
            for (int i = 0; i < 7; i++)
            {
                var dateCol = FindName($"DateColumn{i}") as Border;
                if (dateCol != null)
                    dateCol.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // 顶部默认色

                var taskCol = FindName($"TaskColumn{i}") as Border;
                if (taskCol != null)
                    taskCol.Background = Brushes.Transparent; // 任务区域默认透明
            }

            var selectedTaskCol = FindName($"TaskColumn{index}") as Border;
            if (selectedTaskCol != null)
                selectedTaskCol.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)); // 任务区高亮


        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddTaskDialog(_clickedDate);

            if (dialog.ShowDialog() == true && dialog.ResultTask != null && !string.IsNullOrWhiteSpace(dialog.ResultTask.Name))
            {
                var task = dialog.ResultTask;

                // 关键：设置任务的日期
                if (_clickedDate.HasValue)
                    task.PlannedDate = _clickedDate.Value.Date;

                // 检查时间冲突
                var (hasConflict, conflicts) = _viewModel.CheckTimeConflicts(task);

                if (hasConflict)
                {
                    // 构建冲突提示信息
                    string conflictNames = string.Join(", ", conflicts.Select(c => c.Name));
                    string message = $"该时间段与以下任务冲突：\n{conflictNames}\n\n是否仍要添加?";

                    // 显示确认对话框
                    var result = MessageBox.Show(message, "时间冲突", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    // 如果用户选择取消，不添加任务
                    if (result == MessageBoxResult.No)
                    {
                        ClearSelection();
                        return;
                    }
                }
                System.Diagnostics.Debug.WriteLine($"新任务日期: {task.PlannedDate}");

                _viewModel.AddTask(task); // 推荐用 AddTask 方法，见前述建议
               
            }


            AddTaskButton.Visibility = Visibility.Collapsed;
            ClearSelection();


        }

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

        private Brush GetBrushForTaskType(TaskType type)
        {
            switch (type)
            {
                case TaskType.学习学业:
                    return Brushes.LightBlue;
                case TaskType.自我提升:
                    return Brushes.LightGreen;
                case TaskType.项目实践任务:
                    return Brushes.LightPink;
                case TaskType.其它:
                    return Brushes.LightYellow;
                case TaskType.未分类:
                    return Brushes.MediumPurple;
                default:
                    return Brushes.LightGray;
            }
        }


        /// <summary>
        /// 根据当前日期更新上方 7 天的显示（DateTextBlock0 ~ 6）
        /// </summary>
        private void UpdateDateDisplay(DateTime referenceDate)
        {
            // 获取该周周一
            DateTime monday = referenceDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            for (int i = 0; i < 7; i++)
            {
                DateTime currentDay = monday.AddDays(i);
                if (FindName($"DateTextBlock{i}") is TextBlock textBlock)
                {
                    textBlock.Text = currentDay.Day.ToString();
                }
            }
        }
    }

}
