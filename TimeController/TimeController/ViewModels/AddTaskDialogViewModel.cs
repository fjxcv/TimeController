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

        public ICommand SaveCommand { get; }

        public TaskModel Task { get; } = new TaskModel();

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
            SaveRequested?.Invoke(Task);
        }


        public string Name
        {
            get => Task.Name;
            set { Task.Name = value; OnPropertyChanged(); }
        }

        private string? _note;
        public string? Note
        {
            get => _note;
            set
            {
                _note = value;
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
                if (Task.StartTime < Task.EndTime)
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
                TimeError = null;
                IsTimeValid = true;
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
