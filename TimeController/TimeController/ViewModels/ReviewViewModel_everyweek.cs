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

namespace TimeController.ViewModels
{
    public class ReviewViewModel_everyweek : INotifyPropertyChanged
    {
        private readonly ITaskService _taskService;
        public event Action? NavigateToEverydayRequested;
        private DateTime _selectedWeekStart;
        private ObservableCollection<TaskModel> _weeklyCompletedTasks;
        private ObservableCollection<TaskModel> _weeklyUncompletedTasks;
        private ObservableCollection<string> _reviewReasons;

        public DateTime SelectedWeekStart
        {
            get => _selectedWeekStart;
            set
            {
                if (_selectedWeekStart != value)
                {
                    _selectedWeekStart = value;
                    OnPropertyChanged(nameof(SelectedWeekStart));
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

        private bool _isEverydayPage = false;
        public bool IsEverydayPage
        {
            get => _isEverydayPage;
            set
            {
                if (_isEverydayPage != value)
                {
                    _isEverydayPage = value;
                    OnPropertyChanged(nameof(IsEverydayPage));
                }
            }
        }

        public ICommand NavigateToEverydayCommand { get; }
        public ICommand NavigateToEveryweekCommand { get; }
        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }

        public ReviewViewModel_everyweek()
        {
            IsEverydayPage = false;

            NavigateToEverydayCommand = new RelayCommand(_ =>
            {
                NavigateToEverydayRequested?.Invoke();
            });
            NavigateToEverydayCommand = new RelayCommand(_ => NavigateToEverydayRequested?.Invoke());
            NavigateToEveryweekCommand = new RelayCommand(_ => { }); // 当前页，不处理

            PreviousWeekCommand = new RelayCommand(_ => MoveToPreviousWeek());
            NextWeekCommand = new RelayCommand(_ => MoveToNextWeek());

            // 初始化周开始日期为本周一
            SelectedWeekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            WeeklyCompletedTasks = new ObservableCollection<TaskModel>();
            WeeklyUncompletedTasks = new ObservableCollection<TaskModel>();

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

            LoadTasksForWeek(SelectedWeekStart);
        }

        private async void LoadTasksForWeek(DateTime weekStart)
        {
            //清空任务
            WeeklyCompletedTasks.Clear();
            WeeklyUncompletedTasks.Clear();
            SkippedTasks.Clear(); 

            var weekEnd = weekStart.AddDays(6);

            var tasks = await _taskService.GetTasksForDateRange(weekStart, weekEnd);

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
        }

        private void MoveToPreviousWeek()
        {
            SelectedWeekStart = SelectedWeekStart.AddDays(-7);
        }

        private void MoveToNextWeek()
        {
            SelectedWeekStart = SelectedWeekStart.AddDays(7);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}