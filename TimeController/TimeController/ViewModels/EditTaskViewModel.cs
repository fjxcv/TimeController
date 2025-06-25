using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using TimeController.Models;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.ViewModels
{
    public class EditTaskViewModel : INotifyPropertyChanged
    {
        private const int MaxNoteLength = 200;
        public TaskModel Task { get; }
        public bool IsEdit { get; }

        private DateTime? _startTimeWrapper;
        private DateTime? _endTimeWrapper;
        private bool _isFormValid = true;
        private bool _isTimeValid = true;
        private string _timeError = "";
        private string _nameError = "";

        private string _timePeriodHint = "";

        public RelayCommand SaveCommand { get; }
        public Action? CloseAction { get; set; }

        public EditTaskViewModel(TaskModel existing)
        {
            Task = existing;
            IsEdit = true;

            // 1. 初始化 TimeWrapper 字段，使它们和 Task 里已有的时间同步
            if (Task.StartTime.HasValue)
                _startTimeWrapper = DateTime.Today + Task.StartTime.Value;
            if (Task.EndTime.HasValue)
                _endTimeWrapper = Task.EndTime == TimeSpan.FromHours(24)
                    ? DateTime.Today.AddDays(1)  // 映射 24:00
                    : DateTime.Today + Task.EndTime.Value;

            SaveCommand = new RelayCommand(_ => ExecuteSave());

            // 初始校验
            ValidateName();
            ValidateTimeRange();
            UpdateTimePeriodHint();
            UpdateFormValidity();
        }

        private void ExecuteSave()
        {
            // 名称非空校验
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("任务名称不能为空", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 时间合法性校验
            if (!IsAllDay && (!Task.StartTime.HasValue || !Task.EndTime.HasValue || Task.StartTime > Task.EndTime))
            {
                MessageBox.Show(TimeError == "" ? "开始时间不能晚于结束时间" : TimeError,
                                "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OnSave();
        }

        // —— 保存时回调 —— //
        private void OnSave()
        {
            SaveRequested?.Invoke(Task);
            CloseAction?.Invoke();
        }
        public event Action<TaskModel>? SaveRequested;

        //—— 直接映射 TaskModel 的属性 ——//

        public string Name
        {
            get => Task.Name;
            set
            {
                Task.Name = value;
                OnPropertyChanged();
                ValidateName();
                UpdateFormValidity();
            }
        }

        // —— 备注 & 字数提示 —— //
        public string Note
        {
            get => Task.Note;
            set
            {
                Task.Note = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoteCharHint));
            }
        }
        public string NoteCharHint => $"{(Note?.Length ?? 0)}/{MaxNoteLength}";

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
                ValidateTimeRange();
                UpdateFormValidity();
                UpdateTimePeriodHint();
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
                UpdateFormValidity();
                UpdateTimePeriodHint();
            }
        }

        public DateTime? EndTimeWrapper
        {
            get => _endTimeWrapper;
            set
            {
                _endTimeWrapper = value;
                if (value.HasValue)
                {
                    // 00:00 或次日00:00 视为 24:00
                    Task.EndTime = (value.Value.TimeOfDay == TimeSpan.Zero)
                        ? TimeSpan.FromHours(24)
                        : value.Value.TimeOfDay;
                }
                else Task.EndTime = null;

                OnPropertyChanged();
                ValidateTimeRange();
                UpdateFormValidity();
                UpdateTimePeriodHint();
            }
        }

        /// <summary>
        /// 校验任务名称非空
        /// </summary>
        private void ValidateName()
        {
            if (string.IsNullOrWhiteSpace(Name))
                NameError = "任务名称不能为空";
            else
                NameError = "";
            OnPropertyChanged(nameof(HasNameError));
        }

        //—— 校验结果、UI 状态属性 ——//

        public bool HasNameError => !string.IsNullOrEmpty(NameError);

        public string NameError
        {
            get => _nameError;
            private set
            {
                if (_nameError != value)
                {
                    _nameError = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TimeError
        {
            get => _timeError;
            private set { _timeError = value; OnPropertyChanged(); }
        }
        public bool HasTimeError => !string.IsNullOrEmpty(TimeError);
        public bool IsTimeValid
        {
            get => _isTimeValid;
            private set { _isTimeValid = value; OnPropertyChanged(); }
        }


        /// <summary>
        /// 全日任务勾上后，时间选择器禁用
        /// </summary>
        public bool IsTimeSelectionEnabled => !IsAllDay;

        private void ValidateTimeRange()
        {
            if (IsAllDay || !Task.StartTime.HasValue || !Task.EndTime.HasValue)
            {
                IsTimeValid = true;
                TimeError = "";
            }
            else
            {
                bool valid = Task.EndTime == TimeSpan.FromHours(24)
                             || Task.StartTime < Task.EndTime;
                IsTimeValid = valid;
                TimeError = valid ? "" : "开始时间不能晚于结束时间";
            }
            OnPropertyChanged(nameof(HasTimeError));
        }

        // —— 时间段建议 —— //
        public string TimePeriodHint
        {
            get => _timePeriodHint;
            private set { _timePeriodHint = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasTimePeriodHint)); }
        }
        public bool HasTimePeriodHint => !string.IsNullOrEmpty(TimePeriodHint);

        private void UpdateTimePeriodHint()
        {
            if (!IsAllDay && Task.StartTime.HasValue && Task.EndTime.HasValue)
            {
                var s = Task.StartTime.Value;
                var e = Task.EndTime.Value == TimeSpan.FromHours(24)
                    ? TimeSpan.FromHours(24) : Task.EndTime.Value;

                if (s >= TimeSpan.FromHours(22)) TimePeriodHint = "夜间：建议轻松活动并及时休息";
                else if (s >= TimeSpan.FromHours(7) && e <= TimeSpan.FromHours(12))
                    TimePeriodHint = "早晨：适合高强度专注任务";
                else if (s >= TimeSpan.FromHours(12) && e <= TimeSpan.FromHours(18))
                    TimePeriodHint = "下午：适合中等难度任务";
                else if (s >= TimeSpan.FromHours(18) && e <= TimeSpan.FromHours(22))
                    TimePeriodHint = "傍晚：适合轻松有趣的事情";
                else
                    TimePeriodHint = "";
            }
            else TimePeriodHint = "";
        }

        // —— 整体表单状态 —— //
        private void UpdateFormValidity()
        {
            IsFormValid = !HasNameError && IsTimeValid;
        }
        public bool IsFormValid
        {
            get => _isFormValid;
            private set { _isFormValid = value; OnPropertyChanged(); SaveCommand.RaiseCanExecuteChanged(); }
        }

        //—— INotifyPropertyChanged ——//

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
