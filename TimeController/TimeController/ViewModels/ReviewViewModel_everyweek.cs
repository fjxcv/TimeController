using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Services;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Diagnostics;

namespace TimeController.ViewModels
{
    public class ReviewViewModel_everyweek : INotifyPropertyChanged
    {
        private readonly ITaskService _taskService;
        private DateTime _selectedWeekStart;
        private ObservableCollection<TaskModel> _weeklyCompletedTasks;
        private ObservableCollection<TaskModel> _weeklyUncompletedTasks;
        private ObservableCollection<string> _reviewReasons;
        public bool IsEverydayPage { get; set; } = false;
        public event Action? NavigateToEverydayRequested;
        public bool HasSkippedTasks => SkippedTasks != null && SkippedTasks.Count > 0;


        public DateTime SelectedWeekStart
        {
            get => _selectedWeekStart;
            set
            {
                if (_selectedWeekStart != value)
                {
                    _selectedWeekStart = value;
                    OnPropertyChanged(nameof(SelectedWeekStart));
                    OnPropertyChanged(nameof(CurrentWeekText));
                    OnPropertyChanged(nameof(CurrentWeekRangeText));

                    LoadTasksForWeek(value);
                }
            }
        }

        //推迟or放弃的任务
        private ObservableCollection<TaskModel> _skippedTasks = new();
        public ObservableCollection<TaskModel> SkippedTasks
        {
            get => _skippedTasks;
            set
            {
                _skippedTasks = value;
                OnPropertyChanged(nameof(SkippedTasks));
            }
        }

        public ObservableCollection<TaskModel> WeeklyCompletedTasks
        {
            get => _weeklyCompletedTasks;
            set
            {
                _weeklyCompletedTasks = value;
                OnPropertyChanged(nameof(WeeklyCompletedTasks));
            }
        }

        public ObservableCollection<TaskModel> WeeklyUncompletedTasks
        {
            get => _weeklyUncompletedTasks;
            set
            {
                _weeklyUncompletedTasks = value;
                OnPropertyChanged(nameof(WeeklyUncompletedTasks));
            }
        }

        public ObservableCollection<string> ReviewReasons
        {
            get => _reviewReasons;
            set
            {
                if (_reviewReasons != value)
                {
                    _reviewReasons = value;
                    OnPropertyChanged(nameof(ReviewReasons));
                }
            }
        }




        public ICommand NavigateToEverydayCommand { get; }
        public ICommand NavigateToEveryweekCommand { get; }

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }

        public ReviewViewModel_everyweek(ITaskService taskService)
        {
            _taskService = taskService;
            IsEverydayPage = false;

            NavigateToEverydayCommand = new RelayCommand(_ => NavigateToEverydayRequested?.Invoke());
            NavigateToEveryweekCommand = new RelayCommand(_ => { }); // 当前页

            PreviousWeekCommand = new RelayCommand(_ => MoveToPreviousWeek());
            NextWeekCommand = new RelayCommand(_ => MoveToNextWeek());

            //初始化集合
            WeeklyCompletedTasks = new ObservableCollection<TaskModel>();
            WeeklyUncompletedTasks = new ObservableCollection<TaskModel>();
            SkippedTasks = new ObservableCollection<TaskModel>();

            // 初始化复盘原因列表
            ReviewReasons = new ObservableCollection<string>
            {
                "时间安排问题",
                "主观状态问题",
                "外部干扰",
                "自主延迟决策",
                "动机缺失",
                "不明确"
            };

            // 初始化周开始日期为本周一,这个要放初始化后面
            SelectedWeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            LoadTasksForWeek(SelectedWeekStart);
        }

        private async void LoadTasksForWeek(DateTime weekStart)
        {

            //清空任务
            WeeklyCompletedTasks.Clear();
            WeeklyUncompletedTasks.Clear();
            SkippedTasks.Clear();

            var weekEnd = weekStart.AddDays(6);

            var tasks = await _taskService.GetTasksForDateRange(weekStart, weekStart.AddDays(6));


            foreach (var task in tasks)
            {
                switch (task.Status)
                {
                    case MyTaskStatus.Completed:
                        WeeklyCompletedTasks.Add(task);
                        break;
                    case MyTaskStatus.Pending:
                        WeeklyUncompletedTasks.Add(task);
                        break;
                    case MyTaskStatus.Postponed:
                    case MyTaskStatus.Abandoned:
                        SkippedTasks.Add(task);
                        break;
                }
            }

            var skipped = tasks.Where(t =>
            t.Status == MyTaskStatus.Postponed ||
            t.Status == MyTaskStatus.Abandoned).ToList();

            // ✅ 卡片分析逻辑（加在这儿）
            var allTasks = await _taskService.GetAllTasksAsync(); // 加一个历史任务获取接口
            var generator = new ReviewCardGenerator();

            WeeklyReviewCards = new ObservableCollection<ReviewCardModel>(
                generator.GenerateCards(tasks, allTasks)
            );

            SkippedTasks = new ObservableCollection<TaskModel>(skipped);
            OnPropertyChanged(nameof(SkippedTasks));
            OnPropertyChanged(nameof(HasSkippedTasks));
            OnPropertyChanged(nameof(WeeklyReviewCards));

        }

        //显示本周日期范围
        public string CurrentWeekRangeText
        {
            get
            {
                var end = SelectedWeekStart.AddDays(6);
                return $"📅 {SelectedWeekStart:yyyy-MM-dd} ~ {end:yyyy-MM-dd}";
            }
        }


        //算出当前日期是本月的第几周
        private int GetWeekOfMonth(DateTime date)
        {
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            var firstWeekDay = (int)firstDayOfMonth.DayOfWeek;
            if (firstWeekDay == 0) firstWeekDay = 7;

            int day = date.Day + firstWeekDay - 1;
            int weekNumber = (int)Math.Ceiling(day / 7.0);

            return weekNumber;
        }

        public string CurrentWeekText
        {
            get
            {
                int weekNumber = GetWeekOfMonth(SelectedWeekStart);
                return $"{SelectedWeekStart.Year} 年 {SelectedWeekStart.Month} 月 第 {weekNumber} 周";
            }
        }

        private void MoveToPreviousWeek()
        {
            SelectedWeekStart = SelectedWeekStart.AddDays(-7);
        }

        private void MoveToNextWeek()
        {
            SelectedWeekStart = SelectedWeekStart.AddDays(7);
        }



        public Task<List<TaskModel>> GetAllTasksAsync()
        {
            return _context.Tasks.OrderByDescending(t => t.PlannedDate).ToListAsync();
        }



        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}