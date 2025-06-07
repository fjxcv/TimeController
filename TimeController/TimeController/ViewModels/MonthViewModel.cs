using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Views;

namespace TimeController.ViewModels
{
    /// <summary>
    /// 月份视图的ViewModel，负责日历显示和日期导航逻辑
    /// </summary>
    public class MonthViewModel : INotifyPropertyChanged
    {
        // 年份范围为2024-2026
        private const int MinYear = 2024;
        private const int MaxYear = 2026;

        // 日历日期集合（包含null用于填充空白）?-null
        public ObservableCollection<DateTime?> CalendarDays { get; } = new();
        
        // 所有任务的集合
        public ObservableCollection<TaskModel> AllTasks { get; } = new();

        // 当前年份
        private int _year = DateTime.Today.Year;
        public int Year
        {
            get { return _year; }
            set 
            { 
                _year = value; 
                OnPropertyChanged(); 
                UpdateCalendar(); // 年份变化时更新日历
            }
        }

        // 当前月份
        private int _month = DateTime.Today.Month;
        public int Month
        {
            get => _month;
            set 
            { 
                _month = value; 
                OnPropertyChanged(); 
                UpdateCalendar(); // 月份变化时更新日历
            }
        }

        // 年份显示文本（格式化）
        public string YearText => $"{Year}年";
        
        // 月份显示文本（格式化）
        public string MonthText => $"{Month}月";

        // 命令定义
        public ICommand PrevYearCommand { get; }      // 上一年
        public ICommand NextYearCommand { get; }      // 下一年
        public ICommand PrevMonthCommand { get; }     // 上一月
        public ICommand NextMonthCommand { get; }     // 下一月
        public ICommand ReviewCommand { get; }        // 进入复盘
        public ICommand DateClickCommand { get; }     // 日期点击

        /// <summary>
        /// 构造函数
        /// </summary>
        public MonthViewModel()
        {
            // 初始化所有命令
            PrevYearCommand = new RelayCommand(_ => ChangeYear(-1));
            NextYearCommand = new RelayCommand(_ => ChangeYear(1));
            PrevMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
            NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
            ReviewCommand = new RelayCommand(_ => ShowReview());
            DateClickCommand = new RelayCommand(date => ShowAddTaskDialog((DateTime)date));

            // 初始化日历
            UpdateCalendar();
        }

        /// <summary>
        /// 改变年份
        /// </summary>
        private void ChangeYear(int delta)
        {
            int newYear = Year + delta;
            // 检查年份范围
            if (newYear < MinYear || newYear > MaxYear) return;
            
            Year = newYear;
            OnPropertyChanged(nameof(YearText)); // 通知UI更新年份显示
        }

        /// <summary>
        /// 改变月份（处理跨年情况）
        /// </summary>
        private void ChangeMonth(int delta)
        {
            int newMonth = Month + delta;
            
            // 处理跨年情况
            if (newMonth < 1) // 上一年的12月
            {
                if (Year > MinYear)
                {
                    Year--;
                    newMonth = 12;
                }
                else return; // 已达最小年份
            }
            else if (newMonth > 12) // 下一年的1月
            {
                if (Year < MaxYear)
                {
                    Year++;
                    newMonth = 1;
                }
                else return; // 已达最大年份
            }
            
            Month = newMonth;
            OnPropertyChanged(nameof(MonthText)); // 通知UI更新月份显示
            OnPropertyChanged(nameof(YearText)); // 通知UI更新年份显示
        }

        /// <summary>
        /// 更新日历显示
        /// </summary>
        private void UpdateCalendar()
        {
            CalendarDays.Clear();
            var firstDay = new DateTime(Year, Month, 1);
            int daysInMonth = DateTime.DaysInMonth(Year, Month);
            
            // 第一个日期前需要的空格数
            int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;

            // 空白填充
            for (int i = 0; i < startOffset; i++)
                CalendarDays.Add(null);

            // 添加所有日期卡片
            for (int day = 1; day <= daysInMonth; day++)
                CalendarDays.Add(new DateTime(Year, Month, day));
        }

        /// <summary>
        /// 显示复盘页面
        /// </summary>
        private void ShowReview()
        {
            // TODO: 实现跳转复盘
            System.Windows.MessageBox.Show("跳转复盘页面（待实现）");
        }

        /// <summary>
        /// 显示添加任务表单
        /// </summary>
        private void ShowAddTaskDialog(DateTime date)
        {
            var dialog = new AddTaskDialog(date);
            if (dialog.ShowDialog() == true && dialog.ResultTask != null)
            {
                // 添加新任务到集合
                AllTasks.Add(dialog.ResultTask);
            }
        }

        // INotifyPropertyChanged 实现
        public event PropertyChangedEventHandler? PropertyChanged;
        
        /// <summary>
        /// 属性变更通知方法
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}