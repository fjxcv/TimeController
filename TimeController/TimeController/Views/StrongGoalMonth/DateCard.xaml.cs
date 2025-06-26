using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TimeController.Models;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

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
            new PropertyMetadata(new ObservableCollection<TaskModel>(), OnTasksChanged));

        // 是否展开显示所有任务
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded",
            typeof(bool),
            typeof(DateCard),
            new PropertyMetadata(false, OnIsExpandedChanged));

        /// <summary>
        /// 获取或设置卡片显示的日期
        /// </summary>
        public DateTime? Date
        {
            get => (DateTime?)GetValue(DateProperty);
            set => SetValue(DateProperty, value);
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


        /// <summary>
        /// 鼠标左键抬起事件处理
        /// </summary>
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 检查命令是否可用，并传递当前日期作为参数
            if (Command?.CanExecute(Date) == true)
            {
                Command.Execute(Date);  // 执行命令
            }
        }

        private void ToggleExpand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Tasks?.Any() != true)
                {
                    return;
                }

                // 停止事件冒泡，防止触发其他点击事件
                e.Handled = true;

                if (!IsExpanded)
                {
                    // 计算弹出位置
                    CalculatePopupPosition();

                    // 设置动画效果
                    TaskPopup.PopupAnimation = PopupAnimation.Fade;

                    // 打开弹出框并更新状态
                    TaskPopup.IsOpen = true;
                    IsExpanded = true;
                }
                else
                {
                    // 只有通过收缩按钮才能关闭
                    ClosePopup();
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不影响用户体验
                System.Diagnostics.Debug.WriteLine($"ToggleExpand error: {ex.Message}");
                ClosePopup();
            }
        }

        private void CalculatePopupPosition()
        {
            //// 获取卡片在屏幕上的位置
            //var cardPosition = CardBorder.PointToScreen(new Point(0, 0));

            //// 获取主窗口
            //var mainWindow = Window.GetWindow(this);
            //if (mainWindow == null) return;

            //// 计算相对于主窗口的位置
            //var windowPosition = mainWindow.PointToScreen(new Point(0, 0));
            //var relativeX = cardPosition.X - windowPosition.X;
            //var relativeY = cardPosition.Y - windowPosition.Y;

            //// 设置弹出框位置
            //TaskPopup.HorizontalOffset = relativeX;
            //TaskPopup.VerticalOffset = relativeY + CardBorder.ActualHeight + 4; // 4是间距
        }

        private void ClosePopup()
        {
            if (TaskPopup.IsOpen)
            {
                TaskPopup.IsOpen = false;
            }
        }

        private void TaskPopup_Closed(object? sender, EventArgs e)
        {
            // 确保弹出框确实已关闭
            if (!TaskPopup.IsOpen)
            {
                // 更新展开状态
                IsExpanded = false;

                // 可以在这里添加额外的清理逻辑
                System.Diagnostics.Debug.WriteLine("Popup closed via collapse button, IsExpanded set to false");
            }
        }

        // 添加属性变更通知，用于响应 IsExpanded 的变化
        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateCard card)
            {
                // 通知属性变更，触发按钮图标更新
                card.OnPropertyChanged(nameof(IsExpanded));

                // 可以在这里添加额外的状态同步逻辑
                System.Diagnostics.Debug.WriteLine($"IsExpanded changed to: {e.NewValue}");
            }
        }

        // 添加窗口大小改变事件处理
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            // 如果弹出框是打开的，重新计算位置
            if (TaskPopup.IsOpen)
            {
                CalculatePopupPosition();
            }
        }

        // 添加窗口位置改变事件处理
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // 如果弹出框是打开的，重新计算位置
            if (TaskPopup.IsOpen)
            {
                CalculatePopupPosition();
            }
        }

        /// <summary>
        /// 日期属性变更回调方法
        /// </summary>
        private static void OnDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // 更新卡片上显示的文本
            if (d is DateCard card)
            {
                var date = (DateTime?)e.NewValue;
                if (date.HasValue)
                {
                    card.DateText.Text = date.Value.Day.ToString();
                    card.IsToday = date.Value.Date == DateTime.Today;
                }
                else
                {
                    // 传 null 进来，就清空显示，并且不会把 IsToday 置为 true
                    card.DateText.Text = "";
                    card.IsToday = false;
                }
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