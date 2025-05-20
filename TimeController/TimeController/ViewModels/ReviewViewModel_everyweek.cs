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
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

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
        public ObservableCollection<ReviewCardModel> WeeklyReviewCards { get; set; } = new();

        //折线图相关
        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }
        public SolidColorPaint TooltipTextPaint { get; set; }


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

            //卡片分析逻辑
            var allTasks = await _taskService.GetAllTasksAsync(); // 加一个历史任务获取接口
            var generator = new ReviewCardGenerator();

            WeeklyReviewCards = new ObservableCollection<ReviewCardModel>(
                generator.GenerateCards(tasks, allTasks)
            );

            SkippedTasks = new ObservableCollection<TaskModel>(skipped);
            LoadChart(tasks);
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

        //折线图赋值
        private void LoadChart(List<TaskModel> weeklyTasks)
        {
            var days = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.StartOfWeek(DayOfWeek.Monday).AddDays(offset))
                .ToArray();

            var completedCounts = new List<int>();
            var pendingCounts = new List<int>();
            var postponedCounts = new List<int>();


            foreach (var day in days)
            {
                completedCounts.Add(weeklyTasks.Count(t =>
                    t.PlannedDate.Date == day.Date &&
                    t.Status == MyTaskStatus.Completed));

                postponedCounts.Add(weeklyTasks.Count(t =>
                    t.PlannedDate.Date == day.Date &&
                    (t.Status == MyTaskStatus.Postponed || t.Status == MyTaskStatus.Abandoned)));
            }

            int safeMax(List<int> list) => list.Any() ? list.Max() : 0;//预防无任务记录的情况
            int maxValue = Math.Max(
                Math.Max(safeMax(completedCounts), safeMax(pendingCounts)),
                safeMax(postponedCounts));

            int dynamicMax = Math.Max(5, maxValue + 1); // 任务上限至少是5，再+1防止顶格

            Series = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = completedCounts,
                    Name = "已完成",
                    Stroke = new SolidColorPaint(SKColors.Green, 2),
                    Fill = null
                },

                new LineSeries<int>
                {
                    Values = postponedCounts,
                    Name = "推迟/放弃",
                    Stroke = new SolidColorPaint(SKColors.Red, 2),
                    Fill = null
                }
                    };

            XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = days.Select(d => d.ToString("MM/dd")).ToArray(),
                        LabelsRotation = 0
                    }
                };

            YAxes = new Axis[]
                {
                    new Axis
                    {
                        MinLimit = 0,
                        MaxLimit = dynamicMax
                    }
                };

            //设置个字体防止框框
            TooltipTextPaint = new SolidColorPaint(SKColors.Black)
            {
                SKTypeface = SKTypeface.FromFamilyName("微软雅黑")
            };


            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
            OnPropertyChanged(nameof(TooltipTextPaint));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // 扩展方法：获取本周的开始日期
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }

}