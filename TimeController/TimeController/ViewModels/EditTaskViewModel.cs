using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TimeController.Models;

namespace TimeController.ViewModels
{
    public class EditTaskViewModel : INotifyPropertyChanged
    {
        public TaskModel Task { get; }
        public bool IsEdit { get; }

        // 构造：编辑已有任务
        public EditTaskViewModel(TaskModel existing)
        {
            Task = existing;
            IsEdit = true;

            UpdateTimePeriodHint();
        }

        // 以下属性直接映射 TaskModel
        public string Name
        {
            get => Task.Name;
            set { Task.Name = value; OnPropertyChanged(); }
        }
        public string Note
        {
            get => Task.Note;
            set { Task.Note = value; OnPropertyChanged(); }
        }
        public TaskType Type
        {
            get => Task.Type;
            set { Task.Type = value; OnPropertyChanged(); }
        }
        public bool IsAllDay
        {
            get => Task.IsAllDay;
            set
            {
                Task.IsAllDay = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimeSelectionEnabled));
                UpdateTimePeriodHint(); // 更新时间段提示，全天任务不显示时间段提示
        }
        }
        public bool IsReminderEnabled
        {
            get => Task.IsReminderEnabled;
            set { Task.IsReminderEnabled = value; OnPropertyChanged(); }
        }
        public DateTime PlannedDate
        {
            get => Task.PlannedDate;
            set { Task.PlannedDate = value; OnPropertyChanged(); }
        }

        // 添加IsTimeSelectionEnabled属性
        public bool IsTimeSelectionEnabled => !IsAllDay;

        private void UpdateTimePeriodHint()
        {
            if (!IsAllDay && Task.StartTime.HasValue && Task.EndTime.HasValue)
            {
                TimeSpan startTime = Task.StartTime.Value;
                TimeSpan endTime = Task.EndTime.Value;

                Debug.WriteLine($"检查时间段: {startTime} - {endTime}");

                // 判断时间段是否在7:00-12:00之间
                if (startTime >= new TimeSpan(7, 0, 0) && endTime <= new TimeSpan(12, 0, 0))
                {
                    TimePeriodHint = "7:00-12:00为高度集中和强自制力时间段\n推荐进行不感兴趣且消耗精力的事情";
                    Debug.WriteLine($"设置提示: {TimePeriodHint}");
                    return;
                }

                // 判断时间段是否在13:00-18:00之间
                if (startTime >= new TimeSpan(13, 0, 0) && endTime <= new TimeSpan(18, 0, 0))
                {
                    TimePeriodHint = "13:00-18:00为高强度能量和低自制力时间段\n推荐进行感兴趣但又消耗精力的事情";
                    Debug.WriteLine($"设置提示: {TimePeriodHint}");
                    return;
                }

                // 判断时间段是否在19:00-20:00之间
                if (startTime >= new TimeSpan(19, 0, 0) && endTime <= new TimeSpan(20, 0, 0))
                {
                    TimePeriodHint = "19:00-20:00为低能量和低自制力时间段\n推荐进行有趣且不消耗精力的事情";
                    Debug.WriteLine($"设置提示: {TimePeriodHint}");
                    return;
                }

                // 不在特定时间段内，清空提示
                TimePeriodHint = string.Empty;
                Debug.WriteLine("清空提示: 不在特定时间段内");
            }
            else
            {
                // 全天任务或时间未设置，清空提示
                TimePeriodHint = string.Empty;
            }
        }

        private DateTime? _startTimeWrapper;
        public DateTime? StartTimeWrapper
        {
            get => Task.StartTime.HasValue ? DateTime.Today + Task.StartTime.Value : null;
            set
            {
                _startTimeWrapper = value;
                Task.StartTime = value?.TimeOfDay;
                OnPropertyChanged();

                if (value.HasValue)
                {
                    var newStart = value.Value.TimeOfDay;
                    if (!Task.EndTime.HasValue ||
                        Task.EndTime <= newStart ||
                        Task.EndTime - newStart > TimeSpan.FromHours(12))
                    {
                        Task.EndTime = newStart.Add(TimeSpan.FromHours(1));
                        OnPropertyChanged(nameof(EndTimeWrapper));
                        Debug.WriteLine($"【时间段提示】自动调整EndTime为{Task.EndTime}");
                    }
                }

                ValidateTimeRange(); // 开始时间<结束时间
                UpdateTimePeriodHint(); // 更新时间段提示
            }
        }

        private DateTime? _endTimeWrapper;
        public DateTime? EndTimeWrapper
        {
            get => Task.EndTime.HasValue ? DateTime.Today + Task.EndTime.Value : null;
            set
            {
                _endTimeWrapper = value;
                Task.EndTime = value?.TimeOfDay;
                OnPropertyChanged();
                Debug.WriteLine($"【时间段提示】EndTime改变为{value?.TimeOfDay}");
                ValidateTimeRange(); // 开始时间<结束时间
                UpdateTimePeriodHint(); // 更新时间段提示
            }
        }

        public string NoteCharHint => $"{Note?.Length ?? 0}/200";

        // 确保_timePeriodHint有初始值
        private string _timePeriodHint = string.Empty;

        public string TimePeriodHint
        {
            get => _timePeriodHint;
            private set
            {
                if (_timePeriodHint != value)
                {
                    _timePeriodHint = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasTimePeriodHint));
                    Debug.WriteLine($"【时间段提示】TimePeriodHint已更新，HasTimePeriodHint = {HasTimePeriodHint}");
                }
            }
        }

        public bool HasTimePeriodHint => !string.IsNullOrEmpty(TimePeriodHint);
        public bool IsTimeValid
        {
            get
            {
                if (IsAllDay) return true;
                return Task.StartTime.HasValue && Task.EndTime.HasValue && Task.StartTime <= Task.EndTime;
            }
        }
        public string TimeError => IsTimeValid ? string.Empty : "开始时间不能晚于结束时间";

        // 添加时间验证
        private bool _hasTimeError;
        public bool HasTimeError
        {
            get => _hasTimeError;
            private set
            {
                if (_hasTimeError != value)
                {
                    _hasTimeError = value;
                    OnPropertyChanged();
                }
            }
        }

        // 实现ValidateTimeRange方法
        private void ValidateTimeRange()
        {
            if (Task.StartTime.HasValue && Task.EndTime.HasValue)
            {
                if (Task.StartTime.Value < Task.EndTime.Value)
                {
                    HasTimeError = false;
                }
                else
                {
                    HasTimeError = true;
                }
            }
            else
            {
                // 如果某个时间未设置，则不显示错误
                HasTimeError = false;
            }

            // 通知IsTimeValid属性已更改
            OnPropertyChanged(nameof(IsTimeValid));
            OnPropertyChanged(nameof(TimeError));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}