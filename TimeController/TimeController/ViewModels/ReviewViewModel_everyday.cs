using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Services;
using System.Windows.Controls;
using TimeController.Views.Dialogs;

namespace TimeController.ViewModels
{

    public class ReviewViewModel_everyday : INotifyPropertyChanged
    {
        private readonly ITaskService _taskService;
        public event Action? NavigateToEveryweekRequested;
        //private readonly INavigationService _navigationService;
        private bool _isEverydayPage = true;
        private bool _isAllDay;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private DateTime? _plannedDate;
        private DateTime? _selectedDate;
        private ObservableCollection<TaskModel> _todayPendingTasks;
        private ObservableCollection<TaskModel> _overduePendingTasks;
        private ObservableCollection<string> _reviewReasons;

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
        public ICommand PostponeTaskCommand { get; }
        public ICommand AbandonTaskCommand { get; }
        public ICommand BatchProcessCommand { get; }
        
        //放弃和推迟按钮的命令声明
        public ICommand ShowAbandonMenuCommand { get; }
        public ICommand AbandonReasonCommand { get; }

        public ICommand ShowPostponeMenuCommand { get; }
        public ICommand PostponeReasonCommand { get; }


        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
                    LoadTasksForDate(value ?? DateTime.Today);
                }
            }
        }

        
        public ObservableCollection<TaskModel> CompletedTasks { get; set; }
        public ObservableCollection<TaskModel> UncompletedTasks { get; set; }
        public ObservableCollection<TaskModel> TodayPendingTasks
        {
            get => _todayPendingTasks;
            set
            {
                _todayPendingTasks = value;
                OnPropertyChanged(nameof(TodayPendingTasks));
            }
        }

        public ObservableCollection<TaskModel> OverduePendingTasks
        {
            get => _overduePendingTasks;
            set
            {
                _overduePendingTasks = value;
                OnPropertyChanged(nameof(OverduePendingTasks));
            }
        }

        public int PendingTasksCount => OverduePendingTasks.Count;

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

        public ReviewViewModel_everyday(ITaskService taskService)
        {
            //_navigationService = navigationService;
            _taskService = taskService;

            CompletedTasks = new ObservableCollection<TaskModel>();
            UncompletedTasks = new ObservableCollection<TaskModel>();

            ShowAbandonMenuCommand = new RelayCommand<Button>(ShowAbandonReasonMenu);
            AbandonReasonCommand = new RelayCommand<Tuple<TaskModel, string>>(AbandonWithReason);

            ShowPostponeMenuCommand = new RelayCommand<Button>(ShowPostponeReasonMenu);
            PostponeReasonCommand = new RelayCommand<Tuple<TaskModel, string>>(PostponeWithReason);

            NavigateToEverydayCommand = new RelayCommand(_ => { });
            NavigateToEveryweekCommand = new RelayCommand(_ => NavigateToEveryweekRequested?.Invoke());

            PostponeTaskCommand = new RelayCommand<TaskModel>(PostponeTask);
            AbandonTaskCommand = new RelayCommand<TaskModel>(AbandonTask);
            BatchProcessCommand = new RelayCommand<object>(BatchProcess);


            ReviewReasons = new ObservableCollection<string>
            {
                "时间安排问题",
                "主观状态问题",
                "外部干扰",
                "自主延迟决策",
                "动机缺失",
                "不明确"
            };


            SelectedDate = DateTime.Today;
        }


        private async void LoadTasksForDate(DateTime date)
        {
            // 清空现有任务
            CompletedTasks.Clear();
            UncompletedTasks.Clear();

            // 从数据库或服务中获取指定日期的任务
            var tasks = await _taskService.GetTasksForDate(date);

            // 分类任务
            foreach (var task in tasks)
            {
                if (task.Status == MyTaskStatus.Completed)
                {
                    CompletedTasks.Add(task);
                }
                else
                {
                    UncompletedTasks.Add(task);
                }
            }

            // 更新待处理任务
            var today = DateTime.Today;
            TodayPendingTasks = new ObservableCollection<TaskModel>(
                tasks.Where(t => t.Status == MyTaskStatus.Pending && t.PlannedDate.Date == today));

            OverduePendingTasks = new ObservableCollection<TaskModel>(
                tasks.Where(t => t.Status == MyTaskStatus.Pending && t.PlannedDate.Date < today));

            OnPropertyChanged(nameof(PendingTasksCount));
        }

        private void ShowAbandonReasonMenu(Button button)
        {
            if (button?.DataContext is not TaskModel task || task.Status != MyTaskStatus.Pending)
                return;

            var contextMenu = new ContextMenu();

            foreach (var reason in ReviewReasons)
            {
                var item = new MenuItem
                {
                    Header = reason,
                    DataContext = new Tuple<TaskModel, string>(task, reason),
                    Command = AbandonReasonCommand,
                    CommandParameter = new Tuple<TaskModel, string>(task, reason)
                };
                contextMenu.Items.Add(item);
            }

            contextMenu.PlacementTarget = button;
            contextMenu.IsOpen = true;
        }

        // 放弃任务（异步加保存到数据库）
        private async void AbandonWithReason(Tuple<TaskModel, string> param)
        {
            var (task, reason) = param;
            task.Reason = reason;
            task.Status = MyTaskStatus.Abandoned;

            await _taskService.UpdateTaskAsync(task);
        }


        private void ShowPostponeReasonMenu(Button button)
        {
            if (button?.DataContext is not TaskModel task || task.Status != MyTaskStatus.Pending)
                return;

            var contextMenu = new ContextMenu();

            foreach (var reason in ReviewReasons)
            {
                var item = new MenuItem
                {
                    Header = reason,
                    DataContext = new Tuple<TaskModel, string>(task, reason),
                    Command = PostponeReasonCommand,
                    CommandParameter = new Tuple<TaskModel, string>(task, reason)
                };
                contextMenu.Items.Add(item);
            }

            contextMenu.PlacementTarget = button;
            contextMenu.IsOpen = true;
        }

        // 推迟任务（异步加保存到数据库）
        private async void PostponeWithReason(Tuple<TaskModel, string> param)
        {
            var (task, reason) = param;
            task.Reason = reason;

            var dialog = new PostponeDateDialog();
            bool? result = dialog.ShowDialog();

            if (result == true && dialog.SelectedDate.HasValue)
            {
                task.PostponeDate = dialog.SelectedDate;
                task.Status = MyTaskStatus.Postponed;

                await _taskService.UpdateTaskAsync(task);
            }
            else
            {
                task.Reason = null;
            }
        }



        private void PostponeTask(TaskModel task)
        {
            // TODO: 实现推迟任务的逻辑
        }

        private void AbandonTask(TaskModel task)
        {
            // TODO: 实现放弃任务的逻辑
        }

        private void BatchProcess(object parameter)
        {
            // TODO: 实现批量处理任务的逻辑
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}