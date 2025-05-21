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

namespace TimeController.ViewModels
{
    public class WeekViewModel : INotifyPropertyChanged
    {
        private DateTime _currentDate;
        private readonly ITaskService _taskService;


        public ObservableCollection<TaskModel> Tasks { get; set; } = new ObservableCollection<TaskModel>();
        public ObservableCollection<TaskBlock> TaskBlocks { get; } = new ObservableCollection<TaskBlock>();

        public event Action<TaskModel>? SaveRequested;

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

        public async void AddTask(TaskModel task)
        {
            Tasks.Add(task);

            // 数据库持久化
            if (_taskService != null)
            {
                await _taskService.UpdateTaskAsync(task); // 或 AddTaskAsync(task)
            }

            // 计算本周一
            DateTime monday = _currentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            // 计算任务属于哪一天（0=周一，1=周二...）
            int column = (task.PlannedDate - monday).Days;
            if (column < 0 || column > 6) column = 0; // 防止越界

            // 全天任务在第0行，否则按小时+1（第1行是0:00，第2行是1:00...）
            int row = task.IsAllDay ? 0 : (task.StartTime.HasValue ? task.StartTime.Value.Hours + 1 : 1);
            // 调试用
            System.Diagnostics.Debug.WriteLine($"StartTime: {task.StartTime}, Row: {row}");

            // RowSpan 跨多少小时
            int rowSpan = 1;
            if (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
            {
                rowSpan = Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours);
            }

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
                RowSpan = rowSpan
            };
            TaskBlocks.Add(taskBlock);

            System.Diagnostics.Debug.WriteLine($"Task: {task.Name}, Column: {column}, Row: {row}, RowSpan: {rowSpan}");


            System.Diagnostics.Debug.WriteLine($"task.StartTime: {task.StartTime}");
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
        }



        private void NavigateWeek(int offset)
        {
            CurrentDate = CurrentDate.AddDays(offset);
        }

        private void NavigateMonth(int offset)
        {
            CurrentDate = CurrentDate.AddMonths(offset);
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