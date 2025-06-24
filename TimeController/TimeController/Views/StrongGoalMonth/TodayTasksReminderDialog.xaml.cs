using System;
using System.Collections.ObjectModel;
using System.Windows;
using TimeController.Models;
using TimeController.ViewModels;

namespace TimeController.Views.StrongGoalMonth
{
    public partial class TodayTasksReminderDialog : Window
    {
        private readonly TodayTasksReminderViewModel _viewModel;

        public TodayTasksReminderDialog(ObservableCollection<TaskModel> tasks)
        {
            InitializeComponent();
            _viewModel = new TodayTasksReminderViewModel(tasks, () =>
            {
                DialogResult = true;
                Close();
            });
            DataContext = _viewModel;

            // 窗口关闭时释放定时器，避免后台占用
            Closed += (_, __) => _viewModel.Dispose();
        }
    }
} 