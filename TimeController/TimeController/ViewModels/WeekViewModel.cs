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

namespace TimeController.ViewModels
{
    public class WeekViewModel : INotifyPropertyChanged
    {
        private DateTime _currentDate;
        private readonly ITaskService _taskService;


        public ObservableCollection<TaskModel> Tasks { get; set; } = new ObservableCollection<TaskModel>();
        public ObservableCollection<TaskBlock> TaskBlocks { get; } = new ObservableCollection<TaskBlock>();

        public event Action<TaskModel>? SaveRequested;

        public ICommand RemoveTaskBlockCommand { get; }

        public WeekViewModel(ITaskService taskService)
        {
            _taskService = taskService;
            // ... 其他初始化代码
        }


        private void OnTaskSaved(TaskModel task)
        {
            // 只需调用 AddTask，所有定位和添加逻辑都在 AddTask 里完成
            AddTask(task);
        }

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


        // 添加任务到视图（不持久化到数据库）
        private void AddTaskToView(TaskModel task)
        {
            // 计算本周一
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            int column = (task.PlannedDate - monday).Days;
            if (column < 0 || column > 6) return; // 不在当前周的任务不显示

            int row = task.IsAllDay ? 0 : (task.StartTime.HasValue ? task.StartTime.Value.Hours + 1 : 1) - 1;
            int rowSpan = 1;
            if (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
                rowSpan = Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours) + 1;

            var taskBlock = new TaskBlock
            {
                Name = task.Name,
                Note = task.Note,
                Type = task.Type,
                StartTime = task.StartTime ?? TimeSpan.Zero,
                EndTime = task.EndTime ?? TimeSpan.Zero,
                Brush = GetBrushForTaskType(task.Type),
                Column = column,
                Row = row,
                RowSpan = rowSpan,
                Id=task.Id
            };

            TaskBlocks.Add(taskBlock);
        }



        // 修改 AddTask 方法，增加持久化后的加载
        public async void AddTask(TaskModel task)
        {
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
            _currentDate = DateTime.Today;
            UpdateMonthText();
            UpdateWeekText();

            PreviousWeekCommand = new RelayCommand(_ => NavigateWeek(-7));
            NextWeekCommand = new RelayCommand(_ => NavigateWeek(7));
            PreviousMonthCommand = new RelayCommand(_ => NavigateMonth(-1));
            NextMonthCommand = new RelayCommand(_ => NavigateMonth(1));

            SaveRequested += OnTaskSaved;
            LoadTasksForCurrentWeek();
            RemoveTaskBlockCommand = new RelayCommand<TaskBlock>(RemoveTaskBlock);
        }

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
        }


        // 在 WeekViewModel.cs 中添加这个方法
        public (bool hasConflict, List<TaskBlock> conflicts) CheckTimeConflicts(TaskModel newTask)
        {
            var conflicts = new List<TaskBlock>();

            // 如果是全天任务，则检查同一天的全天任务
            if (newTask.IsAllDay)
            {
                foreach (var block in TaskBlocks)
                {
                    // 同一天的全天任务
                    DateTime monday = CurrentDate.Date;
                    while (monday.DayOfWeek != DayOfWeek.Monday)
                        monday = monday.AddDays(-1);

                    DateTime blockDate = monday.AddDays(block.Column);

                    if (blockDate.Date == newTask.PlannedDate.Date && block.Row == 0)
                    {
                        conflicts.Add(block);
                    }
                }
            }
            else if (newTask.StartTime.HasValue && newTask.EndTime.HasValue)
            {
                // 计算当前任务的位置
                DateTime monday = CurrentDate.Date;
                while (monday.DayOfWeek != DayOfWeek.Monday)
                    monday = monday.AddDays(-1);

                int column = (newTask.PlannedDate - monday).Days;

                // 检查同一天的非全天任务是否有冲突
                foreach (var block in TaskBlocks)
                {
                    if (block.Column == column && block.Row > 0) // 非全天任务
                    {
                        // 检查时间是否重叠
                        TimeSpan blockStart = block.StartTime;
                        TimeSpan blockEnd = block.EndTime;

                        if (!(newTask.EndTime.Value <= blockStart || newTask.StartTime.Value >= blockEnd))
                        {
                            conflicts.Add(block);
                        }
                    }
                }
            }

            return (conflicts.Count > 0, conflicts);
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