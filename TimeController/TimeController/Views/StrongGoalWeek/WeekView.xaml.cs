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
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using static TimeController.ViewModels.WeekViewModel;

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

                DataContext = _viewModel;

                // 初始化视图模型和事件处理
                InitializeViewModel();
                InitializeEvents();

                // 初次加载时更新页面日期块
                UpdateDateDisplay(_viewModel.CurrentDate);

                // 强制初始加载
                _viewModel.LoadTasksForCurrentWeek();


            }
            catch (Exception ex)
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
                // 对于普通任务，显示简单确认对话框
                if (!block.IsCourse)
                {
                    string message = $"确定要删除任务 \"{block.Name}\" 吗？";
                    string title = "确认删除";

                    var result = MessageBox.Show(
                        message,
                        title,
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    return result == MessageBoxResult.Yes;
                }
                else
                {
                    // 对于课程任务，不在这里处理确认，返回 true 让 ViewModel 中的逻辑处理
                    // 这样 ViewModel 中的课程确认对话框就会正常显示
                    return true;
                }
            };

        }

        // 处理任务冲突
        private void OnTaskConflictDetected(TaskModel newTask, List<WeekViewModel.TaskBlock> conflicts)
        {
            Console.WriteLine($"收到冲突检测事件 - 任务: {newTask.Name}, 冲突数: {conflicts.Count}");

            // 只获取课程冲突，因为我们只关心课程冲突
            var courseConflicts = conflicts.Where(c => c.IsCourse).ToList();

            // 如果有课程冲突，显示警告并阻止添加
            if (courseConflicts.Any())
            {
                var courseNames = string.Join("\n", courseConflicts.Select(c => $"- {c.Name}"));
                string message = $"无法添加任务，与以下课程时间冲突:\n{courseNames}\n\n请修改任务时间以避免与课程冲突。";

                // 弹出警告对话框（只有确定按钮）
                MessageBox.Show(
                    message,
                    "课程时间冲突",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                Console.WriteLine("检测到课程冲突，阻止添加任务");
                return; // 直接返回，不继续处理
            }

            // 对于非课程冲突，保持原有处理逻辑
            var taskConflicts = conflicts.Where(c => !c.IsCourse).ToList();
            if (taskConflicts.Any())
            {
                string message = "";
                var taskNames = string.Join("\n", taskConflicts.Select(c => $"- {c.Name}"));
                message += $"该时间段与以下任务冲突:\n{taskNames}\n";
                message += "\n确认后将会删除原有冲突的任务，是否继续?";

                // 弹出确认对话框
                var result = MessageBox.Show(
                    message,
                    "任务时间冲突",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                // 如果用户选择"是"，则处理冲突并添加新任务
                if (result == MessageBoxResult.Yes)
                {
                    Console.WriteLine("用户确认处理任务冲突，开始执行HandleConflictAndAddTask");
                    var viewModel = DataContext as WeekViewModel;
                    if (viewModel != null)
                    {
                        try
                        {
                            // 只传递非课程冲突，因为我们只处理任务冲突
                            viewModel.HandleConflictAndAddTask(newTask, taskConflicts);

                            // 显示通知
                            MessageBox.Show(
                                $"已删除 {taskConflicts.Count} 个冲突任务，并添加新任务：{newTask.Name}",
                                "操作完成",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"处理冲突时出错: {ex.Message}");
                            MessageBox.Show($"处理冲突时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
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
                    // 添加并保存课程
                    var addedTasks = await _viewModel.AddAndSaveCourse(newCourse);

                    // 显示成功消息
                    if (addedTasks.Count > 0)
                    {
                        MessageBox.Show($"成功添加课程：{newCourse.Name}\n上课周次：{newCourse.WeekPattern}",
                            "添加成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("课程时间冲突"))
                {
                    // 显示冲突信息
                    MessageBox.Show(ex.Message, "课程时间冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"添加课程失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // 记录当前显示的详情面板和任务ID
        private StackPanel _currentVisibleDetailsPanel;
        private Border _currentHighlightedTask;
        private int _currentVisibleTaskId = -1;


        public void UpdateCardPositionForElement(FrameworkElement element)
        {
            if (element != null)
            {
                // 计算任务块在页面中的位置
                var taskPosition = element.TransformToVisual(RootCanvas).Transform(new Point(0, 0));

                // 确保卡片有正确的尺寸
                TaskDetailsCard.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                TaskDetailsCard.Arrange(new Rect(0, 0, TaskDetailsCard.DesiredSize.Width, TaskDetailsCard.DesiredSize.Height));

                // 设置卡片位置 - 根据任务块位置计算
                double cardLeft = taskPosition.X + element.ActualWidth - 5; // 让卡片与任务块右侧略微重叠
                double cardTop = taskPosition.Y + 5; // 将卡片定位在任务块正下方的位置，略有偏移

                // 如果卡片会超出右边界，则显示在任务块的左侧
                if (cardLeft + TaskDetailsCard.ActualWidth > RootCanvas.ActualWidth)
                {
                    cardLeft = Math.Max(0, taskPosition.X - TaskDetailsCard.ActualWidth + 5); // 让卡片与任务块左侧略微重叠
                }


                // 如果卡片会超出下边界，调整垂直位置
                if (cardTop + TaskDetailsCard.ActualHeight > RootCanvas.ActualHeight)
                {
                    cardTop = Math.Max(0, RootCanvas.ActualHeight - TaskDetailsCard.ActualHeight - 2);
                }

                // 应用计算出的位置
                Canvas.SetLeft(TaskDetailsCard, cardLeft);
                Canvas.SetTop(TaskDetailsCard, cardTop);
            }
        }

        public void CloseTaskDetailsCard()
        {
            TaskDetailsCard.Visibility = Visibility.Collapsed;
            _currentVisibleTaskId = -1;
        }

        /// <summary>
        /// 点击任务块时：
        /// - 如果是课程任务，什么也不弹（或继续原有详情卡逻辑）
        /// - 否则弹出编辑窗口
        /// </summary>
        private void TaskBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 拿到绑定的 TaskBlock
            if (!(sender is FrameworkElement elt) || !(elt.Tag is TaskBlock block))
                return;

            // 如果这是课程任务，就让事件继续冒泡，不打开编辑窗口
            if (block.IsCourse)
                return;  // 或者：e.Handled = false; 让别的 MouseDown 逻辑接管

            // 以下只处理“非课程”——弹编辑对话框
            var vm = (WeekViewModel)DataContext;
            var taskModel = vm.Tasks.FirstOrDefault(t => t.Id == block.Id);
            if (taskModel == null) return;

            var dialog = new EditTaskWindow(taskModel)
            {
                Owner = Window.GetWindow(this) ?? Application.Current.MainWindow
            };
            if (dialog.ShowDialog() == true)
            {
                vm.LoadTasksForCurrentWeek();
            }

            // 标记已处理，避免其他 MouseDown 再次响应
            e.Handled = true;
        }





        // 关闭详情卡片时，如果点击的不是卡片本身或其子元素，则隐藏卡片
        public void CloseTaskDetailsCardIfOutside(DependencyObject clickedElement)
        {
            if (TaskDetailsCard.Visibility == Visibility.Visible)
            {
                // 检查点击的元素是否是卡片本身或其子元素
                bool isClickInsideCard = IsElementDescendantOf(clickedElement, TaskDetailsCard);

                // 如果点击不在卡片内，关闭卡片
                if (!isClickInsideCard)
                {
                    CloseTaskDetailsCard();
                }
            }
        }

        // 检查元素是否是另一个元素的后代
        private bool IsElementDescendantOf(DependencyObject element, DependencyObject ancestor)
        {
            while (element != null)
            {
                if (element == ancestor)
                    return true;

                // 获取视觉树上的父元素
                try
                {
                    element = VisualTreeHelper.GetParent(element);
                }
                catch
                {
                    return false;
                }
            }
            return false;
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
            _clickedDate = null; // 清除选择的日期

            _selectedColumnIndex = null;
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

        //添加任务按钮点击事件
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
                    // 创建一个标记，用于跟踪任务是否应该被保存
                    bool shouldSaveTask = true;

                    // 设置任务冲突处理的事件处理程序
                    Action<TaskModel, List<TaskBlock>> conflictHandler = null;
                    conflictHandler = async (conflictTask, conflicts) =>
                    {
                        // 获取课程冲突
                        var courseConflicts = conflicts.Where(c => c.IsCourse).ToList();

                        // 如果有课程冲突，显示警告并阻止添加
                        if (courseConflicts.Any())
                        {
                            var courseNames = string.Join("\n", courseConflicts.Select(c => $"- {c.Name}"));
                            string message = $"无法添加任务，与以下课程时间冲突:\n{courseNames}\n\n请修改任务时间以避免与课程冲突。";

                            // 弹出警告对话框
                            Application.Current.Dispatcher.Invoke(() =>
                                MessageBox.Show(
                                    message,
                                    "课程时间冲突",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning)
                            );

                            // 设置标记为false，不保存任务
                            shouldSaveTask = false;

                            // 移除临时事件处理程序
                            _viewModel.ConflictDetected -= conflictHandler;
                            return;
                        }

                        // 获取任务冲突，保持原有处理逻辑
                        var taskConflicts = conflicts.Where(c => !c.IsCourse).ToList();
                        if (taskConflicts.Any())
                        {
                            var taskNames = string.Join("\n", taskConflicts.Select(c => $"- {c.Name}"));
                            string message = $"该时间段与以下任务冲突:\n{taskNames}\n\n确认后将会删除原有冲突的任务，是否继续?";

                            // 弹出确认对话框
                            var result = Application.Current.Dispatcher.Invoke(() =>
                                MessageBox.Show(
                                    message,
                                    "任务时间冲突",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning)
                            );

                            if (result == MessageBoxResult.Yes)
                            {
                                // 用户选择处理冲突并继续
                                await _viewModel.HandleConflictAndAddTask(conflictTask, taskConflicts);

                                // 显示通知
                                Application.Current.Dispatcher.Invoke(() =>
                                    MessageBox.Show(
                                        $"已删除 {taskConflicts.Count} 个冲突任务，并添加新任务：{conflictTask.Name}",
                                        "操作完成",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information)
                                );
                            }
                            else
                            {
                                // 用户取消冲突处理，设置标记为false
                                shouldSaveTask = false;
                            }
                        }

                        // 移除临时事件处理程序
                        _viewModel.ConflictDetected -= conflictHandler;
                    };


                    // 临时订阅冲突事件
                    _viewModel.ConflictDetected += conflictHandler;

                    // 仅当用户确认处理冲突或无冲突时才保存任务
                    var taskService = App.Services.GetRequiredService<ITaskService>();

                    // 使用同步方法检查冲突
                    if (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
                    {
                        // 检查与任务的冲突
                        var (hasTaskConflict, taskConflicts) = _viewModel.CheckTimeConflicts(task);

                        // 检查与课程的冲突
                        var (hasCourseConflict, courseConflicts) = _viewModel.CheckCourseConflicts(task);

                        // 合并冲突列表
                        var allConflicts = taskConflicts.Concat(courseConflicts).ToList();

                        if (allConflicts.Any())
                        {
                            // 手动触发冲突处理，直接在当前UI线程上调用
                            conflictHandler(task, allConflicts);

                            // 等待一段时间，让异步处理完成
                            await Task.Delay(100);
                        }
                    }

                    if (shouldSaveTask)
                    {
                        // 保存到数据库
                        await taskService.UpdateTaskAsync(task);

                        // 添加到视图模型，使用forceAdd=true跳过冲突检查
                        _viewModel.AddTask(task, true);

                        // 添加强制刷新
                        if (dialog.ResultTask.IsAllDay)
                        {
                            // 强制更新 UI
                            WeekContentGrid.UpdateLayout();
                        }
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




        // 悬停进来：显示按钮
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid g && g.FindName("ActionButtons") is UIElement btns)
                btns.Visibility = Visibility.Visible;
        }

        // 悬停离开：隐藏按钮
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid g && g.FindName("ActionButtons") is UIElement btns)
                btns.Visibility = Visibility.Collapsed;
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

        private void TaskColumn_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Border border) || !int.TryParse(border.Tag?.ToString(), out int index))
                return;

            // 如果再次点击同一列，就取消选中
            if (_selectedColumnIndex.HasValue && _selectedColumnIndex.Value == index)
            {
                ClearSelection();
                e.Handled = true;
                return;
            }

            // 记录当前选中列
            _selectedColumnIndex = index;

            // 计算那天的日期
            DateTime monday = _viewModel.CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);
            _clickedDate = monday.AddDays(index);

            // 高亮并显示加号
            HighlightColumn(index);

            // 把加号放到鼠标位置
            var position = e.GetPosition(RootCanvas);
            Canvas.SetLeft(AddTaskButton, position.X - AddTaskButton.Width / 2);
            Canvas.SetTop(AddTaskButton, position.Y - AddTaskButton.Height / 2);
            AddTaskButton.Visibility = Visibility.Visible;

            e.Handled = true;
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

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentDate = DateTime.Today;

            int todayIndex = _viewModel.DateColumns.ToList().FindIndex(c => c.IsToday);
            if (todayIndex >= 0)
            {
                HighlightColumn(todayIndex);
                double colWidth = WeekContentGrid.ActualWidth / 7;
                TimeTasksScrollViewer.ScrollToHorizontalOffset(colWidth * todayIndex);
            }
        }

        private int? _selectedColumnIndex;

        private void WeekContentGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject src &&
                FindVisualChild<Grid>(src, g => g.Tag is WeekViewModel.TaskBlock) != null)
                return; // ignore clicks on task blocks

            Point pos = e.GetPosition(WeekContentGrid);
            double colWidth = WeekContentGrid.ActualWidth / 7;
            int index = Math.Max(0, Math.Min(6, (int)(pos.X / colWidth)));
            SelectColumn(index, e.GetPosition(RootCanvas));
        }

        public void OnDateColumnClicked(int index, MouseButtonEventArgs e)
        {
            SelectColumn(index, e.GetPosition(RootCanvas));
        }

        private void SelectColumn(int index, Point canvasPos)
        {
            DateTime date = _viewModel.DateColumns[index].Date;
            _clickedDate = date;
            _selectedColumnIndex = index;
            HighlightColumn(index);

            Canvas.SetLeft(AddTaskButton, canvasPos.X - AddTaskButton.Width / 2);
            Canvas.SetTop(AddTaskButton, canvasPos.Y - AddTaskButton.Height / 2);
            AddTaskButton.Visibility = Visibility.Visible;
            AddTaskButton.Focus();
        }

        private void RootGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AddTaskButton.Visibility != Visibility.Visible)
                return;

            if (e.OriginalSource is DependencyObject src && IsElementDescendantOf(src, AddTaskButton))
                return;

            int? index = null;
            if (FindVisualParent<DateColumnControl>(e.OriginalSource as DependencyObject) is DateColumnControl col &&
                col.DataContext is DateColumnViewModel vm)
            {
                index = vm.Index;
            }
            else
            {
                Point pos = e.GetPosition(WeekContentGrid);
                if (pos.X >= 0 && pos.X <= WeekContentGrid.ActualWidth &&
                    pos.Y >= 0 && pos.Y <= WeekContentGrid.ActualHeight)
                {
                    double colWidth = WeekContentGrid.ActualWidth / 7;
                    index = Math.Max(0, Math.Min(6, (int)(pos.X / colWidth)));
                }
            }

            if (!index.HasValue || index.Value != _selectedColumnIndex)
            {
                ClearSelection();
            }
        }

        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && child is not T)
            {
                child = VisualTreeHelper.GetParent(child);
            }
            return child as T;
        }

    }

}
