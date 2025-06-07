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
using NPOI.SS.Formula.Functions;
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
        private Popup? _taskDetailPopup;

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

            // 订阅冲突检测事件
            if (DataContext is WeekViewModel viewModel)
            {
                viewModel.ConflictDetected += OnTaskConflictDetected;
            }

        }


        // 处理任务冲突
        private async void OnTaskConflictDetected(TaskModel newTask, List<WeekViewModel.TaskBlock> conflicts)
        {
            // 构建冲突任务列表
            var conflictNames = string.Join("\n", conflicts.Select(c => c.Name));

            // 弹出确认对话框
            var result = MessageBox.Show(
                $"该时间段与以下任务冲突:\n{conflictNames}\n\n是否仍要添加?",
                "时间冲突",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            // 如果用户选择"是"，则删除冲突任务并添加新任务
            if (result == MessageBoxResult.Yes)
            {
                var viewModel = DataContext as WeekViewModel;
                await viewModel.HandleConflictAndAddTask(newTask, conflicts);
            }
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

        //左击任务块显示详细信息
        private void TaskBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is TimeController.ViewModels.WeekViewModel.TaskBlock task)
            {
                // 关闭已有的Popup
                if (_taskDetailPopup != null)
                {
                    _taskDetailPopup.IsOpen = false;
                    _taskDetailPopup = null;
                }

                // 获取当前周的周一日期
                var vm = DataContext as WeekViewModel;
                var currentDate = vm?.CurrentDate ?? DateTime.Today;
                DateTime monday = currentDate.Date;
                while (monday.DayOfWeek != DayOfWeek.Monday)
                {
                    monday = monday.AddDays(-1);
                }

                // 根据任务的列索引计算对应的日期
                DateTime taskDate = monday.AddDays(task.Column);

                // 构建详细信息
                string detail = $"任务备注：{task.Note}\n任务类型：{task.Type}\n";
                if (task.IsAllDay)
                {
                    // 全天任务显示日期
                    detail += $"任务时间：{taskDate:yyyy年M月d日 (dddd)}";
                }
                else if (task.StartTime != TimeSpan.Zero || task.EndTime != TimeSpan.Zero)
                {
                    // 分时任务显示时间
                    detail += $"任务时间：{task.StartTime:hh\\:mm} - {task.EndTime:hh\\:mm}";
                }

                // 创建Popup内容
                var popupContent = new Border
                {
                    Background = Brushes.LightYellow,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(8),
                    Child = new TextBlock
                    {
                        Text = detail,
                        Foreground = Brushes.DarkSlateGray,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    }
                };

                // 创建Popup
                _taskDetailPopup = new Popup
                {
                    Child = popupContent,
                    PlacementTarget = border,
                    Placement = PlacementMode.Mouse,
                    StaysOpen = false, // 点击外部自动关闭
                    AllowsTransparency = true,
                    IsOpen = true
                };

            }
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

                // 强管理模式任务
                task.Mode = TaskMode.Strong;

                System.Diagnostics.Debug.WriteLine($"新任务日期: {task.PlannedDate}");

                // 直接添加任务，让ViewModel内部处理冲突
                _viewModel.AddTask(task);
            }

            AddTaskButton.Visibility = Visibility.Collapsed;
            ClearSelection();
        }


        //任务删除（鼠标悬停
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var button = grid.FindName("DeleteButton") as Button;
                if (button != null)
                    button.Visibility = Visibility.Visible;
            }
        }

        ////任务删除（鼠标离开
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var button = grid.FindName("DeleteButton") as Button;
                if (button != null)
                    button.Visibility = Visibility.Collapsed;
            }
        }

        //查找子元素
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

        /// <summary>
        /// 根据当前日期更新上方 7 天的显示（DateTextBlock0 ~ 6）
        /// </summary>
        private void UpdateDateDisplay(DateTime referenceDate)
        {
            // 获取该周周一
            DateTime monday = referenceDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            // 当前选择的月份（参考日期所在月份）
            int currentMonth = referenceDate.Month;

            for (int i = 0; i < 7; i++)
            {
                DateTime currentDay = monday.AddDays(i);
                if (FindName($"DateTextBlock{i}") is TextBlock textBlock)
                {
                    // 检查当前日期是否属于参考月份
                    if (currentDay.Month != currentMonth)
                    {
                        // 不是当前月份，添加月份前缀
                        textBlock.Text = $"{currentDay.Month}月{currentDay.Day}";
                    }
                    else
                    {
                        // 是当前月份，只显示日期
                        textBlock.Text = currentDay.Day.ToString();
                    }

                    // 可选：为非本月日期应用不同样式
                    if (currentDay.Month != currentMonth)
                    {
                        textBlock.Foreground = new SolidColorBrush(Colors.Gray); // 不同月份的日期显示灰色
                        textBlock.FontWeight = FontWeights.Bold; // 加粗
                    }
                    else
                    {
                        textBlock.Foreground = new SolidColorBrush(Colors.Black); // 当前月份日期显示黑色
                        textBlock.FontWeight = FontWeights.Bold; // 加粗
                    }
                }
            }
        }


    }

}
