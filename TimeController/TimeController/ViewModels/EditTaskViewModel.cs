using System;
using System.ComponentModel;
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
            set { Task.IsAllDay = value; OnPropertyChanged(); }
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

        private DateTime? _startTimeWrapper;
        public DateTime? StartTimeWrapper
        {
            get => Task.StartTime.HasValue ? DateTime.Today + Task.StartTime.Value : _startTimeWrapper;
            set
            {
                _startTimeWrapper = value;
                Task.StartTime = value?.TimeOfDay;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimeValid));
                OnPropertyChanged(nameof(TimeError));
            }
        }

        private DateTime? _endTimeWrapper;
        public DateTime? EndTimeWrapper
        {
            get => Task.EndTime.HasValue ? DateTime.Today + Task.EndTime.Value : _endTimeWrapper;
            set
            {
                _endTimeWrapper = value;
                Task.EndTime = value?.TimeOfDay;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimeValid));
                OnPropertyChanged(nameof(TimeError));
            }
        }

        public bool IsTimeValid
        {
            get
            {
                if (IsAllDay) return true;
                return Task.StartTime.HasValue && Task.EndTime.HasValue && Task.StartTime <= Task.EndTime;
            }
        }
        public string TimeError => IsTimeValid ? string.Empty : "开始时间不能晚于结束时间";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}