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
                var taskService = App.Services.GetRequiredService<ITaskService>();

                DataContext = _viewModel;

                // 初始化视图模型和事件处理
                InitializeViewModel();

                // 初次加载时更新页面日期块
                UpdateDateDisplay(_viewModel.CurrentDate);

                // 强制初始加载
                _viewModel.LoadTasksForCurrentWeek();

<<<<<<< HEAD
                InitializeEvents();
=======

>>>>>>> a5523a6 (临时保存：切换到自己分支之前的未完成工作)
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

        // 供 WeekContentGrid 调用的方法
        private async void WeekContentGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 如果详情卡片是可见的，点击空白区域时隐藏它
            if (TaskDetailsCard.Visibility == Visibility.Visible)
            {
                TaskDetailsCard.Visibility = Visibility.Collapsed;
                _currentVisibleTaskId = -1;
            }

            // 获取原始点击源
            var originalSource = e.OriginalSource as DependencyObject;
            TaskBlock clickedTaskBlock = null;
            Border clickedBorder = null;

            // 添加对 Run 对象的特殊处理
            if (originalSource is System.Windows.Documents.Run run)
            {
                // 对于文本元素，尝试获取父级 TextBlock
                var parent = run.Parent as DependencyObject;
                if (parent != null)
                {
                    originalSource = parent;
                }
            }

            // 向上查找视觉树，确定是否点击了任务块
            try
            {
                DependencyObject current = originalSource;
                while (current != null)
                {
                    // 检查是否是边框且有TaskBlock数据
                    if (current is Border border)
                    {
                        clickedBorder = border;
                        clickedTaskBlock = border.DataContext as TaskBlock ?? border.Tag as TaskBlock;

                        // 如果找到了任务块，跳出循环
                        if (clickedTaskBlock != null)
                            break;
                    }

                    try
                    {
                        current = VisualTreeHelper.GetParent(current);
                    }
                    catch (InvalidOperationException)
                    {
                        break; // 元素不是Visual，跳出循环
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"查找任务块时出错: {ex.Message}");
            }

            // 如果点击了任务块，处理任务块点击事件
            if (clickedTaskBlock != null && clickedBorder != null)
            {
                // 转发到TaskBlock_MouseLeftButtonDown处理
                TaskBlock_MouseLeftButtonDown(clickedBorder, e);
                return; // 任务块点击已处理，不继续处理空白区域点击
            }

            // 以下是处理点击空白区域的逻辑
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

<<<<<<< HEAD

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
=======
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
        /// 点击任务块时，弹出编辑窗口并保存修改
        /// </summary>
        /// 
        private void TaskBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 从 Element.Tag 获取 TaskBlock
            var element = sender as FrameworkElement;
            var block = element?.Tag as TaskBlock;
            if (block == null) return;

            // 如果是课程任务，不显示卡片，继续原有的编辑逻辑
            if (block.IsCourse)
            {
                // 从 DataContext 拿到 ViewModel
                var vm = (TimeController.ViewModels.WeekViewModel)DataContext;
                // 根据 Id 找到对应 TaskModel
                var taskModel = vm.Tasks.FirstOrDefault(t => t.Id == block.Id);
                if (taskModel == null) return;

                // 弹出编辑窗口
                var dialog = new EditTaskWindow(taskModel);
                if (dialog.ShowDialog() == true)
                {
                    // 用户保存后，重新加载本周任务
                    vm.LoadTasksForCurrentWeek();
                }
                return;
            }

            // 如果当前显示的是同一个任务的详情卡片，则隐藏卡片
            if (_currentVisibleTaskId == block.Id && TaskDetailsCard.Visibility == Visibility.Visible)
            {
                TaskDetailsCard.Visibility = Visibility.Collapsed;
                _currentVisibleTaskId = -1;
                e.Handled = true;
                return;
            }

            // 设置详情卡片的内容
            CardNoteText.Text = string.IsNullOrEmpty(block.Note) ?
                "任务备注: 无" : $"任务备注: {block.Note}";
            CardTypeText.Text = $"任务类型: {block.Type}";

            // 根据是否为全天任务设置不同的时间显示
            if (block.IsAllDay)
            {
                CardTimeText.Text = "任务时间: 全天";
            }
            else
            {
                CardTimeText.Text = $"任务时间: {block.StartTime.ToString(@"hh\:mm")} - {block.EndTime.ToString(@"hh\:mm")}";
            }

            // 获取鼠标点击的位置（相对于当前元素）
            Point mousePos = e.GetPosition(element);

            // 将点击位置转换为屏幕坐标
            Point screenPoint = element.PointToScreen(mousePos);

            // 将屏幕坐标转换为相对于RootCanvas的坐标
            Point canvasPoint = RootCanvas.PointFromScreen(screenPoint);

            // 设置卡片位置，确保不超出边界
            double cardLeft = canvasPoint.X + 10; // 右侧偏移10像素
            double cardTop = canvasPoint.Y + 10;  // 下方偏移10像素

            // 确保卡片不会超出右边界
            if (cardLeft + TaskDetailsCard.ActualWidth > RootCanvas.ActualWidth)
            {
                cardLeft = Math.Max(0, canvasPoint.X - TaskDetailsCard.ActualWidth - 10);
            }

            // 确保卡片不会超出下边界
            if (cardTop + TaskDetailsCard.ActualHeight > RootCanvas.ActualHeight)
            {
                cardTop = Math.Max(0, canvasPoint.Y - TaskDetailsCard.ActualHeight - 10);
            }

            Canvas.SetLeft(TaskDetailsCard, cardLeft);
            Canvas.SetTop(TaskDetailsCard, cardTop);

            // 显示卡片并记录当前显示的任务ID
            TaskDetailsCard.Visibility = Visibility.Visible;
            _currentVisibleTaskId = block.Id;

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
>>>>>>> a5523a6 (临时保存：切换到自己分支之前的未完成工作)
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



<<<<<<< HEAD
        //任务删除（鼠标悬停
=======

        // 悬停进来：显示按钮
>>>>>>> a5523a6 (临时保存：切换到自己分支之前的未完成工作)
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var button = grid.FindName("DeleteButton") as Button;
                if (button != null)
                    button.Visibility = Visibility.Visible;
            }
        }

        //任务删除（鼠标离开
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                var button = grid.FindName("DeleteButton") as Button;
                if (button != null)
                    button.Visibility = Visibility.Collapsed;
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
