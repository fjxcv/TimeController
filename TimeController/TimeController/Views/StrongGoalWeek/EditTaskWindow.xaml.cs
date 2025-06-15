using System.Windows;
using TimeController.Models;
using TimeController.Services;
using TimeController.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using DevExpress.Data.Browsing;
using DevExpress.Utils.CommonDialogs.Internal;

namespace TimeController.Views
{
    public partial class EditTaskWindow : Window
    {
        public EditTaskWindow(TaskModel task)
        {
            InitializeComponent();
            DataContext = new EditTaskViewModel(task);
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var vm = (EditTaskViewModel)DataContext;
            await App.AppHost.Services.GetRequiredService<ITaskService>().UpdateTaskAsync(vm.Task);
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}