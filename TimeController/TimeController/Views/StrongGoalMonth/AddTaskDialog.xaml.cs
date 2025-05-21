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
            // 赋值时间
            ViewModel.Task.StartTime = ViewModel.StartTimeWrapper?.TimeOfDay;
            ViewModel.Task.EndTime = ViewModel.EndTimeWrapper?.TimeOfDay;

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