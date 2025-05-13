using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Services;

namespace TimeController.ViewModels
{

    public class ReviewTaskItem : INotifyPropertyChanged
    {
        private DateTime? _postponeDate;
        public DateTime? PostponeDate
        {
            get => _postponeDate;
            set
            {
                _postponeDate = value;
                OnPropertyChanged(nameof(PostponeDate));
            }
        }

        public string Name { get; set; } = string.Empty;

        private MyTaskStatus _status = MyTaskStatus.Pending;
        public MyTaskStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        private string? _reason;
        public string? Reason
        {
            get => _reason;
            set
            {
                if (_reason != value)
                {
                    _reason = value;
                    OnPropertyChanged(nameof(Reason));
                }
            }
        }

        private DateTime? _plannedDate;
        public DateTime? PlannedDate
        {
            get => _plannedDate;
            set
            {
                _plannedDate = value;
                OnPropertyChanged(nameof(PlannedDate));
            }
        }

        private bool _isAllDay;
        public bool IsAllDay
        {
            get => _isAllDay;
            set
            {
                _isAllDay = value;
                OnPropertyChanged(nameof(IsAllDay));
            }
        }

        private DateTime? _startTime;
        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged(nameof(StartTime));
            }
        }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged(nameof(EndTime));
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ReviewViewModel_everyday : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private bool _isEverydayPage = true;
        private bool _isAllDay;
        private DateTime? _startTime;
        private DateTime? _endTime;
        private DateTime? _plannedDate;
        private DateTime? _selectedDate;
        private ObservableCollection<ReviewTaskItem> _pendingTasks;
        private ObservableCollection<string> _reviewReasons;

        public bool IsEverydayPage
        {
            get => _isEverydayPage;
            set
            {
                if (_isEverydayPage != value)
                {
                    _isEverydayPage = value;
                    OnPropertyChanged(nameof(IsEverydayPage));
                }
            }
        }

        public ICommand NavigateToEverydayCommand { get; }
        public ICommand NavigateToEveryweekCommand { get; }
        public ICommand PostponeTaskCommand { get; }
        public ICommand AbandonTaskCommand { get; }
        public ICommand BatchProcessCommand { get; }

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
                    LoadTasksForDate(value ?? DateTime.Today);
                }
            }
        }

        
        public ObservableCollection<TaskModel> CompletedTasks { get; set; }
        public ObservableCollection<TaskModel> UncompletedTasks { get; set; }
        public ObservableCollection<ReviewTaskItem> PendingTasks
        {
            get => _pendingTasks;
            set
            {
                if (_pendingTasks != value)
                {
                    _pendingTasks = value;
                    OnPropertyChanged(nameof(PendingTasks));
                    OnPropertyChanged(nameof(PendingTasksCount));
                }
            }
        }

        public int PendingTasksCount => PendingTasks?.Count ?? 0;

        public ObservableCollection<string> ReviewReasons
        {
            get => _reviewReasons;
            set
            {
                if (_reviewReasons != value)
                {
                    _reviewReasons = value;
                    OnPropertyChanged(nameof(ReviewReasons));
                }
            }
        }

        public ReviewViewModel_everyday(INavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToEverydayCommand = new RelayCommand(_ => { });
            NavigateToEveryweekCommand = new RelayCommand(_ => _navigationService.NavigateTo("Everyweek"));
            PostponeTaskCommand = new RelayCommand<TaskModel>(PostponeTask);
            AbandonTaskCommand = new RelayCommand<TaskModel>(AbandonTask);
            BatchProcessCommand = new RelayCommand<object>(BatchProcess);

            IsEverydayPage = true;
            SelectedDate = DateTime.Today;

            CompletedTasks = new ObservableCollection<TaskModel>();
            UncompletedTasks = new ObservableCollection<TaskModel>();
            PendingTasks = new ObservableCollection<ReviewTaskItem>();


            // 添加一些测试数据
            // 今日已完成任务
            CompletedTasks.Add(new TaskModel
            {
                Name = "完成项目文档",
                IsCompleted = true,
                PlannedDate = DateTime.Today,
                IsAllDay = true
            });
            CompletedTasks.Add(new TaskModel
            {
                Name = "团队会议",
                IsCompleted = true,
                PlannedDate = DateTime.Today,
                IsAllDay = false,
                StartTime = DateTime.Today.AddHours(10),
                EndTime = DateTime.Today.AddHours(11)
            });

            // 今日未完成任务
            UncompletedTasks.Add(new TaskModel
            {
                Name = "代码审查",
                IsCompleted = false,
                PlannedDate = DateTime.Today,
                IsAllDay = false,
                StartTime = DateTime.Today.AddHours(14),
                EndTime = DateTime.Today.AddHours(15)
            });
            UncompletedTasks.Add(new TaskModel
            {
                Name = "准备周报",
                IsCompleted = false,
                PlannedDate = DateTime.Today,
                IsAllDay = true
            });

            // 未处理任务（过期任务）
            PendingTasks.Add(new ReviewTaskItem
            {
                Name = "整理工作笔记",
                Status = MyTaskStatus.Pending,
                PlannedDate = DateTime.Today.AddDays(-3),
                IsAllDay = true
            });

            PendingTasks.Add(new ReviewTaskItem
            {
                Name = "项目进度汇报",
                Status = MyTaskStatus.Pending,
                PlannedDate = DateTime.Today.AddDays(-2),
                IsAllDay = false,
                StartTime = DateTime.Today.AddDays(-2).AddHours(15),
                EndTime = DateTime.Today.AddDays(-2).AddHours(16)
            });
            PendingTasks.Add(new ReviewTaskItem
            {
                Name = "更新项目计划",
                Status = MyTaskStatus.Pending,
                PlannedDate = DateTime.Today.AddDays(-1),
                IsAllDay = true
            });
            PendingTasks.Add(new ReviewTaskItem
            {
                Name = "客户需求分析",
                Status = MyTaskStatus.Pending,
                PlannedDate = DateTime.Today.AddDays(-5),
                IsAllDay = false,
                StartTime = DateTime.Today.AddDays(-5).AddHours(9),
                EndTime = DateTime.Today.AddDays(-5).AddHours(11)
            });


            //监听逻辑
            PendingTasks.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(PendingTasksCount));
            };

            // 初始化复盘原因列表
            ReviewReasons = new ObservableCollection<string>
            {
                "时间安排问题",
                "主观状态问题",
                "外部干扰",
                "自主延迟决策",
                "动机缺失",
                "不明确"
            };

            LoadTasks();
        }

        private void LoadTasks()
        {
            // TODO: 从数据源加载任务
            // 这里需要实现从数据库或文件加载任务数据的逻辑
        }

        private void LoadTasksForDate(DateTime date)
        {
            // TODO: 根据日期加载任务
            // 这里需要实现根据日期加载任务数据的逻辑
        }

        private void PostponeTask(TaskModel task)
        {
            // TODO: 实现推迟任务的逻辑
        }

        private void AbandonTask(TaskModel task)
        {
            // TODO: 实现放弃任务的逻辑
        }

        private void BatchProcess(object parameter)
        {
            // TODO: 实现批量处理任务的逻辑
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}