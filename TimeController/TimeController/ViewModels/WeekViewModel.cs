using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using TimeController.Models;
using Microsoft.Extensions.DependencyInjection;
using TimeController.Services;
using TimeController.Helpers;
using System.Diagnostics;
using System.Windows.Data;


namespace TimeController.ViewModels
{
    public class WeekViewModel : INotifyPropertyChanged
    {
        private DateTime _currentDate;
        private readonly ITaskService _taskService;
        public IEnumerable<TaskBlock> AllDayTaskBlocks => TaskBlocks.Where(t => t.IsAllDay);
        public IEnumerable<TaskBlock> TimedTaskBlocks => TaskBlocks.Where(t => !t.IsAllDay);

        public ICommand ReviewCommand { get; }

        public ObservableCollection<TaskModel> Tasks { get; set; } = new ObservableCollection<TaskModel>();
        public ObservableCollection<TaskBlock> TaskBlocks { get; } = new ObservableCollection<TaskBlock>();

        public event Action<TaskModel>? SaveRequested;

        public ICommand RemoveTaskBlockCommand { get; }
        public ICollectionView TimedTaskBlocksView { get; }

        public WeekViewModel(ITaskService taskService)
        {
            _taskService = taskService;
            _taskService.TaskSaved += OnExternalTaskSaved;
            ReviewCommand = new RelayCommand(_ => ShowReview());
            PreviousWeekCommand = new RelayCommand(_ => NavigateWeek(-7));
            NextWeekCommand = new RelayCommand(_ => NavigateWeek(7));
            PreviousMonthCommand = new RelayCommand(_ => NavigateMonth(-1));
            NextMonthCommand = new RelayCommand(_ => NavigateMonth(1));
            TimedTaskBlocksView = CollectionViewSource.GetDefaultView(TaskBlocks);
            TimedTaskBlocksView.Filter = obj => obj is TaskBlock block && !block.IsAllDay;
            TaskBlocks.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(AllDayTaskBlocks));
                OnPropertyChanged(nameof(TimedTaskBlocks));
                TimedTaskBlocksView.Refresh();
            };
            SaveRequested += OnTaskSaved;
            RemoveTaskBlockCommand = new RelayCommand<TaskBlock>(RemoveTaskBlock);
            _currentDate = DateTime.Today;
            UpdateMonthText();
            UpdateWeekText();
            LoadTasksForCurrentWeek();
        }


        private void OnTaskSaved(TaskModel task)
        {
            // 只需调用 AddTask，所有定位和添加逻辑都在 AddTask 里完成
            AddTask(task);
        }

        //加载本周任务
        public async void LoadTasksForCurrentWeek()
        {
            // 清空当前任务块集合
            TaskBlocks.Clear();

            // 计算当前周的起止日期
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            DateTime sunday = monday.AddDays(6);

            Debug.WriteLine($"加载 {monday:yyyy-MM-dd} 到 {sunday:yyyy-MM-dd} 的任务");

            // 从数据库加载当前周的任务
            List<TaskModel> weekTasks;
            if (_taskService != null)
            {
                weekTasks = await _taskService.GetTasksForDateRange(monday, sunday);
            }
            else
            {
                // 如果没有服务，就从当前缓存的任务中筛选
                weekTasks = Tasks.Where(t => t.PlannedDate >= monday && t.PlannedDate <= sunday).ToList();
            }

            // 将任务转换为 TaskBlock 并添加到集合
            foreach (var task in weekTasks)
            {
                AddTaskToView(task);
            }

            // 通知UI更新
            OnPropertyChanged(nameof(TaskBlocks));
        }


        // 添加任务到视图
        private void AddTaskToView(TaskModel task)
        {
            // 计算本周一
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            int column = (task.PlannedDate - monday).Days;
            if (column < 0 || column > 6) return; // 不在当前周的任务不显示

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
                          ? Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours)+1 : 1,
                Id = task.Id
            };

            TaskBlocks.Add(taskBlock);
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
            }
            OnPropertyChanged(nameof(TimedTaskBlocks));
        }

        // 处理冲突的方法：删除冲突任务并添加新任务
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
        }

        private void OnExternalTaskSaved(TaskModel task)
        {
            // 忽略非强管理任务
            if (task.Mode != TaskMode.Strong)
                return;

            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);
            DateTime sunday = monday.AddDays(6);

            if (task.PlannedDate < monday || task.PlannedDate > sunday)
                return;

            if (Tasks.Any(t => t.Id == task.Id))
                return;

            Tasks.Add(task);
            AddTaskToView(task);
            OnPropertyChanged(nameof(TimedTaskBlocks));
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

        public class TaskBlock
        {
            public string Name { get; set; }
            public string Note { get; set; }
            public TaskType Type { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public Brush Brush { get; set; }
            public bool IsAllDay { get; set; }

            // 定位属性
            public int Column { get; set; } // 星期几（0=周一，1=周二...）
            public int Row { get; set; }    // 对应小时行（0=全天，1=0:00, 2=1:00...）
            public int RowSpan { get; set; } // 跨多少小时

            public int Id { get; set; }//Id
        }


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
                }
            }
        }

        public string MonthText { get; private set; }
        public string WeekText { get; private set; }

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }

        public WeekViewModel()
        {
            _taskService = App.AppHost.Services.GetRequiredService<ITaskService>();
            _taskService.TaskSaved += OnExternalTaskSaved;

            _currentDate = DateTime.Today;
            UpdateMonthText();
            UpdateWeekText();

            ReviewCommand = new RelayCommand(_ => ShowReview());
            PreviousWeekCommand = new RelayCommand(_ => NavigateWeek(-7));
            NextWeekCommand = new RelayCommand(_ => NavigateWeek(7));
            PreviousMonthCommand = new RelayCommand(_ => NavigateMonth(-1));
            NextMonthCommand = new RelayCommand(_ => NavigateMonth(1));
            TimedTaskBlocksView = CollectionViewSource.GetDefaultView(TaskBlocks);
            TimedTaskBlocksView.Filter = obj => obj is TaskBlock block && !block.IsAllDay;

            TaskBlocks.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(AllDayTaskBlocks));
                OnPropertyChanged(nameof(TimedTaskBlocks));
                TimedTaskBlocksView.Refresh();
            };

            SaveRequested += OnTaskSaved;
            LoadTasksForCurrentWeek();
            RemoveTaskBlockCommand = new RelayCommand<TaskBlock>(RemoveTaskBlock);
        }


        // 移除任务块
        private async void RemoveTaskBlock(TaskBlock block)
        {
            if (block == null) return;

            // 从UI集合中移除
            TaskBlocks.Remove(block);

            // 找到对应的 TaskModel 并从数据库删除
            var task = Tasks.FirstOrDefault(t => t.Id == block.Id);
            if (task != null)
            {
                Tasks.Remove(task);
                if (_taskService != null)
                    await _taskService.DeleteTaskAsync(task);
            }
            OnPropertyChanged(nameof(TimedTaskBlocks));
            TimedTaskBlocksView.Refresh(); // 强制刷新视图
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


        private void ShowReview()
        {
            var nav = App.AppHost.Services.GetRequiredService<INavigationService>();
            nav.NavigateTo(AppFrame.Instance!, "Everyday");
        }

        private void NavigateWeek(int offset)
        {
            CurrentDate = CurrentDate.AddDays(offset);
            LoadTasksForCurrentWeek();
        }

        private void NavigateMonth(int offset)
        {
            CurrentDate = CurrentDate.AddMonths(offset);
            LoadTasksForCurrentWeek();
        }

        private void UpdateMonthText()
        {
            // 获取当前日期所在的月份
            var month = _currentDate.Month;
            MonthText = $"{month}月份";
            OnPropertyChanged(nameof(MonthText));
        }

        private void UpdateWeekText()
        {
            // 计算月份中的周数
            var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            var dayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            var dayOfMonth = _currentDate.Day;
            var weekOfMonth = (dayOfMonth + dayOfWeek) / 7 + 1;
            WeekText = $"第{weekOfMonth}周";
            OnPropertyChanged(nameof(WeekText));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}