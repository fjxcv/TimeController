using System;
using System.Collections.ObjectModel;
using System.Windows;
using TimeController.Models;
using TimeController.ViewModels;

namespace TimeController.Views.StrongGoalMonth
{
    public partial class TodayTasksReminderDialog : Window
    {
        public TodayTasksReminderDialog(ObservableCollection<TaskModel> tasks)
        {
            InitializeComponent();
            var viewModel = new TodayTasksReminderViewModel(tasks, () => 
            {
                DialogResult = true;
                Close();
            });
            DataContext = viewModel;
        }
    }
} 