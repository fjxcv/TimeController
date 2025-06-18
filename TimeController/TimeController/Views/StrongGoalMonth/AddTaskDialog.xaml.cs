using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Windows;
using System.Windows.Media;
using TimeController.ViewModels;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;

namespace TimeController.Views
{
    public partial class AddTaskDialog : Window
    {
        public AddTaskDialogViewModel ViewModel { get; }

        public AddTaskDialog(DateTime? defaultTime = null)
        {
            InitializeComponent();
            ViewModel = new AddTaskDialogViewModel(defaultTime);
            if (defaultTime.HasValue)
                ViewModel.Task.PlannedDate = defaultTime.Value.Date;
            DataContext = ViewModel;
        }

        public Models.TaskModel? ResultTask { get; private set; }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 特殊处理 23:00-24:00 的情况（11 PM到12 PM）
            if (ViewModel.StartTimeWrapper?.TimeOfDay >= new TimeSpan(23, 0, 0))
            {
                // 对于结束时间，如果是当天的00:00或次日的00:00，都视为当天的24:00
                if (ViewModel.EndTimeWrapper?.TimeOfDay == TimeSpan.Zero)
                {
                    // 明确设置为当天的24:00
                    ViewModel.Task.StartTime = ViewModel.StartTimeWrapper?.TimeOfDay;
                    ViewModel.Task.EndTime = TimeSpan.FromHours(24);
                    Debug.WriteLine($"特殊处理11 PM到12 PM时间段: {ViewModel.Task.StartTime} - {ViewModel.Task.EndTime}");
                }
                else
                {
                    // 如果选择了其他结束时间，则使用常规处理
                    ViewModel.Task.StartTime = ViewModel.StartTimeWrapper?.TimeOfDay;
                    ViewModel.Task.EndTime = ViewModel.EndTimeWrapper?.TimeOfDay;
                }
            }
            else
            {
                // 非23:00开始的时间处理
                ViewModel.Task.StartTime = ViewModel.StartTimeWrapper?.TimeOfDay;

                // 仍然需要检查结束时间是否为00:00，视为24:00
                if (ViewModel.EndTimeWrapper?.TimeOfDay == TimeSpan.Zero)
                {
                    ViewModel.Task.EndTime = TimeSpan.FromHours(24);
                }
                else
                {
                    ViewModel.Task.EndTime = ViewModel.EndTimeWrapper?.TimeOfDay;
                }
            }

            var errors = ViewModel.Task.Validate();
            if (errors.Count > 0)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(string.Join("\n", errors), "校验错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 创建副本，防止外部修改ViewModel内部状态
            ResultTask = new Models.TaskModel
            {
                Name = ViewModel.Name,
                Note = ViewModel.Note,
                Type = ViewModel.Type,
                IsAllDay = ViewModel.IsAllDay,
                IsReminderEnabled = ViewModel.IsReminderEnabled,
                StartTime = ViewModel.Task.StartTime,
                EndTime = ViewModel.Task.EndTime,
                PlannedDate = ViewModel.Task.PlannedDate,
                // 其他需要的属性可补充
            };

            DialogResult = true;
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ResultTask = null;
            DialogResult = false;
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typed) return typed;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        //防止Timepicker的x直接关闭窗口
        protected override void OnClosing(CancelEventArgs e)
        {
            if (Keyboard.FocusedElement is DependencyObject focused &&
                FindParent<TimePicker>(focused) != null)
            {
                // 当前焦点在 TimePicker 里，不允许关闭窗口！
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }
    }
}
