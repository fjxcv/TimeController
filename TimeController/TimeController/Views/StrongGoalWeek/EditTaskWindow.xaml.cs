using System.Windows;
using TimeController.Models;
using TimeController.Services;
using TimeController.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace TimeController.Views
{
    public partial class EditTaskWindow : Window
    {
        private readonly EditTaskViewModel _vm;
        private readonly ITaskService _taskService;

        public EditTaskWindow(TaskModel task)
        {
            InitializeComponent();

            // 拿到 ITaskService 实例
            _taskService = App.AppHost.Services.GetRequiredService<ITaskService>();

            // 创建 VM 并设置回调
            _vm = new EditTaskViewModel(task);

            // 当 VM 校验通过并触发保存时，真正去调用服务并关闭窗口
            _vm.SaveRequested += async updatedTask =>
            {
                await _taskService.UpdateTaskAsync(updatedTask);
                // 如果你想让外面 ShowDialog() 拿到 true:
                this.DialogResult = true;
                this.Close();
            };
            // VM 内部调用 CloseAction() 时，也要关窗
            _vm.CloseAction = () =>
            {
                this.Close();
            };

            DataContext = _vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 由 VM 来执行保存流程：先校验、弹窗、再触发 SaveRequested
            _vm.SaveCommand.Execute(null);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
