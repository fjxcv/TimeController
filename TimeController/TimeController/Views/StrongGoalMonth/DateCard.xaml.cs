using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TimeController.Models;
using System.Windows.Controls.Primitives;

namespace TimeController.Views.StrongGoalMonth
{
    
    public partial class DateCard : UserControl, INotifyPropertyChanged
    {
        // 定义Date依赖属性，用于绑定日期数据
        public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
            "Date",                         // 属性名称
            typeof(DateTime),               // 属性类型
            typeof(DateCard),               // 所属控件类型
            new PropertyMetadata(DateTime.Now, OnDateChanged)); // 默认值和变更回调

        // 定义Command依赖属性，用于绑定点击命令
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command",                      // 属性名称
            typeof(ICommand),               // 属性类型(ICommand接口)
            typeof(DateCard),               // 所属控件类型
            new PropertyMetadata(null));    // 默认值null


        // 新增IsToday依赖属性
        public static readonly DependencyProperty IsTodayProperty = DependencyProperty.Register(
            "IsToday",
            typeof(bool),
            typeof(DateCard),
            new PropertyMetadata(false));

        // 任务集合
        public static readonly DependencyProperty TasksProperty = DependencyProperty.Register(
            "Tasks",
            typeof(ObservableCollection<TaskModel>),
            typeof(DateCard),
            new PropertyMetadata(null, OnTasksChanged));

        // 是否展开显示所有任务
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded",
            typeof(bool),
            typeof(DateCard),
            new PropertyMetadata(false, OnExpandedChanged));

        /// <summary>
        /// 获取或设置卡片显示的日期
        /// </summary>
        public DateTime Date
        {
            get => (DateTime)GetValue(DateProperty);   // 从依赖属性获取值
            set => SetValue(DateProperty, value);       // 设置依赖属性的值
        }

        /// <summary>
        /// 获取或设置点击卡片时执行的命令
        /// </summary>
        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty); // 从依赖属性获取命令
            set => SetValue(CommandProperty, value);     // 设置依赖属性的命令
        }

        public bool IsToday
        {
            get => (bool)GetValue(IsTodayProperty);
            set => SetValue(IsTodayProperty, value);
        }

        public ObservableCollection<TaskModel>? Tasks
        {
            get => (ObservableCollection<TaskModel>?)GetValue(TasksProperty);
            set => SetValue(TasksProperty, value);
        }

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        private ObservableCollection<TaskModel>? _displayedTasks;
        public ObservableCollection<TaskModel>? DisplayedTasks
        {
            get => _displayedTasks;
            set { _displayedTasks = value; OnPropertyChanged(); }
        }

        private bool _hasMoreTasks;
        public bool HasMoreTasks
        {
            get => _hasMoreTasks;
            set { _hasMoreTasks = value; OnPropertyChanged(); }
        }



        /// <summary>
        /// 构造函数
        /// </summary>
        public DateCard()
        {
            InitializeComponent();
            // 注册鼠标左键抬起事件处理程序
            //this.MouseLeftButtonUp += Border_MouseLeftButtonUp;
        }

        private static void OnTasksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateCard card)
            {
                if (e.OldValue is ObservableCollection<TaskModel> oldCollection)
                    oldCollection.CollectionChanged -= card.Tasks_CollectionChanged;
                if (e.NewValue is ObservableCollection<TaskModel> newCollection)
                    newCollection.CollectionChanged += card.Tasks_CollectionChanged;

                card.UpdateDisplayedTasks();
            }
        }

        private void Tasks_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateDisplayedTasks();
        }

        private static void OnExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 改成popup了，暂时没删
        }

        /// <summary>
        /// 鼠标左键抬起事件处理
        /// </summary>
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 切换展开状态
            IsExpanded = !IsExpanded;

            // 检查命令是否可用，并传递当前日期作为参数
            if (Command?.CanExecute(Date) == true)
            {
                Command.Execute(Date);  // 执行命令
            }
        }

        private void ToggleExpand(object sender, RoutedEventArgs e)
        {
            IsExpanded = !IsExpanded;
            TaskPopup.IsOpen = IsExpanded;
        }

        private void TaskPopup_Closed(object? sender, EventArgs e)
        {
            IsExpanded = false;
        }

        /// <summary>
        /// 日期属性变更回调方法
        /// </summary>
        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 更新卡片上显示的文本
            if (d is DateCard card)
            {
                 var date = (DateTime)e.NewValue;
                card.DateText.Text = date.Day.ToString();
                card.IsToday = DateTime.Today == date.Date; // 自动更新IsToday状态
            }
        }

        private void UpdateDisplayedTasks()
        {
            if (Tasks == null)
            {
                DisplayedTasks = null;
                HasMoreTasks = false;
                return;
            }

            var sorted = new ObservableCollection<TaskModel>(System.Linq.Enumerable.OrderBy(Tasks, t => t.StartTime ?? TimeSpan.Zero));
            if (sorted.Count <= 3)
            {
                DisplayedTasks = sorted;
                HasMoreTasks = false;
            }
            else
            {
                DisplayedTasks = new ObservableCollection<TaskModel>(System.Linq.Enumerable.Take(sorted, 3));
                HasMoreTasks = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}