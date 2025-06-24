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

        private DateTime? _startTimeWrapper;
        private DateTime? _endTimeWrapper;
        private bool _isTimeValid = true;
        private string _timeError = "";

        public EditTaskViewModel(TaskModel existing)
        {
            Task = existing;
            IsEdit = true;

            // 1. 初始化 TimeWrapper 字段，使它们和 Task 里已有的时间同步
            if (Task.StartTime.HasValue)
                _startTimeWrapper = DateTime.Today + Task.StartTime.Value;
            if (Task.EndTime.HasValue)
                _endTimeWrapper = DateTime.Today + Task.EndTime.Value;

            // 2. 触发一次初始的 PropertyChanged，让界面立刻拿到最新值
            OnPropertyChanged(nameof(IsAllDay));
            OnPropertyChanged(nameof(IsTimeSelectionEnabled));
            OnPropertyChanged(nameof(StartTimeWrapper));
            OnPropertyChanged(nameof(EndTimeWrapper));

            // 3. 校验一次时间合法性（如果是全天，就永远合法）
            ValidateTimeRange();
        }

        //—— 直接映射 TaskModel 的属性 ——//

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
                if (Task.IsAllDay != value)
                {
                    Task.IsAllDay = value;
                    OnPropertyChanged();
                    // 全天切换，也要更新“时间选择是否可用”
                    OnPropertyChanged(nameof(IsTimeSelectionEnabled));
                    ValidateTimeRange();
                }
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

        //—— 时间包装属性 + 校验 ——//

        public DateTime? StartTimeWrapper
        {
            get => _startTimeWrapper;
            set
            {
                _startTimeWrapper = value;
                Task.StartTime = value?.TimeOfDay;
                OnPropertyChanged();
                ValidateTimeRange();
            }
        }

        public DateTime? EndTimeWrapper
        {
            get => _endTimeWrapper;
            set
            {
                _endTimeWrapper = value;
                Task.EndTime = value?.TimeOfDay;
                OnPropertyChanged();
                ValidateTimeRange();
            }
        }

        private void ValidateTimeRange()
        {
            if (IsAllDay)
            {
                IsTimeValid = true;
                TimeError = "";
            }
            else if (Task.StartTime.HasValue && Task.EndTime.HasValue && Task.StartTime <= Task.EndTime)
            {
                IsTimeValid = true;
                TimeError = "";
            }
            else
            {
                IsTimeValid = false;
                TimeError = "开始时间不能晚于结束时间";
            }
            OnPropertyChanged(nameof(HasTimeError));
        }

        //—— 校验结果、UI 状态属性 ——//

        public bool IsTimeValid
        {
            get => _isTimeValid;
            private set
            {
                if (_isTimeValid != value)
                {
                    _isTimeValid = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TimeError
        {
            get => _timeError;
            private set
            {
                if (_timeError != value)
                {
                    _timeError = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasTimeError => !string.IsNullOrEmpty(TimeError);

        /// <summary>
        /// 全日任务勾上后，时间选择器禁用
        /// </summary>
        public bool IsTimeSelectionEnabled => !IsAllDay;

        //—— INotifyPropertyChanged ——//

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
