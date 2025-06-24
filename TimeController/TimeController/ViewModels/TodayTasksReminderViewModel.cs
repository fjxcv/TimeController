using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Timers;
using TimeController.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TimeController.ViewModels
{
    public class TodayTasksReminderViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly System.Timers.Timer _checkTimer;
        private readonly ObservableCollection<TaskModel> _allTasks;
        private bool _disposed;

        // 全天任务集合
        public ObservableCollection<TaskModel> AllDayTasks { get; }
        // 当前活动任务集合
        public ObservableCollection<TaskModel> ActiveTasks { get; }

        public ICommand CloseCommand { get; }

        public TodayTasksReminderViewModel(ObservableCollection<TaskModel> tasks, Action closeAction)
        {
            _allTasks = tasks;
            AllDayTasks = new ObservableCollection<TaskModel>();
            ActiveTasks = new ObservableCollection<TaskModel>();
            CloseCommand = new RelayCommand(_ => closeAction());

            // 初始化任务分类
            InitializeTasks();

            // 设置定时器，每分钟检查一次
            _checkTimer = new System.Timers.Timer(60000); // 60000ms = 1分钟
            _checkTimer.Elapsed += CheckTimer_Elapsed;
            _checkTimer.Start();
        }

        private void InitializeTasks()
        {
            // 分类全天任务和非全天任务
            var allDayTasks = _allTasks.Where(t => t.IsAllDay && t.IsReminderEnabled && !t.IsCompleted).ToList();
            var nonAllDayTasks = _allTasks.Where(t => !t.IsAllDay && t.IsReminderEnabled && !t.IsCompleted).ToList();

            // 更新全天任务列表
            AllDayTasks.Clear();
            foreach (var task in allDayTasks)
            {
                AllDayTasks.Add(task);
            }

            // 检查并添加当前应该显示的非全天任务
            CheckAndUpdateActiveTasks(nonAllDayTasks);
        }

        private void CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            // 在UI线程上更新任务
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var nonAllDayTasks = _allTasks.Where(t => !t.IsAllDay && t.IsReminderEnabled && !t.IsCompleted).ToList();
                CheckAndUpdateActiveTasks(nonAllDayTasks);
            });
        }

        private void CheckAndUpdateActiveTasks(System.Collections.Generic.List<TaskModel> tasks)
        {
            var now = DateTime.Now.TimeOfDay;
            var activeTasks = tasks.Where(t => 
                t.StartTime <= now && 
                (t.EndTime == null || now <= t.EndTime)).ToList();

            // 更新活动任务列表
            ActiveTasks.Clear();
            foreach (var task in activeTasks)
            {
                ActiveTasks.Add(task);
            }

            // 通知UI更新
            OnPropertyChanged(nameof(ActiveTasks));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _checkTimer?.Stop();
                    _checkTimer?.Dispose();
                }
                _disposed = true;
            }
        }
    }
} 