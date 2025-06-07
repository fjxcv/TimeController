using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TimeController.Views.StrongGoalMonth
{
    
    public partial class DateCard : UserControl
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

        // 新增IsExpanded依赖属性
        public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
            "IsExpanded",
            typeof(bool),
            typeof(DateCard),
            new PropertyMetadata(false, OnIsExpandedChanged));

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

        public bool IsExpanded
        {
            get => (bool)GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
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

        /// <summary>
        /// 展开状态变更回调方法
        /// </summary>
        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DateCard card)
            {
                // 触发展开/收缩动画
                card.UpdateExpandedState();
            }
        }

        /// <summary>
        /// 更新展开状态
        /// </summary>
        private void UpdateExpandedState()
        {
            // 这里可以添加展开/收缩的动画效果
            if (IsExpanded)
            {
                // 展开状态
                this.Height = 300; // 展开时的高度
            }
            else
            {
                // 收缩状态
                this.Height = 150; // 收缩时的高度
            }
        }
    }
}