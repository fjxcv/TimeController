using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TimeController.Models;

namespace TimeController.ViewModels
{
    public class AddCourseViewModel : INotifyPropertyChanged
    {
        public Course Course { get; } = new Course();
        private DateTime _semesterStartDate;

        public AddCourseViewModel(DateTime semesterStartDate)
        {
            _semesterStartDate = semesterStartDate;

            // 设置默认值
            DayOfWeek = "一"; // 周一
            StartTimeWrapper = DateTime.Today.AddHours(8); // 8:00
            EndTimeWrapper = DateTime.Today.AddHours(9).AddMinutes(30); // 9:30
        }

        public string Name
        {
            get => Course.Name;
            set { Course.Name = value; OnPropertyChanged(); }
        }

        public string DayOfWeek
        {
            get => Course.DayOfWeek;
            set { Course.DayOfWeek = value; OnPropertyChanged(); }
        }

        public string Location
        {
            get => Course.Location;
            set { Course.Location = value; OnPropertyChanged(); }
        }

        public string Teacher
        {
            get => Course.Teacher;
            set { Course.Teacher = value; OnPropertyChanged(); }
        }

        // TimePicker绑定的属性
        public DateTime? StartTimeWrapper
        {
            get => Course.StartTime != TimeSpan.Zero ? DateTime.Today + Course.StartTime : null;
            set
            {
                Course.StartTime = value?.TimeOfDay ?? TimeSpan.Zero;
                OnPropertyChanged();

                if (value.HasValue)
                {
                    var newStart = value.Value.TimeOfDay;
                    if (Course.EndTime <= newStart)
                    {
                        Course.EndTime = newStart.Add(TimeSpan.FromHours(1));
                        OnPropertyChanged(nameof(EndTimeWrapper));
                    }
                }

                ValidateTimeRange();
            }
        }

        public DateTime? EndTimeWrapper
        {
            get => Course.EndTime != TimeSpan.Zero ? DateTime.Today + Course.EndTime : null;
            set
            {
                Course.EndTime = value?.TimeOfDay ?? TimeSpan.Zero;
                OnPropertyChanged();
                ValidateTimeRange();
            }
        }

        private void ValidateTimeRange()
        {
            if (Course.StartTime < Course.EndTime)
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
                }
            }
        }

        private string _timeError;
        public string TimeError
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

