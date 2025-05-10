using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.VisualElements;
using System.Collections.ObjectModel;
using TimeController.Helpers;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace TimeController.ViewModels
{
    public class ReviewViewModel_everyweek : INotifyPropertyChanged
    {
        public string[] WeekDays { get; set; } = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };

        public string Card1Text => "本周共设了 5 个任务，仅完成 2 个，或许可以试试「轻装一天」策略？";
        public string Card2Text => "任务【XXX】已连续推迟三次，是否需要重新评估优先级或者计划？";
        public string Card3Text => "本周完成了 4 个任务，是否不适合有新目标了？调整负载或效率试试看？";
        public string Card4Text => "本周任务有多次放弃，要不要看看影响的自我激励因素？";
        public string Card5Text => "本周完成率 90%，靠稳！下周目标可以自由安排一份奖励～";
        public string Card6Text => "......"; // 自行填充更多分析逻辑



        private DateTime _currentDate = DateTime.Now;
        private readonly INavigationService _navigationService;
        private bool _isEverydayPage = false;

        //第x月 第x周
        public string CurrentMonthText => $"{_currentDate:MMMM}"; // "四月"
        public string CurrentWeekText => $"第{GetWeekOfMonth(_currentDate)}周";

        public ICommand PreviousWeekCommand => new RelayCommand(_ => ChangeWeek(-1));
        public ICommand NextWeekCommand => new RelayCommand(_ => ChangeWeek(1));

        private void ChangeWeek(int direction)
        {
            _currentDate = _currentDate.AddDays(7 * direction);
            OnPropertyChanged(nameof(CurrentMonthText));
            OnPropertyChanged(nameof(CurrentWeekText));
        }

        private int GetWeekOfMonth(DateTime date)
        {
            var first = new DateTime(date.Year, date.Month, 1);
            var culture = CultureInfo.CurrentCulture;
            int week = culture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday)
                      - culture.Calendar.GetWeekOfYear(first, CalendarWeekRule.FirstDay, DayOfWeek.Monday) + 1;
            return week < 1 ? 1 : week;
        }



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


        // 折线图数据
        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }
        public LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint TooltipTextPaint { get; set; }

        public ReviewViewModel_everyweek(INavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToEverydayCommand = new RelayCommand(_ => _navigationService.NavigateTo("Everyday"));
            NavigateToEveryweekCommand = new RelayCommand(_ => { }); // 当前页面，不跳转
            IsEverydayPage = false;  // 确保设置为false

            // 初始化折线图
            InitializeChart();

            //能滚动的静态任务，后面要改
            SkippedTasks = new ObservableCollection<SkippedTask>
            {
                new SkippedTask { Name = "整理衣柜", Status = "推迟" },
                new SkippedTask { Name = "整理衣柜2", Status = "放弃" },
                new SkippedTask { Name = "整理衣柜3", Status = "推迟" },
                new SkippedTask { Name = "收纳小屋", Status = "放弃" },
                new SkippedTask { Name = "写周报", Status = "推迟" },
                new SkippedTask { Name = "洗衣服", Status = "放弃" },
                new SkippedTask { Name = "买菜", Status = "推迟" },
                new SkippedTask { Name = "运动", Status = "放弃" },
                new SkippedTask { Name = "读书", Status = "推迟" },
                new SkippedTask { Name = "写代码", Status = "放弃" }
            };


            TooltipTextPaint = new LiveChartsCore.SkiaSharpView.Painting.SolidColorPaint(SKColors.Black)
            {
                SKTypeface = SKTypeface.FromFamilyName("微软雅黑"),
                Color = SKColors.Black,
                StrokeThickness = 1
            };
        }


        // 初始化折线图
        private void InitializeChart()
        {
            Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = new double[] { 2, 1, 3, 5, 3, 4, 6 },
                Fill = null,
                GeometrySize = 15,
                Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2)
            }
        };

            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new List<string> { "周一", "周二", "周三", "周四", "周五", "周六", "周日" },
                    LabelsPaint = new SolidColorPaint(new SKColor(30, 30, 30)) { SKTypeface = SKTypeface.FromFamilyName("微软雅黑") },
                    TextSize = 18,
                    LabelsRotation = 0,
                    Padding = new LiveChartsCore.Drawing.Padding(10)
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    MaxLimit = 7, // 你可以根据最大任务数动态设置
                    LabelsPaint = new SolidColorPaint(new SKColor(30, 30, 30)) { SKTypeface = SKTypeface.FromFamilyName("微软雅黑") },
                    TextSize = 18,
                    Padding = new LiveChartsCore.Drawing.Padding(10)
                }
            };
        }

        //推迟和放弃的任务
        public ObservableCollection<SkippedTask> SkippedTasks { get; set; } //这里后面要改，检测到推迟和放弃的任务就要添加到这个集合里

        public class SkippedTask
        {
            public string Name { get; set; }
            public string Status { get; set; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
