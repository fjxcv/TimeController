using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using TimeController.Models;

namespace TimeController.ViewModels
{
    public class TodayTasksReminderViewModel
    {
        public ObservableCollection<TaskModel> Tasks { get; }
        public ICommand CloseCommand { get; }

        public TodayTasksReminderViewModel(ObservableCollection<TaskModel> tasks, Action closeAction)
        {
            Tasks = tasks;
            CloseCommand = new RelayCommand(_ => closeAction());
        }
    }
} 