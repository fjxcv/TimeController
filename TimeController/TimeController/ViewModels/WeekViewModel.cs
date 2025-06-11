using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using TimeController.Models;
using System.Windows;
using System.Globalization;
using TimeController.Services;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Data;
using System.Data;

namespace TimeController.ViewModels
{
    public class WeekViewModel : INotifyPropertyChanged
    {
        private DateTime _currentDate;
        private readonly ITaskService _taskService;
        public IEnumerable<TaskBlock> AllDayTaskBlocks => TaskBlocks.Where(t => t.IsAllDay);
        public IEnumerable<TaskBlock> TimedTaskBlocks => TaskBlocks.Where(t => !t.IsAllDay);

        public ITaskService TaskService => _taskService;
        public ObservableCollection<TaskModel> Tasks { get; set; } = new ObservableCollection<TaskModel>();
        public ObservableCollection<TaskBlock> TaskBlocks { get; } = new ObservableCollection<TaskBlock>();
        // 课程任务块集合
        public ObservableCollection<TaskBlock> CourseTaskBlocks { get; } = new ObservableCollection<TaskBlock>();
        public ObservableCollection<DateColumnViewModel> DateColumns { get; } = new ObservableCollection<DateColumnViewModel>();


        public event Action<TaskModel>? SaveRequested;
        // 添加一个确认删除的事件
        public event Func<TaskBlock, Task<bool>> DeleteConfirmationRequested;


        public ICommand RemoveTaskBlockCommand { get; private set; }
        public ICollectionView TimedTaskBlocksView { get; private set; }

        public WeekViewModel(ITaskService taskService)
        {
            _taskService = taskService;
            Initialize();

        }

        public string MonthText { get; private set; }
        public string WeekText { get; private set; }

        public ICommand PreviousWeekCommand { get; private set; }
        public ICommand NextWeekCommand { get; private set; }
        public ICommand PreviousMonthCommand { get; private set; }
        public ICommand NextMonthCommand { get; private set; }

        public WeekViewModel()
        {
            // 尝试从服务定位器获取 TaskService
            try
            {
                _taskService = App.Services?.GetService(typeof(ITaskService)) as ITaskService;
                if (_taskService == null)
                {
                    Console.WriteLine("警告: 未能从服务容器获取任务服务，尝试创建本地实例...");

                    // 或者在这里创建临时的服务实例
                    // _taskService = new TaskService(new TaskDbContext(...));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取任务服务失败: {ex.Message}");
            }

            Initialize();
        }

        private void Initialize()
        {
            _currentDate = DateTime.Today;
            UpdateMonthText();
            UpdateWeekText();

            PreviousWeekCommand = new RelayCommand(_ => NavigateWeek(-7));
            NextWeekCommand = new RelayCommand(_ => NavigateWeek(7));
            PreviousMonthCommand = new RelayCommand(_ => NavigateMonth(-1));
            NextMonthCommand = new RelayCommand(_ => NavigateMonth(1));
            TimedTaskBlocksView = CollectionViewSource.GetDefaultView(TaskBlocks);
            TimedTaskBlocksView.Filter = obj => obj is TaskBlock block && !block.IsAllDay;

            SaveRequested += OnTaskSaved;
            RemoveTaskBlockCommand = new RelayCommand<TaskBlock>(RemoveTaskBlock);
            ToggleColumnExpandCommand = new RelayCommand<int>(ToggleColumnExpand);

            // 初始化日期列
            for (int i = 0; i < 7; i++)
            {
                DateColumns.Add(new DateColumnViewModel
                {
                    Index = i,
                    WeekDayText = GetWeekDayText(i),
                    AllDayTasks = AllDayTaskBlocksPerDay[i],
                    IsExpanded = ExpandedColumns[i]
                });
            }

            // 更新日期文本和月份状态
            UpdateDateColumns();

            // 如果服务可用，加载任务
            if (_taskService != null)
            {
                LoadTasksForCurrentWeek();
            }
        }

        // 检查特定列是否需要显示下拉按钮（全天任务数大于3个）
        public bool[] ShouldShowMoreButtonForColumn => Enumerable.Range(0, 7)
            .Select(i => AllDayTaskBlocksPerDay[i].Count > 2).ToArray();


        private string GetWeekDayText(int dayIndex)
        {
            switch (dayIndex)
            {
                case 0: return "一";
                case 1: return "二";
                case 2: return "三";
                case 3: return "四";
                case 4: return "五";
                case 5: return "六";
                case 6: return "日";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 将课程转换为任务模型 - 根据重复规则创建
        /// </summary>
        private TaskModel ConvertCourseToTaskModel(Course course)
        {
            // 星期几对应的索引
            int dayIndex = GetDayIndex(course.DayOfWeek);

            // 创建任务模型的基础信息
            var taskModel = new TaskModel
            {
                Name = course.Name,
                Note = $"教师:{course.Teacher}, 地点:{course.Location}",
                Type = TaskType.学习学业,
                Mode = TaskMode.Strong,
                // 占位日期，实际会根据当前周动态计算
                PlannedDate = DateTime.Today,
                IsAllDay = false,
                StartTime = course.StartTime,
                EndTime = course.EndTime,
                Status = MyTaskStatus.Pending,
                IsReminderEnabled = true,
                CreatedAt = DateTime.Now,
                // 添加课程特殊标记
                IsCourseTask = true,
                WeekDay = dayIndex // 存储星期几(0=周一)
            };

            return taskModel;
        }

        /// <summary>
        /// 一步完成课程的添加和保存
        /// </summary>
        public async Task<TaskModel> AddAndSaveCourse(Course course)
        {
            // 1. 转换为任务模型
            TaskModel taskModel = ConvertCourseToTaskModel(course);

            // 设置为当前周的日期，而不是使用默认日期
            DateTime monday = GetCurrentWeekMonday();
            taskModel.PlannedDate = monday.AddDays(taskModel.WeekDay);
            // 2. 保存到数据库
            if (_taskService != null)
            {
                await _taskService.UpdateTaskAsync(taskModel);
                Console.WriteLine($"课程 {course.Name} 已保存到数据库，ID={taskModel.Id}");
            }

            // 3. 添加到任务列表
            Tasks.Add(taskModel);

            // 4. 刷新当前视图
            LoadTasksForCurrentWeek();

            return taskModel;
        }


        /// <summary>
        /// 获取当前周的周一日期
        /// </summary>
        private DateTime GetCurrentWeekMonday()
        {
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
            {
                monday = monday.AddDays(-1);
            }
            return monday;
        }

        /// <summary>
        /// 获取星期几对应的索引 (0=周一, 1=周二...)
        /// </summary>
        private int GetDayIndex(string dayOfWeek)
        {
            return dayOfWeek.Trim() switch
            {
                "周一" or "星期一" or "1" or "一" => 0,
                "周二" or "星期二" or "2" or "二" => 1,
                "周三" or "星期三" or "3" or "三" => 2,
                "周四" or "星期四" or "4" or "四" => 3,
                "周五" or "星期五" or "5" or "五" => 4,
                "周六" or "星期六" or "6" or "六" => 5,
                "周日" or "星期日" or "周天" or "星期天" or "7" or "日" or "天" => 6,
                _ => 0
            };
        }
        //导入改
        private DateTime? _semesterStartDate;

        public DateTime? SemesterStartDate
        {
            get => _semesterStartDate;
            set
            {
                if (_semesterStartDate != value)
                {
                    _semesterStartDate = value;
                    OnPropertyChanged();
                    UpdateSemesterWeekText(); // 更新学期周数文本
                }
            }
        }

        //导入改
        private string _semesterWeekText = "";
        public string SemesterWeekText
        {
            get => _semesterWeekText;
            private set
            {
                if (_semesterWeekText != value)
                {
                    _semesterWeekText = value;
                    OnPropertyChanged();
                }
            }
        }

        private void UpdateSemesterWeekText()
        {
            if (!_semesterStartDate.HasValue)
            {
                SemesterWeekText = "";
                return;
            }

            // 获取本周一
            DateTime currentMonday = CurrentDate;
            while (currentMonday.DayOfWeek != DayOfWeek.Monday)
            {
                currentMonday = currentMonday.AddDays(-1);
            }

            // 获取学期第一周的周一
            // 获取学期第一周的周一
            DateTime semesterMonday = _semesterStartDate.Value;
            while (semesterMonday.DayOfWeek != DayOfWeek.Monday)
            {
                semesterMonday = semesterMonday.AddDays(-1);
            }

            // 计算相差的周数
            int weeksDiff = (int)Math.Round((currentMonday - semesterMonday).TotalDays / 7) + 1;

            SemesterWeekText = weeksDiff > 0
                ? $"学期第 {weeksDiff} 周"
                : $"学期前 {Math.Abs(weeksDiff) + 2} 周";
        }

        // 更新日期列的日期文本和月份状态
        private void UpdateDateColumns()
        {
            // 获取该周周一
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            // 当前选择的月份
            int currentMonth = CurrentDate.Month;

            for (int i = 0; i < 7; i++)
            {
                DateTime currentDay = monday.AddDays(i);
                var column = DateColumns[i];

                // 更新日期文本
                if (currentDay.Month != currentMonth)
                {
                    column.DateText = $"{currentDay.Month}月{currentDay.Day}";
                    column.IsCurrentMonth = false;
                }
                else
                {
                    column.DateText = currentDay.Day.ToString();
                    column.IsCurrentMonth = true;
                }
            }
        }

        // 在 CurrentDate 属性发生变化时调用此方法
        private void OnCurrentDateChanged()
        {
            UpdateDateColumns();
            UpdateSemesterWeekText();
            LoadTasksForCurrentWeek();
        }

        public ObservableCollection<TaskBlock>[] AllDayTaskBlocksPerDay { get; } =
    Enumerable.Range(0, 7).Select(_ => new ObservableCollection<TaskBlock>()).ToArray();


        private void OnTaskSaved(TaskModel task)
        {
            // 只需调用 AddTask，所有定位和添加逻辑都在 AddTask 里完成
            AddTask(task);
        }

        // 添加任务到视图
        private void AddTaskToView(TaskModel task)
        {
            // 计算本周一
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            // 添加调试输出
            Console.WriteLine($"尝试添加任务: {task.Name}, 计划日期: {task.PlannedDate:yyyy-MM-dd}, 本周一: {monday:yyyy-MM-dd}");
            int column = (task.PlannedDate - monday).Days;
            if (column < 0 || column > 6) return; // 不在当前周的任务不显示

            // 检查是否为课程任务
            bool isCourse = task.Type == TaskType.学习学业 &&
                !string.IsNullOrEmpty(task.Note) &&
                (task.Note.Contains("教师:") || task.Note.Contains("地点:"));


            // 如果是课程任务，则只添加到CourseTaskBlocks，不添加到普通任务
            if (isCourse)
            {
                var courseBlock = new TaskBlock
                {
                    Name = task.Name,
                    Note = task.Note,
                    Type = task.Type,
                    StartTime = task.StartTime ?? TimeSpan.Zero,
                    EndTime = task.EndTime ?? TimeSpan.Zero,
                    Brush = new SolidColorBrush(Color.FromRgb(230, 247, 255)), // 浅蓝色背景
                    Column = column,
                    Row = task.StartTime.HasValue ? task.StartTime.Value.Hours : 0,
                    RowSpan = (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
                              ? Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours) + 1 : 1,
                    Id = task.Id,
                    IsCourse = true
                };

                CourseTaskBlocks.Add(courseBlock);
                Console.WriteLine($"添加课程到CourseTaskBlocks: {task.Name}");
            }
            else
            {
                // 非课程任务，添加到普通任务集合
                var taskBlock = new TaskBlock
                {
                    Name = task.Name,
                    Note = task.Note,
                    Type = task.Type,
                    IsAllDay = task.IsAllDay,  // 确保设置IsAllDay属性
                    StartTime = task.StartTime ?? TimeSpan.Zero,
                    EndTime = task.EndTime ?? TimeSpan.Zero,
                    Brush = GetBrushForTaskType(task.Type),
                    Column = column,
                    Row = task.StartTime.HasValue ? task.StartTime.Value.Hours : 0,
                    RowSpan = (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
                              ? Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours) + 1 : 1,
                    Id = task.Id,
                    IsCourse = false
                };

                TaskBlocks.Add(taskBlock);
            }

            OnPropertyChanged(nameof(AllDayTaskBlocks));
        }

        // 添加任务的事件，用于通知View层处理冲突
        public event Action<TaskModel, List<TaskBlock>>? ConflictDetected;

        // 添加任务方法，增加冲突处理逻辑
        public async void AddTask(TaskModel task, bool forceAdd = false)
        {
            // 非强制添加模式下，先检查时间冲突
            if (!forceAdd && !task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
            {
                var (hasConflict, conflicts) = CheckTimeConflicts(task);

                if (hasConflict)
                {
                    // 通知View层处理冲突
                    ConflictDetected?.Invoke(task, conflicts);
                    return; // 不继续执行添加，等待用户决定
                }
            }

            // 无冲突或强制添加，执行正常的添加流程
            Tasks.Add(task);

            // 数据库持久化
            if (_taskService != null)
            {
                await _taskService.UpdateTaskAsync(task);
            }

            // 只有当任务在当前周才添加到视图
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            DateTime sunday = monday.AddDays(6);

            if (task.PlannedDate >= monday && task.PlannedDate <= sunday)
            {
                AddTaskToView(task);
                // 关键修改：手动添加全天任务到 AllDayTaskBlocksPerDay
                if (task.IsAllDay)
                {
                    int column = (task.PlannedDate - monday).Days;
                    if (column >= 0 && column < 7)
                    {
                        // 查找刚刚添加的任务块
                        var addedBlock = TaskBlocks.LastOrDefault(b => b.Id == task.Id);
                        if (addedBlock != null)
                        {
                            // 直接添加到对应天的全天任务集合
                            AllDayTaskBlocksPerDay[column].Add(addedBlock);
                            Console.WriteLine($"手动添加全天任务到第{column}列: {task.Name}");
                        }
                    }
                }

            }
            OnPropertyChanged(nameof(TimedTaskBlocks));
            OnPropertyChanged(nameof(AllDayTaskBlocks));
            OnPropertyChanged(nameof(AllDayTaskBlocksPerDay));

            // 更新对应日期列
            if (task.IsAllDay)
            {
                int column = (task.PlannedDate - monday).Days;
                if (column >= 0 && column < 7)
                {
                    DateColumns[column].RefreshAllDayTasksView();
                    OnPropertyChanged(nameof(ShouldShowMoreButtonForColumn));
                }
            }
            // 确保 TimedTaskBlocksView 也被刷新
            TimedTaskBlocksView.Refresh();
        }


        // 处理冲突：删除冲突任务并添加新任务
        public async Task HandleConflictAndAddTask(TaskModel newTask, List<TaskBlock> conflicts)
        {
            // 先删除所有冲突的任务
            foreach (var conflict in conflicts)
            {
                // 从UI集合移除
                TaskBlocks.Remove(conflict);

                // 找到对应的TaskModel并从数据库删除
                var taskToRemove = Tasks.FirstOrDefault(t => t.Id == conflict.Id);
                if (taskToRemove != null)
                {
                    Tasks.Remove(taskToRemove);
                    if (_taskService != null)
                    {
                        await _taskService.DeleteTaskAsync(taskToRemove);
                    }
                }
            }

            // 然后以强制模式添加新任务
            AddTask(newTask, true);
            TimedTaskBlocksView.Refresh(); // 强制刷新视图
            OnPropertyChanged(nameof(AllDayTaskBlocks));
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

        //任务块类
        public class TaskBlock
        {
            public string Name { get; set; }
            public string Note { get; set; }
            public TaskType Type { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public Brush Brush { get; set; }
            public bool IsAllDay { get; set; }
            public bool IsCourse { get; set; }

            // 定位属性
            public int Column { get; set; } // 星期几（0=周一，1=周二...）
            public int Row { get; set; }    // 对应小时行（0=全天，1=0:00, 2=1:00...）
            public int RowSpan { get; set; } // 跨多少小时

            public int Id { get; set; }//Id
        }

        //当前日期
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (_currentDate != value)
                {
                    _currentDate = value;
                    OnPropertyChanged();
                    UpdateMonthText();
                    UpdateWeekText();
                    OnCurrentDateChanged();
                }
            }
        }

        private async void RemoveTaskBlock(TaskBlock block)
        {
            if (block == null) return;

            try
            {
                // 请求确认
                if (DeleteConfirmationRequested != null)
                {
                    bool confirmed = await DeleteConfirmationRequested(block);
                    if (!confirmed)
                        return; // 用户取消删除
                }

                Console.WriteLine($"开始删除任务: {block.Name}, ID: {block.Id}, 类型: {(block.IsAllDay ? "全天任务" : "分时任务")}");

                // 1. 首先找到对应的 TaskModel，确保有正确的引用
                var taskToDelete = Tasks.FirstOrDefault(t => t.Id == block.Id);
                if (taskToDelete == null)
                {
                    Console.WriteLine($"警告：找不到ID为 {block.Id} 的任务模型，无法删除");

                    // 如果内存中找不到，尝试强制从数据库删除（确保ID匹配）
                    if (_taskService != null && block.Id > 0)
                    {
                        try
                        {
                            // 创建一个临时任务对象用于删除
                            var tempTask = new TaskModel { Id = block.Id };
                            await _taskService.DeleteTaskAsync(tempTask);
                            Console.WriteLine($"已尝试强制从数据库删除ID={block.Id}的任务");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"强制删除失败: {ex.Message}");
                        }
                    }

                    // 无论如何，继续UI更新
                }
                else
                {
                    // 2. 从数据库删除任务
                    if (_taskService != null)
                    {
                        try
                        {
                            // 直接传递任务ID和引用以确保删除正确的任务
                            Console.WriteLine($"删除数据库任务 ID={taskToDelete.Id}, Name={taskToDelete.Name}");
                            await _taskService.DeleteTaskAsync(taskToDelete);
                            Console.WriteLine($"数据库删除任务成功：ID = {taskToDelete.Id}");
                        }
                        catch (Exception ex)
                        {
                            // 数据库删除失败时，显示错误信息并中断操作
                            Console.WriteLine($"数据库删除任务失败：{ex.Message}");
                            MessageBox.Show($"无法从数据库删除任务: {ex.Message}", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            return; // 关键：如果数据库删除失败，不继续UI更新
                        }

                        // 3. 从内存集合中移除
                        Tasks.Remove(taskToDelete);
                        Console.WriteLine($"从内存集合移除任务：{taskToDelete.Name}，剩余任务数：{Tasks.Count}");
                    }
                    else
                    {
                        Console.WriteLine("错误: _taskService 为 null，无法从数据库删除任务");
                        MessageBox.Show("任务服务不可用，无法删除任务", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // 4. 从UI集合中移除相应的TaskBlock
                bool uiRemoved = false;

                if (block.IsCourse)
                {
                    uiRemoved = CourseTaskBlocks.Remove(block);
                    Console.WriteLine($"从课程集合移除: {uiRemoved}");
                }
                else if (block.IsAllDay)
                {
                    // 从全天任务集合中移除
                    int column = block.Column;
                    if (column >= 0 && column < 7)
                    {
                        bool removed = AllDayTaskBlocksPerDay[column].Remove(block);
                        Console.WriteLine($"从全天任务集合移除结果: {removed}");
                        uiRemoved = uiRemoved || removed;

                        // 更新该列的视图
                        DateColumns[column].RefreshAllDayTasksView();
                    }
                    // 确保从总TaskBlocks中也移除
                    uiRemoved = TaskBlocks.Remove(block) || uiRemoved;
                    Console.WriteLine($"从总任务集合移除结果: {uiRemoved}");
                }
                else
                {
                    // 普通分时任务
                    uiRemoved = TaskBlocks.Remove(block);
                    Console.WriteLine($"从分时任务集合移除结果: {uiRemoved}");
                }

                if (!uiRemoved)
                {
                    Console.WriteLine("警告: 任务从UI中移除失败");
                }

                // 5. 强制刷新所有相关视图
                OnPropertyChanged(nameof(AllDayTaskBlocks));
                OnPropertyChanged(nameof(AllDayTaskBlocksPerDay));
                OnPropertyChanged(nameof(ShouldShowMoreButtonForColumn));
                OnPropertyChanged(nameof(TimedTaskBlocks));
                TimedTaskBlocksView.Refresh();

                // 关键步骤：强制从数据库重新加载当前周任务，确保视图与数据库同步
                await Task.Delay(100); // 短暂延迟以确保数据库操作完成
                LoadTasksForCurrentWeek();

                Console.WriteLine("任务删除操作完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除任务过程中出错: {ex.Message}");
                MessageBox.Show($"删除任务时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // 修改 LoadTasksForCurrentWeek 方法，确保只加载数据库中的最新数据
        public async void LoadTasksForCurrentWeek()
        {
            try
            {
                Console.WriteLine("开始加载本周任务...");

                // 清空当前任务块集合
                TaskBlocks.Clear();
                CourseTaskBlocks.Clear();

                // 清空每天的全天任务集合
                for (int i = 0; i < 7; i++)
                {
                    AllDayTaskBlocksPerDay[i].Clear();
                }

                // 计算当前周的起止日期
                DateTime monday = GetCurrentWeekMonday();
                DateTime sunday = monday.AddDays(6);

                Console.WriteLine($"加载周视图 - 当前周: {monday:yyyy-MM-dd} 到 {sunday:yyyy-MM-dd}");

                // 确保我们直接从数据库获取最新数据
                if (_taskService != null)
                {
                    // 重新从数据库加载任务，确保与数据库同步
                    var weekTasks = await _taskService.GetTasksForDateRange(monday, sunday);
                    var courseTasks = await _taskService.GetCourseTasksForWeekAsync(CurrentDate);

                    // 更新内存中的任务集合（关键修改：更新而不是追加）
                    Tasks.Clear();
                    foreach (var task in weekTasks.Concat(courseTasks))
                    {
                        Tasks.Add(task);
                    }

                    Console.WriteLine($"从数据库加载了 {Tasks.Count} 个任务");

                    // 处理所有加载的任务
                    foreach (var task in Tasks)
                    {
                        // 特殊处理课程任务
                        if (task.IsCourseTask)
                        {
                            task.PlannedDate = monday.AddDays(task.WeekDay);
                        }

                        // 只添加当前周内的任务
                        if (task.PlannedDate >= monday && task.PlannedDate <= sunday)
                        {
                            AddTaskToView(task);
                        }
                    }

                    // 分组全天任务
                    foreach (var block in TaskBlocks.Where(t => t.IsAllDay))
                    {
                        if (block.Column >= 0 && block.Column < 7)
                        {
                            AllDayTaskBlocksPerDay[block.Column].Add(block);
                        }
                    }

                    foreach (var column in DateColumns)
                    {
                        column.RefreshAllDayTasksView();
                    }

                    // 通知UI更新
                    OnPropertyChanged(nameof(TaskBlocks));
                    OnPropertyChanged(nameof(CourseTaskBlocks));
                    OnPropertyChanged(nameof(AllDayTaskBlocks));
                    OnPropertyChanged(nameof(AllDayTaskBlocksPerDay));
                    OnPropertyChanged(nameof(ShouldShowMoreButtonForColumn));
                    OnPropertyChanged(nameof(ExpandedColumns));

                    // 额外刷新一次集合视图确保UI同步
                    TimedTaskBlocksView.Refresh();

                    Console.WriteLine($"任务加载完成 - 全天任务: {TaskBlocks.Count(t => t.IsAllDay)}, 分时任务: {TaskBlocks.Count(t => !t.IsAllDay)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载任务时发生错误: {ex.Message}");
            }
        }



        // 检查分时任务时间冲突
        public (bool hasConflict, List<TaskBlock> conflicts) CheckTimeConflicts(TaskModel newTask)
        {
            var conflicts = new List<TaskBlock>();

            if (newTask.StartTime.HasValue && newTask.EndTime.HasValue && newTask.StartTime.Value < newTask.EndTime.Value)
            {
                // 计算本周一
                DateTime monday = CurrentDate.Date;
                while (monday.DayOfWeek != DayOfWeek.Monday)
                    monday = monday.AddDays(-1);

                int column = (newTask.PlannedDate - monday).Days;

                // 检查同一天的分时任务是否有冲突
                foreach (var block in TaskBlocks)
                {
                    if (!block.IsAllDay && block.Column == column)
                    {
                        // 跳过无效时间段
                        if (block.StartTime >= block.EndTime) continue;

                        // 时间区间有重叠
                        if (!(newTask.EndTime.Value <= block.StartTime || newTask.StartTime.Value >= block.EndTime))
                        {
                            conflicts.Add(block);
                        }
                    }
                }
            }
            return (conflicts.Count > 0, conflicts);
        }



        //导航栏周
        private void NavigateWeek(int offset)
        {
            CurrentDate = CurrentDate.AddDays(offset);
            LoadTasksForCurrentWeek();
        }
        
        //导航栏月
        private void NavigateMonth(int offset)
        {
            CurrentDate = CurrentDate.AddMonths(offset);
            LoadTasksForCurrentWeek();
        }

        // 获取当前日期所在的月份
        private void UpdateMonthText()
        {
            
            var month = _currentDate.Month;
            MonthText = $"{month}月份";
            OnPropertyChanged(nameof(MonthText));
        }

        // 计算月份中的周数
        private void UpdateWeekText()
        {
            // 获取当前月的第一天
            var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);

            // 获取当前日期所在周的周一
            var currentMonday = CurrentDate.Date;
            while (currentMonday.DayOfWeek != DayOfWeek.Monday)
            {
                currentMonday = currentMonday.AddDays(-1);
            }

            // 获取本月第一个周一
            var firstMonday = firstDayOfMonth;
            // 如果月初不是周一，向后找到第一个周一
            while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            {
                firstMonday = firstMonday.AddDays(1);
            }

            // 如果第一个周一比月初晚，且月初不是周一，那么第一周应该从上个月的某个周一开始
            // 所以需要判断第一天属于哪一周
            if (firstMonday > firstDayOfMonth && firstDayOfMonth.DayOfWeek != DayOfWeek.Monday)
            {
                firstMonday = firstMonday.AddDays(-7);
            }

            // 计算当前日期所在周是本月的第几周
            int weekOfMonth = ((currentMonday - firstMonday).Days / 7) + 1;

            // 如果当前周的周一在本月第一天之前，说明这是跨月周，月份数需要调整
            if (currentMonday < firstDayOfMonth)
            {
                weekOfMonth = 1;
            }

            WeekText = $"第{weekOfMonth}周";
            OnPropertyChanged(nameof(WeekText));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // 存储每天的展开状态
        public bool[] ExpandedColumns { get; set; } = new bool[7];

        // 获取特定列的全天任务数量
        public int GetAllDayTaskCountForColumn(int column)
        {
            return TaskBlocks.Count(t => t.IsAllDay && t.Column == column);
        }

        // 切换展开状态的命令
        public ICommand ToggleColumnExpandCommand { get; }

        // 切换某一列的展开状态
        // 切换某一列的展开状态
        private void ToggleColumnExpand(int column)
        {
            if (column >= 0 && column < 7)
            {
                ExpandedColumns[column] = !ExpandedColumns[column];

                // 更新对应的 DateColumnViewModel
                DateColumns[column].IsExpanded = ExpandedColumns[column];

                // 通知相关属性更新
                OnPropertyChanged(nameof(ExpandedColumns));
                DateColumns[column].RefreshAllDayTasksView();
            }
        }


    }
}