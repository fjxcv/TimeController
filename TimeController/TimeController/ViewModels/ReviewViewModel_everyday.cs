using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Services;
using System.Windows.Controls;
using TimeController.Views.Dialogs;
using System.Diagnostics;

namespace TimeController.ViewModels
{

    public class ReviewViewModel_everyday : INotifyPropertyChanged
    {
        private readonly ITaskService _taskService;
        private DateTime? _selectedDate;
        private ObservableCollection<TaskModel> _todayPendingTasks;
        private ObservableCollection<TaskModel> _overduePendingTasks;
        private ObservableCollection<string> _reviewReasons;
        public bool IsEverydayPage { get; set; } = true;
        public event Action? NavigateToEveryweekRequested;


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
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));
                LoadTasksForDate(value ?? DateTime.Today); //无条件刷新
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
            IsEverydayPage = true;
            _taskService = taskService;

//#if DEBUG
//            _ = ResetDataForDevelopment();
//#endif

            CompletedTasks = new ObservableCollection<TaskModel>();
            UncompletedTasks = new ObservableCollection<TaskModel>();

            ShowAbandonMenuCommand = new RelayCommand<Button>(ShowAbandonReasonMenu);
            AbandonReasonCommand = new RelayCommand<Tuple<TaskModel, string>>(AbandonWithReason);

            ShowPostponeMenuCommand = new RelayCommand<Button>(ShowPostponeReasonMenu);
            PostponeReasonCommand = new RelayCommand<Tuple<TaskModel, string>>(PostponeWithReason);

            NavigateToEverydayCommand = new RelayCommand(_ => { }); // 当前页，不跳转
            NavigateToEveryweekCommand = new RelayCommand(_ => NavigateToEveryweekRequested?.Invoke());


            ReviewReasons = new ObservableCollection<string>
            {
                "时间安排问题",
                "主观状态问题",
                "外部干扰",
                "自主延迟决策",
                "动机缺失",
                "不明确"
            };

            // 订阅带参事件
            App.TaskChanged += newTask =>
            {
                // 刷新：用新任务的 PlannedDate
                var dateToLoad = newTask.PlannedDate.Date;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine($"{dateToLoad:yyyy-MM-dd} 刷新复盘");
                    LoadTasksForDate(dateToLoad);
                });
            };

            // 初始化待办任务
            SelectedDate = DateTime.Today;

        }

        private async void LoadTasksForDate(DateTime date)
        {
            // 清空现有任务
            CompletedTasks.Clear();
            UncompletedTasks.Clear();

            // 从数据库或服务中获取指定日期的任务
            var allTasks = await _taskService.GetTasksForDate(date);

            // 过滤出强管理的非课程任务
            var tasks = allTasks
            .Where(t => t.Mode == TaskMode.Strong && !t.IsCourseTask)
            .ToList();

            //调试！！！！
            System.Diagnostics.Debug.WriteLine($"任务加载数: {tasks.Count}");
            Debug.WriteLine($"▶▶ LoadTasksForDate for {date:yyyy-MM-dd}, total fetched: {tasks.Count}");
            foreach (var t in tasks)
            {
                Debug.WriteLine($"任务: {t.Name} 状态: {t.Status} 日期: {t.PlannedDate:yyyy-MM-dd}");
            }

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
            var today = date.Date;
            TodayPendingTasks = new ObservableCollection<TaskModel>(
                tasks.Where(t =>
                (t.Status == MyTaskStatus.Pending || t.Status == MyTaskStatus.Postponed) &&
                t.PlannedDate.Date == today));      //只要PlannedDate == date，就显示


            // 获取所有 pending 任务，排除课程任务
            var allPending = await _taskService.GetAllPendingTasksAsync();

            OverduePendingTasks = new ObservableCollection<TaskModel>(
                allPending
                  .Where(t =>
                        t.Mode == TaskMode.Strong
                        && t.Status == MyTaskStatus.Pending
                        && t.PlannedDate.Date < today
                        && !t.IsCourseTask));

            Debug.WriteLine($"📝 过期任务数 = {OverduePendingTasks.Count}");

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
            task.MarkAbandoned(DateTime.Now);

            await _taskService.UpdateTaskAsync(task);
            LoadTasksForDate(SelectedDate ?? DateTime.Today);

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
            if (dialog.ShowDialog() != true || !dialog.SelectedDate.HasValue)
            {
                task.Reason = null;
                return;
            }

            var newDate = dialog.SelectedDate.Value;

            // 记录推迟历史戳
            task.PostponedAt = DateTime.Now;

            task.PostponedCount += 1;

            // 更新到新日期
            task.PostponeDate = newDate;
            task.PlannedDate = newDate;
            // —— 关键：第一次推迟也把状态设为 Pending，这样它就会被 TodayPendingTasks 包括进来 —— 
            task.Status = MyTaskStatus.Pending;

            await _taskService.UpdateTaskAsync(task);

            LoadTasksForDate(SelectedDate ?? DateTime.Today);

        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}