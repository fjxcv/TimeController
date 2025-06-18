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
            WeekPattern = "1"; // 默认只在第一周显示
        }

        // 课程名称相关属性
        public string Name
        {
            get => Course.Name;
            set
            {
                Course.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFormValid)); // 添加这行
            }
        }

        // 课程时间范围相关属性
        public string DayOfWeek
        {
            get => Course.DayOfWeek;
            set { Course.DayOfWeek = value; OnPropertyChanged(); }
        }

        // 课程地点相关属性
        public string Location
        {
            get => Course.Location;
            set { Course.Location = value; OnPropertyChanged(); }
        }

        // 课程教师相关属性
        public string Teacher
        {
            get => Course.Teacher;
            set { Course.Teacher = value; OnPropertyChanged(); }
        }

        // 周次模式相关属性
        public string WeekPattern
        {
            get => Course.WeekPattern;
            set
            {
                Course.WeekPattern = value;
                OnPropertyChanged();
                ValidateWeekPattern();
            }
        }

        /// <summary>
        /// 表示表单是否有效的属性，用于绑定提交按钮的启用状态
        /// </summary>
        public bool IsFormValid
        {
            get
            {
                // 表单有效需要满足：
                // 1. 课程名称不为空
                // 2. 时间范围有效
                // 3. 周次格式有效
                return !string.IsNullOrWhiteSpace(Name) && IsTimeValid && IsWeekPatternValid;
            }
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

        // TimePicker绑定的属性
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

        // WeekPattern绑定的属性
        private void ValidateTimeRange()
        {
            if (Course.StartTime < Course.EndTime)
            {
                IsTimeValid = true && IsWeekPatternValid;
                TimeError = null;
            }
            else
            {
                IsTimeValid = false;
                TimeError = "开始时间不能晚于结束时间";
            }
        }

        // WeekPattern绑定的属性
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

            // 更新总体验证状态
            IsTimeValid = IsTimeValid && IsWeekPatternValid;
        }

        // 解析周次模式，返回包含所有周次的集合
        public static HashSet<int> ParseWeekPattern(string pattern)
        {
            var weeks = new HashSet<int>();

            if (string.IsNullOrWhiteSpace(pattern))
                return weeks;

            var parts = pattern.Split(new char[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries);
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
                    OnPropertyChanged(nameof(IsFormValid));
                }
            }
        }

        // 对24:00特殊处理
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
                    OnPropertyChanged(nameof(IsFormValid));
                }
            }
        }

        // 时间范围相关属性
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

        // 周次模式相关属性
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

        // 时间是否有错误
        public bool HasTimeError => !string.IsNullOrEmpty(TimeError);

        // 周次模式是否有错误
        public bool HasWeekPatternError => !string.IsNullOrEmpty(WeekPatternError);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

