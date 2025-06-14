using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TimeController.Models;
using System.Windows.Input;

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

        public bool IsAllDay
        {
            get => Task.IsAllDay;
            set
            {
                Task.IsAllDay = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimeSelectionEnabled));
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
                    if (!Task.EndTime.HasValue || Task.EndTime <= newStart)
                    {
                        Task.EndTime = newStart.Add(TimeSpan.FromHours(1));
                        OnPropertyChanged(nameof(EndTimeWrapper));
                    }
                }

                ValidateTimeRange();//开始时间<结束时间
            }
        }
        private void ValidateTimeRange()
        {
            if (Task.StartTime.HasValue && Task.EndTime.HasValue)
            {
                if (Task.StartTime.Value < Task.EndTime.Value)
                {
                    IsTimeValid = true;
                    TimeError = null;
                }
                else
                {
                    IsTimeValid = false;
                    TimeError = "开始时间不能晚于结束时间";
                }
            }
            else
            {
                // 处理一个或两个时间为空的情况
                IsTimeValid = true;
                TimeError = null;
            }

            // 更新总体表单有效性
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



        public DateTime? EndTimeWrapper
        {
            get => Task.EndTime.HasValue ? DateTime.Today + Task.EndTime.Value : null;
            set
            {
                Task.EndTime = value?.TimeOfDay;
                OnPropertyChanged();
                ValidateTimeRange();//开始时间<结束时间
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