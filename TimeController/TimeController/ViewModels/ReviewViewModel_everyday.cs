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
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

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

        // 命令声明
        public ICommand NavigateToEverydayCommand { get; }
        public ICommand NavigateToEveryweekCommand { get; }
        public ICommand PostponeTaskCommand { get; }
        public ICommand AbandonTaskCommand { get; }
        public ICommand BatchProcessCommand { get; }
        public ICommand ShowAbandonMenuCommand { get; }
        public ICommand AbandonReasonCommand { get; }
        public ICommand ShowPostponeMenuCommand { get; }
        public ICommand PostponeReasonCommand { get; }

        // 属性绑定
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged(nameof(SelectedDate));
                LoadTasksForDate(value ?? DateTime.Today); // 每次变更选中日期都刷新
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

            CompletedTasks = new ObservableCollection<TaskModel>();
            UncompletedTasks = new ObservableCollection<TaskModel>();

            // 初始化命令
            ShowAbandonMenuCommand = new RelayCommand<Button>(ShowAbandonReasonMenu);
            AbandonReasonCommand = new RelayCommand<Tuple<TaskModel, string>>(AbandonWithReason);
            ShowPostponeMenuCommand = new RelayCommand<Button>(ShowPostponeReasonMenu);
            PostponeReasonCommand = new RelayCommand<Tuple<TaskModel, string>>(PostponeWithReason);

            NavigateToEverydayCommand = new RelayCommand(_ => { }); // 当前页，不跳转
            NavigateToEveryweekCommand = new RelayCommand(_ => NavigateToEveryweekRequested?.Invoke());

            // 设定原因下拉菜单的选项
            ReviewReasons = new ObservableCollection<string>
            {
                "时间安排问题",
                "主观状态问题",
                "外部干扰",
                "自主延迟决策",
                "动机缺失",
                "不明确"
            };

            // 订阅任务更新事件，刷新对应日期的任务
            App.TaskChanged += newTask =>
            {
                var dateToLoad = newTask.PlannedDate.Date;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Debug.WriteLine($"{dateToLoad:yyyy-MM-dd} 刷新复盘");
                    LoadTasksForDate(dateToLoad);
                });
            };

            // 默认加载今天任务
            SelectedDate = DateTime.Today;
        }

        private async void LoadTasksForDate(DateTime date)
        {
            CompletedTasks.Clear();
            UncompletedTasks.Clear();

            var allTasks = await _taskService.GetTasksForDate(date);

            // 筛选出强管理且不是课程任务的列表
            var tasks = allTasks
                .Where(t => t.Mode == TaskMode.Strong && !t.IsCourseTask)
                .ToList();

            // 分类添加任务到对应集合
            foreach (var task in tasks)
            {
                if (task.Status == MyTaskStatus.Completed)
                    CompletedTasks.Add(task);
                else
                    UncompletedTasks.Add(task);
            }

            // 生成今日的待完成任务
            var today = date.Date;
            TodayPendingTasks = new ObservableCollection<TaskModel>(
                tasks.Where(t =>
                    (t.Status == MyTaskStatus.Pending || t.Status == MyTaskStatus.Postponed)
                    && t.PlannedDate.Date == today));

            // 查询所有未完成任务中已过期的部分作为逾期任务
            var allPending = await _taskService.GetAllPendingTasksAsync();
            OverduePendingTasks = new ObservableCollection<TaskModel>(
                allPending.Where(t =>
                    t.Mode == TaskMode.Strong &&
                    t.Status == MyTaskStatus.Pending &&
                    t.PlannedDate.Date < today &&
                    !t.IsCourseTask));

            OnPropertyChanged(nameof(PendingTasksCount));
        }

        private void ShowAbandonReasonMenu(Button button)
        {
            if (button?.DataContext is not TaskModel task || task.Status != MyTaskStatus.Pending)
                return;

            var contextMenu = new ContextMenu();

            // 为每个原因创建菜单项
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

        private async void PostponeWithReason(Tuple<TaskModel, string> param)
        {
            var (task, reason) = param;
            task.Reason = reason;

            // 弹出日期选择对话框
            var dialog = new PostponeDateDialog();
            if (dialog.ShowDialog() != true || !dialog.SelectedDate.HasValue)
            {
                task.Reason = null;
                return;
            }

            var newDate = dialog.SelectedDate.Value;

            bool convertToAllDay = false;

            if (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
            {
                var tasksOnDate = await _taskService.GetTasksForDate(newDate);
                var conflicts = tasksOnDate
                    .Where(t => !t.IsAllDay && t.Id != task.Id &&
                           t.StartTime.HasValue && t.EndTime.HasValue &&
                           t.StartTime.Value < t.EndTime.Value &&
                           !(task.EndTime.Value <= t.StartTime.Value || task.StartTime.Value >= t.EndTime.Value))
                    .Select(t => t.Name)
                    .ToList();

                if (conflicts.Any())
                {
                    string conflictNames = string.Join("\n- ", conflicts);
                    MessageBox.Show($"推迟后与以下任务时间冲突:\n- {conflictNames}\n本任务将自动转为全天任务。",
                                   "任务时间冲突", MessageBoxButton.OK, MessageBoxImage.Warning);
                    convertToAllDay = true;
                }
            }

            // 记录推迟历史与计数
            task.PostponedAt = DateTime.Now;
            task.PostponedCount += 1;

            // 更新任务为新的计划日期
            task.PostponeDate = newDate;
            task.PlannedDate = newDate;
            task.Status = MyTaskStatus.Pending;

            if (convertToAllDay)
            {
                task.IsAllDay = true;
                task.StartTime = null;
                task.EndTime = null;
            }

            await _taskService.UpdateTaskAsync(task);
            LoadTasksForDate(SelectedDate ?? DateTime.Today);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
