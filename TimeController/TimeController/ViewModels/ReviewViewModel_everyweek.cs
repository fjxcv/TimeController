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
using System.Collections.Specialized;
using System.Windows.Documents;

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

        // 当 SkippedTasks 集合变化时触发
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
                if (_skippedTasks != value)
                {
                    // 取消旧集合订阅
                    _skippedTasks.CollectionChanged -= SkippedTasks_CollectionChanged;

                    _skippedTasks = value;

                    // 订阅新集合的变化
                    _skippedTasks.CollectionChanged += SkippedTasks_CollectionChanged;

                    OnPropertyChanged(nameof(SkippedTasks));
                    OnPropertyChanged(nameof(HasSkippedTasks));
                }
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

            var weekEnd = weekStart.AddDays(7); // 半开区间 [weekStart, weekEnd)
            var tasksThisWeek = await _taskService.GetTasksForDateRange(weekStart, weekEnd);
            var allHistory = await _taskService.GetAllTasksAsync();

            // 1. 拉出本周所在日期范围内的所有“按计划”任务
            var allThisWeek = await _taskService.GetTasksForDateRange(weekStart, weekStart.AddDays(6));
            var scheduledTasks = allThisWeek
                .Where(t => t.Mode == TaskMode.Strong)
                .ToList();

            // 2. 按状态分类：本周“完成” & “未完成”
            foreach (var t in scheduledTasks)
            {
                if (t.Status == MyTaskStatus.Completed)
                    WeeklyCompletedTasks.Add(t);
                else if (t.Status == MyTaskStatus.Pending)
                    WeeklyUncompletedTasks.Add(t);
            }

            // 3. 拉出所有任务，找出本周发生过的“推迟/放弃”事件
            var allTasks = await _taskService.GetAllTasksAsync();
            var strongHistory = allTasks.Where(t => t.Mode == TaskMode.Strong);

            var postponedThisWeek = strongHistory
                .Where(t => t.PostponedAt >= weekStart && t.PostponedAt < weekEnd);

            var abandonedThisWeek = strongHistory
                .Where(t => t.AbandonedAt >= weekStart && t.AbandonedAt < weekEnd);


            // 合并并按时间排序
            var skippedThisWeek = postponedThisWeek
                .Concat(abandonedThisWeek)
                .OrderBy(t => t.PostponedAt ?? t.AbandonedAt)// 按 Name 分组，取每组第一条，去重效果
                .GroupBy(t => t.Name)
                .Select(g => g.First())
                .ToList();
            
            SkippedTasks = new ObservableCollection<TaskModel>(skippedThisWeek);


            // 生成卡片、折线图
            var generator = new ReviewCardGenerator();
            WeeklyReviewCards = new ObservableCollection<ReviewCardModel>(
                generator.GenerateCards(scheduledTasks, strongHistory.ToList(), weekStart, weekEnd)
            );

            // 生成完成率卡片
            LoadChart(scheduledTasks);

            OnPropertyChanged(nameof(WeeklyCompletedTasks));
            OnPropertyChanged(nameof(WeeklyUncompletedTasks));
            OnPropertyChanged(nameof(SkippedTasks));
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

        private void OnTaskSaved(TaskModel task)
        {
            var weekStart = SelectedWeekStart;
            var weekEnd = weekStart.AddDays(7);
            if (task.PlannedDate >= weekStart && task.PlannedDate < weekEnd)
            {
                LoadTasksForWeek(weekStart);
            }
        }

        //折线图赋值
        private void LoadChart(List<TaskModel> weeklyTasks)
        {
            var days = Enumerable.Range(0, 7)
                .Select(i => SelectedWeekStart.AddDays(i))
                .ToArray();

            var completedCounts = new List<int>();
            var skippedCounts = new List<int>();


            foreach (var day in days)
            {
                // 完成数：按Status==Completed&PlannedDate
                completedCounts.Add(weeklyTasks.Count(t =>
                    t.PlannedDate.Date == day.Date
                 && t.Status == MyTaskStatus.Completed));

                // 跳过数：看事件时间戳
                int skip = weeklyTasks.Count(t =>
                       (t.PostponedAt.HasValue && t.PostponedAt.Value.Date == day.Date)
                    || (t.AbandonedAt.HasValue && t.AbandonedAt.Value.Date == day.Date));
                skippedCounts.Add(skip);
            }

            int safeMax(List<int> list) => list.Any() ? list.Max() : 0;//预防无任务记录的情况
            // 比较已完成和跳过两条线
            int maxValue = Math.Max(
                safeMax(completedCounts),
                safeMax(skippedCounts)
                );

            int dynamicMax = Math.Max(5, maxValue + 1); // 任务上限至少是5，再+1防止顶格

            Series = new ISeries[]
            {
            new LineSeries<int> { Values = completedCounts, Name = "已完成", /* … */ },
            new LineSeries<int> { Values = skippedCounts, Name = "推迟/放弃", /* … */ },
            };

            //X轴显示日期
            XAxes = new[] { new Axis { Labels = days.Select(d => d.ToString("MM/dd")).ToArray() } };

            //Y轴显示数量
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

        //触发HasSkippedTasks通知
        private void SkippedTasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasSkippedTasks));
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