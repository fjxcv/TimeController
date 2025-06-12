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
            try
            {
                // 从服务容器获取 TaskService
                _viewModel = App.Services.GetRequiredService<WeekViewModel>();

                // 使用有参构造函数创建 ViewModel
                //_viewModel = new WeekViewModel(taskService);

                DataContext = _viewModel;

                // 初始化视图模型和事件处理
                InitializeViewModel();
                InitializeEvents();

                // 初次加载时更新页面日期块
                UpdateDateDisplay(_viewModel.CurrentDate);

                // 强制初始加载
                _viewModel.LoadTasksForCurrentWeek();

            }
            catch(Exception ex)
            {
                MessageBox.Show($"初始化错误：{ex.Message}\n\n应用程序可能无法正常工作。",
                       "初始化错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"初始化错误：{ex.Message}\n{ex.StackTrace}");

            }

        }

        private void InitializeEvents()
        {
            // 当 CurrentDate 改变时更新日期块
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CurrentDate))
                {
                    UpdateDateDisplay(_viewModel.CurrentDate);
                }
            };

            // 订阅冲突检测事件
            _viewModel.ConflictDetected += OnTaskConflictDetected;
        }

        // 初始化视图模型的事件处理
        private void InitializeViewModel()
        {
            // 添加删除确认事件处理
            _viewModel.DeleteConfirmationRequested += async (block) =>
            {
                var result = MessageBox.Show(
                     $"确定要删除任务 \"{block.Name}\" 吗？",
                     "确认删除",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                return result == MessageBoxResult.Yes;
            };
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

        //导入改1
        private void ImportSchedule_Click(object sender, RoutedEventArgs e)
        {
            var importWindow = new ImportScheduleWindow
            {
                Owner = Window.GetWindow(this)
            };

            // 保存导入窗口的视图模型引用
            var importVM = importWindow.DataContext as ImportScheduleViewModel;

            if (importVM != null)
            {
                // 订阅开学日期事件

                importVM.CoursesSavedWithStartDate += (startDate) =>
                {
                    Console.WriteLine($"收到课程保存事件，开学日期: {startDate:yyyy-MM-dd}");
                    // 设置学期开始日期但不改变当前日期
                    _viewModel.SemesterStartDate = startDate;
                };
            }
            // 添加关闭事件
            importWindow.Closed += (s, args) =>
            {
                Console.WriteLine("导入窗口已关闭，重新加载周视图数据");
                if (importVM != null && importVM.HasImportedCourses)
                {
                    // 获取所选的开学日期，并跳转到那一周
                    //_viewModel.CurrentDate = importVM.SemesterStartDate;
                    _viewModel.LoadTasksForCurrentWeek();
                }
            };

            importWindow.ShowDialog();
        }

        //手动添加课表
        private async void AddCourse_Click(object sender, RoutedEventArgs e)
        {
            // 获取当前周视图的开学日期，如果未设置则使用当天日期
            DateTime semesterStartDate = _viewModel.SemesterStartDate ?? DateTime.Today;

            // 正确创建添加课程窗口，提供必要的参数
            var addCourseWindow = new AddCourseWindow(semesterStartDate)
            {
                Owner = Window.GetWindow(this)
            };

            // 显示窗口并处理结果
            if (addCourseWindow.ShowDialog() == true && addCourseWindow.ResultCourse != null)
            {
                // 获取添加的课程
                var newCourse = addCourseWindow.ResultCourse;

                try
                {
                    // 需要在 WeekViewModel 中先添加 AddAndSaveCourse 方法
                    // 如果尚未实现该方法，将在下面提供实现
                    await _viewModel.AddAndSaveCourse(newCourse);

                    // 显示成功消息
                    //MessageBox.Show($"成功添加课程：{newCourse.Name}", "添加成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    //MessageBox.Show($"添加课程失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 供 DateColumnControl 调用的方法
        public void OnTaskBlockClicked(object sender, MouseButtonEventArgs e)
        {
            TaskBlock_MouseLeftButtonDown(sender, e);
        }

        public void OnDeleteAllDayButtonClicked(object sender, RoutedEventArgs e)
        {
            DeleteAllDayButton_Click(sender, e);
        }

        // 供 WeekContentGrid 调用的方法
        // 修改WeekContentGrid_MouseDown方法，调整点击位置计算
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

            Point pos = e.GetPosition(WeekContentGrid);

            double contentX = pos.X - timeAxisWidth;
            if (contentX < 0) return;

            double colWidth = (WeekContentGrid.ActualWidth - timeAxisWidth) / 7;
            int colIndex = (int)(contentX / colWidth);

            // 获取当前周的周一
            var vm = DataContext as WeekViewModel;
            var currentDate = vm?.CurrentDate ?? DateTime.Today;

            DateTime monday = currentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
            {
                monday = monday.AddDays(-1);
            }

            // 推算当前点击的是哪一天
            DateTime clickedDate = monday.AddDays(colIndex);

            //如果再次点击已选中的列，取消
            if (_clickedDate.HasValue && _clickedDate.Value.Date == clickedDate.Date)
            {
                ClearSelection();
                return;
            }

            _clickedDate = clickedDate;
            HighlightColumn(colIndex);

            // 在按钮宽高方面调整点击位置
            Point screenPoint = WeekContentGrid.PointToScreen(pos);
            Point canvasPoint = RootCanvas.PointFromScreen(screenPoint);

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
            try // 添加异常处理
            {
                // 确保sender是Border
                if (!(sender is Border border)) return;

                // 确保DataContext是TaskBlock
                if (!(border.DataContext is TimeController.ViewModels.WeekViewModel.TaskBlock task)) return;

                // 关闭已有的Popup
                if (_taskDetailPopup != null)
                {
                    _taskDetailPopup.IsOpen = false;
                    _taskDetailPopup = null;
                }

                // 获取当前周的周一日期
                var vm = DataContext as WeekViewModel;
                if (vm == null) return;

                // 检查是否为课程任务 - 简化判断逻辑
                bool isCourse = vm.CourseTaskBlocks.Contains(task);

                // 如果是课程，不显示任何信息
                if (isCourse)
                {
                    e.Handled = true; // 标记事件已处理
                    return; // 直接返回，不显示任何弹出信息
                }

                var currentDate = vm.CurrentDate;
                DateTime monday = currentDate.Date;
                while (monday.DayOfWeek != DayOfWeek.Monday)
                {
                    monday = monday.AddDays(-1);
                }

                // 根据任务的列索引计算对应的日期
                DateTime taskDate = monday.AddDays(task.Column);

                // 构建详细信息（仅针对非课程任务）
                string detail = $"备注：{task.Note ?? ""}\n类型：{task.Type}\n";
                if (task.IsAllDay)
                {
                    // 全天任务显示日期
                    detail += $"时间：{taskDate:yyyy年M月d日 (dddd)}";
                }
                else if (task.StartTime != TimeSpan.Zero || task.EndTime != TimeSpan.Zero)
                {
                    // 分时任务显示时间
                    detail += $"时间：{task.StartTime:hh\\:mm} - {task.EndTime:hh\\:mm}";
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

                e.Handled = true; // 标记事件已处理
            }
            catch (Exception ex)
            {
                // 记录异常，但不中断应用程序
                Console.WriteLine($"显示任务详情时发生异常: {ex.Message}");
                MessageBox.Show($"显示任务详情时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void AddTaskButton_Click(object sender, RoutedEventArgs e)
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

                try
                {
                    // 直接使用 TaskService 保存任务
                    var taskService = App.Services.GetRequiredService<ITaskService>();
                    await taskService.UpdateTaskAsync(task);

                    // 任务已保存到数据库，现在添加到视图模型
                    _viewModel.AddTask(task, true); // 使用 forceAdd 参数，避免重复保存到数据库

                    // 添加消息提示任务已保存
                    MessageBox.Show($"任务「{task.Name}」已成功保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 添加强制刷新
                    if (dialog.ResultTask.IsAllDay)
                    {
                        // 强制更新 UI
                        WeekContentGrid.UpdateLayout();
                    }
                }
                catch (Exception ex)
                {
                    // 显示错误信息
                    MessageBox.Show($"保存任务失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            AddTaskButton.Visibility = Visibility.Collapsed;
            ClearSelection();
        }



        //任务删除（鼠标悬停
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var del = grid.FindName("DeleteButton") as Button;
                if (del != null)
                    del.Visibility = Visibility.Visible;
                var complete = grid.FindName("CompleteButton") as Button;
                if (complete != null)
                    complete.Visibility = Visibility.Visible;
            }
        }

        //任务删除（鼠标离开
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var del = grid.FindName("DeleteButton") as Button;
                if (del != null)
                    del.Visibility = Visibility.Collapsed;
                var complete = grid.FindName("CompleteButton") as Button;
                if (complete != null)
                    complete.Visibility = Visibility.Collapsed;
            }
        }

        // 全天任务鼠标进入事件
        private void AllDayGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var button = FindVisualChild<Button>(grid, b => b.Name == "DeleteButton");
                if (button != null)
                    button.Visibility = Visibility.Visible;
            }
        }

        // 全天任务鼠标离开事件
        private void AllDayGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var button = FindVisualChild<Button>(grid, b => b.Name == "DeleteButton");
                if (button != null)
                    button.Visibility = Visibility.Collapsed;
            }
        }

        private void DeleteAllDayButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WeekViewModel.TaskBlock task)
            {
                var vm = DataContext as WeekViewModel;
                if (vm?.RemoveTaskBlockCommand.CanExecute(task) == true)
                {
                    vm.RemoveTaskBlockCommand.Execute(task);
                }
            }
            e.Handled = true;
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
