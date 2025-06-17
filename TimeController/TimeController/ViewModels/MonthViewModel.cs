using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TimeController.Services;
using TimeController.Helpers;
using TimeController.Models;
using TimeController.Views;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using static SkiaSharp.HarfBuzz.SKShaper;

namespace TimeController.ViewModels
{
    /// <summary>
    /// 月份视图的ViewModel，负责日历显示和日期导航逻辑
    /// </summary>
    public class MonthViewModel : INotifyPropertyChanged
    {
        // 年份范围为2010-2030
        private const int MinYear = 2010;
        private const int MaxYear = 2030;

        // 日历日期集合（包含null用于填充空白）
        public ObservableCollection<DateTime?> CalendarDays { get; } = new();

        private readonly ITaskService _taskService;

        // 按日期组织的任务集合
        public Dictionary<DateTime, ObservableCollection<TaskModel>> TasksByDate { get; } = new();

        // 当前年份
        private int _year = DateTime.Today.Year;
        public int Year
        {
            get => _year;
            set
            {
                _year = value;
                OnPropertyChanged();
                UpdateCalendar(); // 年份变化时更新日历
                LoadTasksForCurrentMonth();
                OnPropertyChanged(nameof(IsCurrentMonth)); // 通知当前月份状态变化
            }
        }

        // 当前月份
        private int _month = DateTime.Today.Month;
        public int Month
        {
            get => _month;
            set
            {
                _month = value;
                OnPropertyChanged();
                UpdateCalendar(); // 月份变化时更新日历
                LoadTasksForCurrentMonth();
                OnPropertyChanged(nameof(IsCurrentMonth)); // 通知当前月份状态变化
            }
        }

        // 是否当前月份
        public bool IsCurrentMonth => Year == DateTime.Today.Year && Month == DateTime.Today.Month;

        // 年份显示文本（格式化）
        public string YearText => $"{Year}年";

        // 月份显示文本（格式化）
        public string MonthText => $"{Month}月";

        // 命令定义
        public ICommand PrevYearCommand { get; }      // 上一年
        public ICommand NextYearCommand { get; }      // 下一年
        public ICommand PrevMonthCommand { get; }     // 上一月
        public ICommand NextMonthCommand { get; }     // 下一月
        public ICommand ReviewCommand { get; }        // 进入复盘
        public ICommand DateClickCommand { get; }     // 日期点击
        public ICommand GoToCurrentMonthCommand { get; } // 回到当前月份

        private DateTime _currentMonth;
        public DateTime CurrentMonth
        {
            get => _currentMonth;
            set
            {
                if (_currentMonth != value)
                {
                    _currentMonth = value;
                    OnPropertyChanged();
                    OnCurrentMonthChanged(value);
                }
            }
        }

        public event EventHandler<DateTime>? CurrentMonthChanged;
        public event EventHandler<ObservableCollection<TaskModel>>? TodayTasksFound;

        /// <summary>
        /// 构造函数
        /// </summary>
        public MonthViewModel()
        {
            _taskService = App.AppHost.Services.GetRequiredService<ITaskService>();
            _taskService.TaskSaved += OnExternalTaskSaved;

            // 初始化所有命令
            PrevYearCommand = new RelayCommand(_ => ChangeYear(-1));
            NextYearCommand = new RelayCommand(_ => ChangeYear(1));
            PrevMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
            NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
            ReviewCommand = new RelayCommand(_ => ShowReview());
            DateClickCommand = new RelayCommand(date => ShowAddTaskDialog((DateTime)date));
            GoToCurrentMonthCommand = new RelayCommand(_ => GoToCurrentMonth());

            // 初始化日历
            UpdateCalendar();
            LoadTasksForCurrentMonth();

            _currentMonth = DateTime.Today;
        }

        /// <summary>
        /// 改变年份
        /// </summary>
        private void ChangeYear(int delta)
        {
            int newYear = Year + delta;
            // 检查年份范围
            if (newYear < MinYear || newYear > MaxYear) return;

            Year = newYear;
            OnPropertyChanged(nameof(YearText)); // 通知UI更新年份显示
        }

        /// <summary>
        /// 改变月份（处理跨年情况）
        /// </summary>
        private void ChangeMonth(int delta)
        {
            int newMonth = Month + delta;

            // 处理跨年情况
            if (newMonth < 1) // 上一年的12月
            {
                if (Year > MinYear)
                {
                    Year--;
                    newMonth = 12;
                }
                else return; // 已达最小年份
            }
            else if (newMonth > 12) // 下一年的1月
            {
                if (Year < MaxYear)
                {
                    Year++;
                    newMonth = 1;
                }
                else return; // 已达最大年份
            }

            Month = newMonth;
            OnPropertyChanged(nameof(MonthText)); // 通知UI更新月份显示
            OnPropertyChanged(nameof(YearText)); // 通知UI更新年份显示
        }

        /// <summary>
        /// 回到当前月份
        /// </summary>
        private void GoToCurrentMonth()
        {
            var today = DateTime.Today;
            Year = today.Year;
            Month = today.Month;
            OnPropertyChanged(nameof(YearText));
            OnPropertyChanged(nameof(MonthText));
            OnPropertyChanged(nameof(IsCurrentMonth));
        }

        /// <summary>
        /// 更新日历显示
        /// </summary>
        private void UpdateCalendar()
        {
            CalendarDays.Clear();
            var firstDay = new DateTime(Year, Month, 1);
            int daysInMonth = DateTime.DaysInMonth(Year, Month);

            // 第一个日期前需要的空格数
            int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;

            // 空白填充
            for (int i = 0; i < startOffset; i++)
                CalendarDays.Add(null);

            // 添加所有日期卡片
            for (int day = 1; day <= daysInMonth; day++)
                CalendarDays.Add(new DateTime(Year, Month, day));
        }

        private async void LoadTasksForCurrentMonth()
        {
            TasksByDate.Clear();

            var start = new DateTime(Year, Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var tasks = await _taskService.GetTasksForDateRange(start, end);
            foreach (var group in tasks.Where(t => t.Mode == TaskMode.Strong).GroupBy(t => t.PlannedDate.Date))
            {
                var sorted = group.OrderBy(t => t.StartTime ?? TimeSpan.Zero);
                TasksByDate[group.Key] = new ObservableCollection<TaskModel>(sorted);
            }

            OnPropertyChanged(nameof(TasksByDate));
        }

        /// <summary>
        /// 显示复盘页面
        /// </summary>
        private void ShowReview()
        {
            var nav = App.AppHost.Services.GetRequiredService<INavigationService>();
            nav.NavigateTo(AppFrame.Instance!, "Everyday");
        }

        /// <summary>
        /// 显示添加任务表单
        /// </summary>
        private async void ShowAddTaskDialog(DateTime date)
        {

            var dialog = new AddTaskDialog(date);

            if (dialog.ShowDialog() == true && dialog.ResultTask != null)
            {
                var task = dialog.ResultTask!;
                task.Mode = TaskMode.Strong;
                task.Status = MyTaskStatus.Pending;  //设成待处理
                task.IsCompleted = false;                 //设成未完成

                // 持久化到数据库
                await _taskService.UpdateTaskAsync(task);

                // 触发复盘自动刷新
                App.NotifyTaskChanged(task);
                Debug.WriteLine($"🛎️ App.NotifyTaskChanged({task.Name}) 已调用完毕");
            }

        }

        private void AddTaskToDictionary(TaskModel task)
        {
            var key = task.PlannedDate.Date;
            if (!TasksByDate.TryGetValue(key, out var list))
            {
                list = new ObservableCollection<TaskModel>();
                TasksByDate[key] = list;
            }
            int index = list.TakeWhile(t => (t.StartTime ?? TimeSpan.Zero) <= (task.StartTime ?? TimeSpan.Zero)).Count();
            list.Insert(index, task);
            OnPropertyChanged(nameof(TasksByDate));
        }

        private void OnExternalTaskSaved(TaskModel task)
        {
            if (task.Mode != TaskMode.Strong)
                return;

            if (task.PlannedDate.Year != Year || task.PlannedDate.Month != Month)
                return;

            AddTaskToDictionary(task);
        }

        private void OnCurrentMonthChanged(DateTime newMonth)
        {
            CurrentMonthChanged?.Invoke(this, newMonth);
        }

        public void NavigateToToday()
        {
            CurrentMonth = DateTime.Today;
            CheckTodayTasks();
        }

        public async void CheckTodayTasks()
        {
            if (CurrentMonth.Year == DateTime.Now.Year &&
                CurrentMonth.Month == DateTime.Now.Month)
            {
                var todayTasks = await GetTasksForDate(DateTime.Today);
                if (todayTasks?.Any() == true)
                {
                    //TodayTasksFound?.Invoke(this, todayTasks);
                    // 只过滤出开启了提醒的任务
                    var reminderTasks = new ObservableCollection<TaskModel>(
                        todayTasks.Where(t => t.IsReminderEnabled));

                    if (reminderTasks.Any())
                    {
                        TodayTasksFound?.Invoke(this, reminderTasks);
                    }
                }
            }
        }

        public async Task<ObservableCollection<TaskModel>?> GetTasksForDate(DateTime date)
        {
            var tasks = await _taskService.GetTasksForDateRange(date, date);
            var strongTasks = tasks.Where(t => t.Mode == TaskMode.Strong).ToList();
            return strongTasks.Any() ? new ObservableCollection<TaskModel>(strongTasks.OrderBy(t => t.StartTime ?? TimeSpan.Zero)) : null;
        }

        // INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 属性变更通知方法
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}