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
        //private readonly INavigationService _navigationService;
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

        public ReviewViewModel_everyweek()
        {
            //_navigationService = navigationService;
            NavigateToEverydayCommand = new RelayCommand(_ =>
            {
                NavigateToEverydayRequested?.Invoke();
            });
            NavigateToEveryweekCommand = new RelayCommand(_ => { });
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
            // 清空现有任务
            WeeklyCompletedTasks.Clear();
            WeeklyUncompletedTasks.Clear();

            // 计算周结束日期
            var weekEnd = weekStart.AddDays(6);

            // 从数据库或服务中获取指定周的任务
            var tasks = await _taskService.GetTasksForDateRange(weekStart, weekEnd);



            // 分类任务
            foreach (var task in tasks)
            {
                if (task.Status == MyTaskStatus.Completed)
                {
                    WeeklyCompletedTasks.Add(task);
                }
                else
                {
                    WeeklyUncompletedTasks.Add(task);
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