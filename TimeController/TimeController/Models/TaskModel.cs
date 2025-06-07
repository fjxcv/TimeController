

//Id = table.Column<int>(type: "INTEGER", nullable: false)              任务的id
//Name = table.Column<string>(type: "TEXT", nullable: false),           任务名称
//Note = table.Column<string>(type: "TEXT", nullable: true),            任务备注（强管理）
//Type = table.Column<string>(type: "TEXT", nullable: true),            任务类型（强管理）
//Mode = table.Column<string>(type: "TEXT", nullable: false),           咸鱼or强管理模式
//PlannedDate = table.Column<DateTime>(type: "TEXT", nullable: false),  任务所在日期（强管理）
//IsAllDay = table.Column<bool>(type: "INTEGER", nullable: false),      是否为全天任务（强管理）
//StartTime = table.Column<DateTime>(type: "TEXT", nullable: true),     开始时间（强管理）
//EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),       结束时间（强管理）
//Status = table.Column<string>(type: "TEXT", nullable: false),         任务状态（强管理）
//Reason = table.Column<string>(type: "TEXT", nullable: true),          推迟和放弃原因（强管理）
//PostponeDate = table.Column<DateTime>(type: "TEXT", nullable: true),  推迟的日期（强管理）
//PostponedAt = table.Column<DateTime>(type: "TEXT", nullable: true),   每次推迟的时间（强管理）
//AbandonedAt = table.Column<DateTime>(type: "TEXT", nullable: true),   放弃的时间（强管理）
//Category = table.Column<string>(type: "TEXT", nullable: true),        分类（咸鱼模式）
//CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),    创建时间（咸鱼模式）
//IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),   是否完成（咸鱼模式，疑似冗余）
//IsReminderEnabled = table.Column<bool>(type: "INTEGER", nullable: false),是否提醒（强管理）
//IsEditing = table.Column<bool>(type: "INTEGER", nullable: false)         是否正在编辑（咸鱼模式，疑似冗余）
//IsSelected = table.Column<bool>(type: "INTEGER", nullable: false),        是否选中（咸鱼模式，疑似冗余）
//PostponedCount = table.Column<int>(type: "INTEGER", nullable: false)      推迟次数（强管理）





using System;
using System.ComponentModel.DataAnnotations.Schema;
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

    public partial class TaskModel : INotifyPropertyChanged
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

        //为了周复盘的需要，记录推迟和放弃的时间
        public DateTime? PostponedAt { get; set; }    // 记录最后一次推迟的时间
        public DateTime? AbandonedAt { get; set; }    // 记录放弃的时间


        // 这个属性不映射到数据库
        [NotMapped]
        public string StatusShownText
        {
            get
            {
                // 推迟过=》已推迟，放弃=》已放弃
                if (AbandonedAt.HasValue) return "已放弃";
                if (PostponedAt.HasValue && !AbandonedAt.HasValue) return "已推迟";
                return "待处理";
            }
        }


        public void MarkPostponed(DateTime when)
        {
            PostponedAt = when;
            OnPropertyChanged(nameof(PostponedAt));
            OnPropertyChanged(nameof(StatusShownText));
        }

        public void MarkAbandoned(DateTime when)
        {
            AbandonedAt = when;
            OnPropertyChanged(nameof(AbandonedAt));
            OnPropertyChanged(nameof(StatusShownText));
        }

        public string? Category { get; set; }

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

        public string StatusText => Status switch
        {
            MyTaskStatus.Postponed => "已推迟",
            MyTaskStatus.Abandoned => "已放弃",
            MyTaskStatus.Completed => "已完成",
            MyTaskStatus.Pending => "待处理",
            _ => "未知"
        };


        public int PostponedCount { get; set; } // 非数据库字段，用于复盘卡片
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
