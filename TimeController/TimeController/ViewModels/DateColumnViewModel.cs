using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace TimeController.ViewModels
{
    public class DateColumnViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private string _weekDayText = "";
        private string _dateText = "";
        private bool _isCurrentMonth;
        private bool _isToday;
        private ObservableCollection<WeekViewModel.TaskBlock> _allDayTasks = new();

        // —— 新增这一行 —— 
        public ICollectionView AllDayTasksView { get; private set; }

        public DateColumnViewModel()
        {
            // 先给 AllDayTasksView 一个“空包装”，防止后面空引用
            AllDayTasksView = CollectionViewSource.GetDefaultView(_allDayTasks);
            _allDayTasks.CollectionChanged += AllDayTasks_CollectionChanged;
            // 初始化一次过滤
            RefreshAllDayTasksView();
        }

        public ObservableCollection<WeekViewModel.TaskBlock> AllDayTasks
        {
            get => _allDayTasks;
            set
            {
                if (_allDayTasks != value)
                {
                    if (_allDayTasks != null)
                        _allDayTasks.CollectionChanged -= AllDayTasks_CollectionChanged;
                    _allDayTasks = value;

                    OnPropertyChanged();

                    // 重新 Wrap 一下新集合
                    AllDayTasksView = CollectionViewSource.GetDefaultView(_allDayTasks);
                    OnPropertyChanged(nameof(AllDayTasksView));

                    if (_allDayTasks != null)
                        _allDayTasks.CollectionChanged += AllDayTasks_CollectionChanged;

                    // 更新按钮显示判断
                    OnPropertyChanged(nameof(ShouldShowMoreButton));

                    // 并且立即刷新一次过滤
                    RefreshAllDayTasksView();
                }
            }
        }

        public bool ShouldShowMoreButton => AllDayTasks?.Count > 2;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                    // 切换展开/折叠后也要刷新过滤
                    RefreshAllDayTasksView();
                }
            }
        }

        // IsToday 属性
        public bool IsToday
        {
            get => _isToday;
            set
            {
                if (_isToday != value)
                {
                    _isToday = value;
                    OnPropertyChanged();
                }
            }
        }


        public int Index { get; set; }
        public string WeekDayText { get => _weekDayText; set { _weekDayText = value; OnPropertyChanged(); } }
        public string DateText { get => _dateText; set { _dateText = value; OnPropertyChanged(); } }
        public bool IsCurrentMonth { get => _isCurrentMonth; set { _isCurrentMonth = value; OnPropertyChanged(); } }

        /// <summary>
        /// 根据 IsExpanded 切换 AllDayTasksView 的过滤器：折叠时只看前 3 条，展开时看全部
        /// </summary>
        public void RefreshAllDayTasksView()
        {
            if (AllDayTasksView == null) return;

            AllDayTasksView.Filter = item =>
            {
                var block = item as WeekViewModel.TaskBlock;
                if (block == null) return false;
                int idx = AllDayTasks.IndexOf(block);
                return idx < 2 || IsExpanded;
            };
            AllDayTasksView.Refresh();
            OnPropertyChanged(nameof(ShouldShowMoreButton));
        }

        private void AllDayTasks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ShouldShowMoreButton));
            RefreshAllDayTasksView();
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
