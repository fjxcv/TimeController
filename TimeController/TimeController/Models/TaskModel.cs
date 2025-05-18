using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace TimeController.Models
{
    public enum MyTaskStatus
    {
        Pending,
        Completed,
        Postponed,
        Abandoned
    }

    public enum TaskMode
    {
        Casual,
        Strong
    }

    public enum TaskType
    {
        未分类,
        学习学业,
        项目实践任务,
        日常任务,
        自我提升,
        其它
    }

    public class TaskModel : INotifyPropertyChanged
    {
        public int Id { get; set; }

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        private string? _note;
        public string? Note
        {
            get => _note;
            set { _note = value; OnPropertyChanged(nameof(Note)); }
        }

        public TaskType Type { get; set; } = TaskType.未分类;

        public TaskMode Mode { get; set; }

        private DateTime _plannedDate = DateTime.Today;
        public DateTime PlannedDate
        {
            get => _plannedDate;
            set { _plannedDate = value; OnPropertyChanged(nameof(PlannedDate)); }
        }

        private bool _isAllDay;
        public bool IsAllDay
        {
            get => _isAllDay;
            set { _isAllDay = value; OnPropertyChanged(nameof(IsAllDay)); }
        }

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }


        private MyTaskStatus _status = MyTaskStatus.Pending;
        public MyTaskStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private string? _reason;
        public string? Reason
        {
            get => _reason;
            set { _reason = value; OnPropertyChanged(nameof(Reason)); }
        }

        private DateTime? _postponeDate;
        public DateTime? PostponeDate
        {
            get => _postponeDate;
            set { _postponeDate = value; OnPropertyChanged(nameof(PostponeDate)); }
        }

        public string? Category { get; set; } // 仅咸鱼模式使用
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(RequiresSort));

                    if (value)
                        Status = MyTaskStatus.Completed;
                }
            }
        }

        private bool _isReminderEnabled;
        public bool IsReminderEnabled
        {
            get => _isReminderEnabled;
            set
            {
                if (_isReminderEnabled != value)
                {
                    _isReminderEnabled = value;
                    OnPropertyChanged(nameof(IsReminderEnabled));
                }
            }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
                }
            }
        }

        public bool RequiresSort => true;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// 验证任务字段合法性
        /// </summary>
        /// <returns>错误信息列表</returns>
        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("任务名称不能为空");

            if (Name?.Length > 10)
                errors.Add("任务名称不能超过10个字符");

            if (Note?.Length > 20)
                errors.Add("任务备注不能超过20个字符");

            if (!IsAllDay && StartTime.HasValue && EndTime.HasValue && StartTime > EndTime)
                errors.Add("开始时间不能晚于结束时间");

            return errors;
        }
    }
}
