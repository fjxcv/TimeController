using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TimeController.Models;
using System.Windows.Input;
using System.Diagnostics;

namespace TimeController.ViewModels
{
    public class AddTaskDialogViewModel : INotifyPropertyChanged
    {

        public RelayCommand SaveCommand { get; }

        public TaskModel Task { get; } = new TaskModel();
        // 添加周次模式相关属性
        private string _weekPattern = "1";
        public string WeekPattern
        {
            get => _weekPattern;
            set
            {
                _weekPattern = value;
                OnPropertyChanged();
                ValidateWeekPattern();
            }
        }

        private bool _isWeekPatternValid = true;
        public bool IsWeekPatternValid
        {
            get => _isWeekPatternValid;
            set
            {
                if (_isWeekPatternValid != value)
                {
                    _isWeekPatternValid = value;
                    OnPropertyChanged();
                    // 更新保存按钮状态
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _weekPatternError;
        public string WeekPatternError
        {
            get => _weekPatternError;
            set
            {
                _weekPatternError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasWeekPatternError));
            }
        }

        public bool HasWeekPatternError => !string.IsNullOrEmpty(WeekPatternError);

        public AddTaskDialogViewModel(DateTime? defaultTime = null)
        {
            if (defaultTime.HasValue)
            {
                Task.StartTime = defaultTime.Value.TimeOfDay;
            }

            Task.EndTime = TimeSpan.FromHours(1); // 默认结束时间

            SaveCommand = new RelayCommand(_ => OnSave(), _ => IsTimeValid);

            // 初始化时更新时间段提示
            UpdateTimePeriodHint();

        }

        public event Action<TaskModel>? SaveRequested;

        private void OnSave()
        {
            // 创建 Task 的副本，避免外部修改 ViewModel 内部状态
            var newTask = new TaskModel
            {
                Name = this.Name,
                Note = this.Note,
                Type = this.Type,
                IsAllDay = this.IsAllDay,
                IsReminderEnabled = this.IsReminderEnabled,
                StartTime = this.Task.StartTime,
                EndTime = this.Task.EndTime,
                PlannedDate = this.Task.PlannedDate,
                // 其他需要的属性
            };
            SaveRequested?.Invoke(newTask);
            // 可选：保存后自动关闭窗口
            CloseAction?.Invoke();
        }

        // 增加关闭窗口的回调
        public Action? CloseAction { get; set; }

        public string Name
        {
            get => Task.Name;
            set
            {
                Task.Name = value;
                OnPropertyChanged();
                // 更新表单有效性
                UpdateFormValidity();
            }
        }

        // 添加时间段提示属性
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
                }
            }
        }

        public bool HasTimePeriodHint => !string.IsNullOrEmpty(TimePeriodHint);


        //根据时间段推荐任务
        private void UpdateTimePeriodHint()
        {
            if (!IsAllDay && Task.StartTime.HasValue && Task.EndTime.HasValue)
            {
                TimeSpan startTime = Task.StartTime.Value;
                TimeSpan endTime = Task.EndTime.Value;
                bool is24Hour = endTime == TimeSpan.FromHours(24);

                // 判断时间段是否在22:00-24:00之间
                if (startTime >= new TimeSpan(22, 0, 0) &&
                    (endTime <= TimeSpan.FromHours(24) || is24Hour))
                {
                    TimePeriodHint = "22:00-24:00为夜间时间段\n建议安排轻松活动并及时休息";
                    return;
                }

                // 判断时间段是否在7:00-12:00之间
                if (startTime >= new TimeSpan(7, 0, 0) && endTime <= new TimeSpan(12, 0, 0))
                {
                    TimePeriodHint = "7:00-12:00为高度集中和强自制力时间段\n推荐进行不感兴趣且消耗精力的事情";
                    Debug.WriteLine($"设置提示: {TimePeriodHint}");
                    return;
                }

                // 判断时间段是否在12:00-18:00之间
                if (startTime >= new TimeSpan(12, 0, 0) && endTime <= new TimeSpan(18, 0, 0))
                {
                    TimePeriodHint = "12:00-18:00为高强度能量和低自制力时间段\n推荐进行感兴趣但又消耗精力的事情";
                    Debug.WriteLine($"设置提示: {TimePeriodHint}");
                    return;
                }

                // 判断时间段是否在18:00-22:00之间
                if (startTime >= new TimeSpan(18, 0, 0) && endTime <= new TimeSpan(22, 0, 0))
                {
                    TimePeriodHint = "18:00-22:00为低能量和低自制力时间段\n推荐进行有趣且不消耗精力的事情";
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
                Debug.WriteLine("清空提示: 全天任务或时间未设置");
            }
        }


        private string? _note;
        public string? Note
        {
            get => Task.Note;
            set
            {
                Task.Note = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NoteCharHint));
            }
        }


        public string NoteCharHint => $"{Note?.Length ?? 0}/200";


        public TaskType Type
        {
            get => Task.Type;
            set { Task.Type = value; OnPropertyChanged(); }
        }

        // 修改IsAllDay属性，添加时间段提示更新
        public bool IsAllDay
        {
            get => Task.IsAllDay;
            set
            {
                Task.IsAllDay = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimeSelectionEnabled));
                UpdateTimePeriodHint(); // 更新时间段提示
            }
        }

        public bool IsReminderEnabled
        {
            get => Task.IsReminderEnabled;
            set { Task.IsReminderEnabled = value; OnPropertyChanged(); }
        }

        // TimePicker绑定的属性
        public DateTime? StartTimeWrapper
        {
            get => Task.StartTime.HasValue ? DateTime.Today + Task.StartTime.Value : null;
            set
            {
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
                    }
                }

                ValidateTimeRange();//开始时间<结束时间
                UpdateTimePeriodHint(); // 更新时间段提示
            }
        }

        // TimePicker绑定的属性
        private void ValidateTimeRange()
        {
            if (Task.StartTime.HasValue && Task.EndTime.HasValue)
            {
                bool isValid;
                TimeSpan startTime = Task.StartTime.Value;
                TimeSpan endTime = Task.EndTime.Value;

                // 特殊处理24:00的情况
                bool is24Hour = endTime == TimeSpan.FromHours(24);

                // 24:00总是大于任何其他时间，包括23:59:59
                if (is24Hour)
                {
                    // 允许任何开始时间与24:00组合
                    isValid = true;
                    Debug.WriteLine($"24:00特殊处理: 允许与任何开始时间组合 {startTime}");
                }
                else
                {
                    // 常规情况：开始时间必须早于结束时间
                    isValid = startTime < endTime;
                    Debug.WriteLine($"常规时间验证: {startTime} < {endTime} = {isValid}");
                }

                IsTimeValid = isValid;
                TimeError = isValid ? null : "开始时间不能晚于结束时间";
            }
            else
            {
                IsTimeValid = true;
                TimeError = null;
            }

            UpdateFormValidity();
        }



        private void ValidateWeekPattern()
        {
            if (string.IsNullOrWhiteSpace(WeekPattern))
            {
                IsWeekPatternValid = false;
                WeekPatternError = "周次不能为空";
            }
            else
            {
                // 检查格式：可以是单个数字、逗号分隔的数字列表或者用-连接的范围
                bool isValid = true;
                string error = null;

                try
                {
                    // 解析周次
                    ParseWeekPattern(WeekPattern);
                }
                catch (Exception ex)
                {
                    isValid = false;
                    error = "周次格式不正确，请使用数字、逗号或连字符，例如：1、1,3,5 或 1-10";
                }

                IsWeekPatternValid = isValid;
                WeekPatternError = error;
            }

            // 更新总体表单有效性，而不是直接修改IsTimeValid
            UpdateFormValidity();
        }

        // 解析周次模式，返回包含所有周次的集合
        public static HashSet<int> ParseWeekPattern(string pattern)
        {
            var weeks = new HashSet<int>();

            if (string.IsNullOrWhiteSpace(pattern))
                return weeks;

            var parts = pattern.Split(',');
            foreach (var part in parts)
            {
                if (part.Contains("-"))
                {
                    var range = part.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                    {
                        for (int i = start; i <= end; i++)
                        {
                            weeks.Add(i);
                        }
                    }
                    else
                    {
                        throw new FormatException("周次范围格式不正确");
                    }
                }
                else if (int.TryParse(part, out int week))
                {
                    weeks.Add(week);
                }
                else
                {
                    throw new FormatException("周次必须是数字");
                }
            }

            return weeks;
        }

        // 更新总体表单有效性
        private void UpdateFormValidity()
        {
            IsFormValid = IsTimeValid && IsWeekPatternValid;
        }

        private bool _isFormValid = true;
        public bool IsFormValid
        {
            get => _isFormValid;
            set
            {
                if (_isFormValid != value)
                {
                    _isFormValid = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool _isTimeValid = true;
        public bool IsTimeValid
        {
            get => _isTimeValid;
            set
            {
                if (_isTimeValid != value)
                {
                    _isTimeValid = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        // 对24:00特殊处理
        // 对24:00和00:00进行区分处理
        public DateTime? EndTimeWrapper
        {
            get
            {
                if (Task.EndTime.HasValue)
                {
                    // 特殊处理24:00情况 - 依然显示为当天，但时间显示为00:00
                    if (Task.EndTime.Value == TimeSpan.FromHours(24))
                    {
                        // 返回当天的00:00，但在UI上理解为24:00
                        return DateTime.Today;
                    }

                    return DateTime.Today + Task.EndTime.Value;
                }
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    // 如果选择了当天的00:00，视为24:00
                    if (value.Value.Date == DateTime.Today &&
                        value.Value.TimeOfDay == TimeSpan.Zero)
                    {
                        Task.EndTime = TimeSpan.FromHours(24); // 设置为24:00
                        Debug.WriteLine($"将当天00:00映射为24:00: {Task.EndTime}");
                    }
                    // 如果选择了次日的00:00，也视为24:00
                    else if (value.Value.Date > DateTime.Today &&
                             value.Value.TimeOfDay == TimeSpan.Zero)
                    {
                        Task.EndTime = TimeSpan.FromHours(24); // 设置为24:00
                        Debug.WriteLine($"将次日00:00映射为24:00: {Task.EndTime}");
                    }
                    else
                    {
                        Task.EndTime = value.Value.TimeOfDay;
                        Debug.WriteLine($"设置普通结束时间: {Task.EndTime}");
                    }
                }
                else
                {
                    Task.EndTime = null;
                    Debug.WriteLine("结束时间设置为null");
                }

                OnPropertyChanged();
                ValidateTimeRange();
                UpdateTimePeriodHint();
            }
        }


        private string? _timeError;
        public string? TimeError
        {
            get => _timeError;
            set
            {
                _timeError = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTimeError));
            }
        }

        public bool HasTimeError => !string.IsNullOrEmpty(TimeError);


        public bool IsTimeSelectionEnabled => !IsAllDay;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}