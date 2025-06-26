using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Services;
using System.Threading.Tasks;
using System.Diagnostics;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Specialized;

namespace TimeController.ViewModels
{
    public class ReviewViewModel_everyweek : INotifyPropertyChanged
    {
        private readonly ITaskService _taskService; // 注入任务服务，用于获取任务数据
        private DateTime _selectedWeekStart; // 当前所选的周起始日（周一）

        // 存储本周完成和未完成的任务
        private ObservableCollection<TaskModel> _weeklyCompletedTasks;
        private ObservableCollection<TaskModel> _weeklyUncompletedTasks;

        // 本周推迟或放弃的任务
        private ObservableCollection<TaskModel> _skippedTasks = new();

        // 用户选择的放弃/推迟原因列表
        private ObservableCollection<string> _reviewReasons;

        public bool IsEverydayPage { get; set; } = false;
        public event Action? NavigateToEverydayRequested;

        public bool HasSkippedTasks => SkippedTasks != null && SkippedTasks.Count > 0;

        // 系统生成的复盘建议卡片
        public ObservableCollection<ReviewCardModel> WeeklyReviewCards { get; set; } = new();

        // 折线图相关字段
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
                    LoadTasksForWeek(value); // 周切换后加载对应任务
                }
            }
        }

        public ObservableCollection<TaskModel> SkippedTasks
        {
            get => _skippedTasks;
            set
            {
                if (_skippedTasks != value)
                {
                    _skippedTasks.CollectionChanged -= SkippedTasks_CollectionChanged;
                    _skippedTasks = value;
                    _skippedTasks.CollectionChanged += SkippedTasks_CollectionChanged;
                    OnPropertyChanged(nameof(SkippedTasks));
                    OnPropertyChanged(nameof(HasSkippedTasks));
                }
            }
        }

        public ObservableCollection<TaskModel> WeeklyCompletedTasks
        {
            get => _weeklyCompletedTasks;
            set { _weeklyCompletedTasks = value; OnPropertyChanged(nameof(WeeklyCompletedTasks)); }
        }

        public ObservableCollection<TaskModel> WeeklyUncompletedTasks
        {
            get => _weeklyUncompletedTasks;
            set { _weeklyUncompletedTasks = value; OnPropertyChanged(nameof(WeeklyUncompletedTasks)); }
        }

        public ObservableCollection<string> ReviewReasons
        {
            get => _reviewReasons;
            set { _reviewReasons = value; OnPropertyChanged(nameof(ReviewReasons)); }
        }

        // 命令绑定（导航 + 周切换）
        public ICommand NavigateToEverydayCommand { get; }
        public ICommand NavigateToEveryweekCommand { get; }
        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }

        public ReviewViewModel_everyweek(ITaskService taskService)
        {
            _taskService = taskService;

            NavigateToEverydayCommand = new RelayCommand(_ => NavigateToEverydayRequested?.Invoke());
            NavigateToEveryweekCommand = new RelayCommand(_ => { });
            PreviousWeekCommand = new RelayCommand(_ => MoveToPreviousWeek());
            NextWeekCommand = new RelayCommand(_ => MoveToNextWeek());

            WeeklyCompletedTasks = new ObservableCollection<TaskModel>();
            WeeklyUncompletedTasks = new ObservableCollection<TaskModel>();
            SkippedTasks = new ObservableCollection<TaskModel>();

            ReviewReasons = new ObservableCollection<string>
            {
                "时间安排问题",
                "主观状态问题",
                "外部干扰",
                "自主延迟决策",
                "动机缺失",
                "不明确"
            };

            // 默认设置为本周一
            SelectedWeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            LoadTasksForWeek(SelectedWeekStart);
        }

        private async void LoadTasksForWeek(DateTime weekStart)
        {
            WeeklyCompletedTasks.Clear();
            WeeklyUncompletedTasks.Clear();

            var weekEnd = weekStart.AddDays(7);

            // 获取该周所有非课程任务
            var scheduledTasks = (await _taskService.GetTasksForDateRange(weekStart, weekEnd))
                .Where(t => t.Mode == TaskMode.Strong && !t.IsCourseTask).ToList();

            // 分类填充“完成”与“未完成”任务
            foreach (var t in scheduledTasks)
            {
                if (t.Status == MyTaskStatus.Completed)
                    WeeklyCompletedTasks.Add(t);
                else if (t.Status == MyTaskStatus.Pending)
                    WeeklyUncompletedTasks.Add(t);
            }

            // 筛选出本周被推迟/放弃的任务
            var allTasks = await _taskService.GetAllTasksAsync();
            var strongHistory = allTasks.Where(t => t.Mode == TaskMode.Strong && !t.IsCourseTask);

            var postponedThisWeek = strongHistory
                .Where(t => t.PostponedAt >= weekStart && t.PostponedAt < weekEnd);

            var abandonedThisWeek = strongHistory
                .Where(t => t.AbandonedAt >= weekStart && t.AbandonedAt < weekEnd);

            // 合并并去重（以任务名称分组）
            var skippedThisWeek = postponedThisWeek
                .Concat(abandonedThisWeek)
                .OrderBy(t => t.PostponedAt ?? t.AbandonedAt)
                .GroupBy(t => t.Name)
                .Select(g => g.First())
                .ToList();

            SkippedTasks = new ObservableCollection<TaskModel>(skippedThisWeek);

            // 生成系统建议卡片
            var generator = new ReviewCardGenerator();
            WeeklyReviewCards = new ObservableCollection<ReviewCardModel>(
                generator.GenerateCards(scheduledTasks, strongHistory.ToList(), weekStart, weekEnd)
            );

            // 初始化折线图数据
            LoadChart(scheduledTasks);

            // 通知属性更新
            OnPropertyChanged(nameof(WeeklyCompletedTasks));
            OnPropertyChanged(nameof(WeeklyUncompletedTasks));
            OnPropertyChanged(nameof(SkippedTasks));
            OnPropertyChanged(nameof(WeeklyReviewCards));
        }

        public string CurrentWeekRangeText =>
            $"📅 {SelectedWeekStart:yyyy-MM-dd} ~ {SelectedWeekStart.AddDays(6):yyyy-MM-dd}";

        // 获取当前是本月第几周
        public string CurrentWeekText
        {
            get
            {
                int weekNumber = GetWeekOfMonth(SelectedWeekStart);
                return $"{SelectedWeekStart.Year} 年 {SelectedWeekStart.Month} 月 第 {weekNumber} 周";
            }
        }

        private int GetWeekOfMonth(DateTime date)
        {
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            int firstWeekDay = (int)firstDayOfMonth.DayOfWeek;
            if (firstWeekDay == 0) firstWeekDay = 7;
            int day = date.Day + firstWeekDay - 1;
            return (int)Math.Ceiling(day / 7.0);
        }

        private void MoveToPreviousWeek() => SelectedWeekStart = SelectedWeekStart.AddDays(-7);
        private void MoveToNextWeek() => SelectedWeekStart = SelectedWeekStart.AddDays(7);
        private void LoadChart(List<TaskModel> weeklyTasks)
        {
            var days = Enumerable.Range(0, 7).Select(i => SelectedWeekStart.AddDays(i)).ToArray();
            var completedCounts = new List<int>();
            var skippedCounts = new List<int>();

            foreach (var day in days)
            {
                // 完成任务计数
                completedCounts.Add(weeklyTasks.Count(t =>
                    t.PlannedDate.Date == day.Date &&
                    t.Status == MyTaskStatus.Completed));

                // 跳过任务计数（按推迟或放弃时间）
                int skip = weeklyTasks.Count(t =>
                    (t.PostponedAt?.Date == day.Date) ||
                    (t.AbandonedAt?.Date == day.Date));

                skippedCounts.Add(skip);
            }

            int maxValue = Math.Max(
                completedCounts.DefaultIfEmpty(0).Max(),
                skippedCounts.DefaultIfEmpty(0).Max()
            );

            int dynamicMax = Math.Max(5, maxValue + 1);

            Series = new ISeries[]
            {
                new LineSeries<int> { Values = completedCounts, Name = "已完成" },
                new LineSeries<int> { Values = skippedCounts, Name = "推迟/放弃" }
            };

            XAxes = new[] { new Axis { Labels = days.Select(d => d.ToString("MM/dd")).ToArray() } };
            YAxes = new[] { new Axis { MinLimit = 0, MaxLimit = dynamicMax } };

            TooltipTextPaint = new SolidColorPaint(SKColors.Black)
            {
                SKTypeface = SKTypeface.FromFamilyName("微软雅黑")
            };

            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
            OnPropertyChanged(nameof(TooltipTextPaint));
        }

        private void SkippedTasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasSkippedTasks));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}
