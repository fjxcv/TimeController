using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TimeController.Models;
using TimeController.ViewModels;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace TimeController.Views.StrongGoalWeek
{
    /// <summary>
    /// WeekView.xaml 的交互逻辑
    /// </summary>
    public partial class WeekView : Page
    {
        private readonly WeekViewModel _viewModel;
        private DateTime? _clickedDate;

        public WeekView()
        {
            InitializeComponent();

            _viewModel = new WeekViewModel();
            DataContext = _viewModel;

            // 初次加载时更新页面日期块
            UpdateDateDisplay(_viewModel.CurrentDate);

            // 当 CurrentDate 改变时更新日期块
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CurrentDate))
                {
                    UpdateDateDisplay(_viewModel.CurrentDate);
                }
            };


        }

        private void ImportSchedule_Click(object sender, RoutedEventArgs e)
        {
            var importWindow = new ImportScheduleWindow
            {
                Owner = Window.GetWindow(this)
            };
            importWindow.ShowDialog();
        }


        private async void WeekContentGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            double timeAxisWidth = 75; // 时间轴宽度
            double dateHeaderHeight = 100; // 日期栏高度

            Point pos = e.GetPosition(WeekContentGrid);

            if (pos.Y < dateHeaderHeight) return;

            double contentX = pos.X - timeAxisWidth;
            if (contentX < 0) return;

            double colWidth = (WeekContentGrid.ActualWidth - timeAxisWidth) / 7;
            int colIndex = (int)(contentX / colWidth);

            // 1. 获取当前周的周一
            var vm = DataContext as WeekViewModel;
            var currentDate = vm?.CurrentDate ?? DateTime.Today;

            DateTime monday = currentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
            {
                monday = monday.AddDays(-1);
            }

            while (monday.DayOfWeek != DayOfWeek.Monday)
            {
                monday = monday.AddDays(-1);
            }

            // 2. 推算当前点击的是哪一天
            DateTime clickedDate = monday.AddDays(colIndex); // 保存到字段中以便打开任务窗口时使用

            //如果再次点击已选中的列，取消
            if (_clickedDate.HasValue && _clickedDate.Value.Date == clickedDate.Date)
            {
                ClearSelection();
                return;
            }


            _clickedDate = clickedDate;
            HighlightColumn(colIndex);

            // 3. 把加号移动到点击点附近
            Point canvasPoint = WeekContentGrid.TranslatePoint(pos, RootCanvas);
            Canvas.SetLeft(AddTaskButton, canvasPoint.X - AddTaskButton.Width / 2);
            Canvas.SetTop(AddTaskButton, canvasPoint.Y - AddTaskButton.Height / 2);

            AddTaskButton.Visibility = Visibility.Visible;

            //点击时显示日期的悬浮提示
            ToolTip toolTip = new ToolTip
            {
                Content = _clickedDate?.ToString("M月d日（dddd）") ?? "",
                Background = Brushes.LightYellow,
                Foreground = Brushes.DarkSlateGray,
                FontSize = 14,
                Padding = new Thickness(8),
                Placement = PlacementMode.Top,
                PlacementTarget = AddTaskButton,
                IsOpen = true
            };


            // 设置 ToolTip 到按钮
            AddTaskButton.ToolTip = toolTip;

            //自动关闭提示
            await Task.Delay(1000);
            toolTip.IsOpen = false;


        }

        //取消选中的方法
        private void ClearSelection()
        {
            for (int i = 0; i < 7; i++)
            {
                var dateCol = FindName($"DateColumn{i}") as Border;
                if (dateCol != null)
                    dateCol.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // 顶部还原

                var taskCol = FindName($"TaskColumn{i}") as Border;
                if (taskCol != null)
                    taskCol.Background = Brushes.Transparent; // 任务区还原
            }

            AddTaskButton.Visibility = Visibility.Collapsed;
            _clickedDate = null; // 清除选择的日期（你要先把 _clickedDate 声明为 Nullable）
        }



        //高亮
        private void HighlightColumn(int index)
        {
            for (int i = 0; i < 7; i++)
            {
                var dateCol = FindName($"DateColumn{i}") as Border;
                if (dateCol != null)
                    dateCol.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // 顶部默认色

                var taskCol = FindName($"TaskColumn{i}") as Border;
                if (taskCol != null)
                    taskCol.Background = Brushes.Transparent; // 任务区域默认透明
            }

            var selectedDateCol = FindName($"DateColumn{index}") as Border;
            if (selectedDateCol != null)
                selectedDateCol.Background = new SolidColorBrush(Color.FromRgb(210, 210, 210)); // 顶部高亮

            var selectedTaskCol = FindName($"TaskColumn{index}") as Border;
            if (selectedTaskCol != null)
                selectedTaskCol.Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)); // 任务区高亮
        }


        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddTaskDialog(_clickedDate)
            {
                Owner = Window.GetWindow(this) //居中当前窗口
            };
            

            if (dialog.ShowDialog() == true && dialog.ResultTask != null && !string.IsNullOrWhiteSpace(dialog.ResultTask.Name))
            {
                var task = dialog.ResultTask;

                // 添加到当前视图的任务列表中
                _viewModel.Tasks.Add(task);
            }

            AddTaskButton.Visibility = Visibility.Collapsed;
        }



        /// <summary>
        /// 根据当前日期更新上方 7 天的显示（DateTextBlock0 ~ 6）
        /// </summary>
        private void UpdateDateDisplay(DateTime referenceDate)
        {
            // 获取该周周一
            DateTime monday = referenceDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            for (int i = 0; i < 7; i++)
            {
                DateTime currentDay = monday.AddDays(i);
                if (FindName($"DateTextBlock{i}") is TextBlock textBlock)
                {
                    textBlock.Text = currentDay.Day.ToString();
                }
            }
        }
    }

}
