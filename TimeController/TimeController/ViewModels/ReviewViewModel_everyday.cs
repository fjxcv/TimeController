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
    public enum ReviewStatus
    {
        None,
        Postponed,
        Abandoned
    }

    public class TaskItem : INotifyPropertyChanged
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

        private ReviewStatus _status = ReviewStatus.None;
        public ReviewStatus Status
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ReviewViewModel_everyday : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private bool _isEverydayPage = true;


        private DateTime? _selectedDate;
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

        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (_selectedDate != value)
                {
                    _selectedDate = value;
                    OnPropertyChanged(nameof(SelectedDate));
                }
            }
        }

        public ObservableCollection<TaskItem> CompletedTasks { get; set; }
        public ObservableCollection<TaskItem> UncompletedTasks { get; set; }
        public ObservableCollection<string> ReviewReasons { get; set; }

        public ReviewViewModel_everyday(INavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToEverydayCommand = new RelayCommand(_ => { }); // 当前页面，不跳转
            NavigateToEveryweekCommand = new RelayCommand(_ => _navigationService.NavigateTo("Everyweek"));
            IsEverydayPage = true;// 确保为ture
            // 默认选中今天
            SelectedDate = DateTime.Today;

            // 示例数据
            CompletedTasks = new ObservableCollection<TaskItem>
            {
                new TaskItem { Name = "任务 A" },
                new TaskItem { Name = "任务 B" }
            };

            UncompletedTasks = new ObservableCollection<TaskItem>
            {
                new TaskItem { Name = "任务 C" },
                new TaskItem { Name = "任务 D" }
            };

            // 预定义的复盘原因
            ReviewReasons = new ObservableCollection<string>
            {
                "时间安排问题", "主观状态问题", "外部干扰",
                "自主延迟决策", "动机缺失", "不明确"
            };
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
