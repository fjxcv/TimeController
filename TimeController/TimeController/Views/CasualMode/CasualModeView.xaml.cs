using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TimeController.ViewModels;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;


namespace TimeController.Views.CasualMode
{
    /// <summary>
    /// CasualModeView.xaml 的交互逻辑
    /// </summary>
    public partial class CasualModeView : Page
    {
        public CasualModeView()
        {
            InitializeComponent();
            _viewModel = new CasualModeViewModel();
            DataContext = _viewModel;
        }

        private readonly CasualModeViewModel _viewModel;

        private void NewTaskTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox textBox && textBox.Tag is ModuleViewModel module)
                {
                    var viewModel = DataContext as CasualModeViewModel;
                    if (viewModel != null && !string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        viewModel.AddTask(module, textBox.Text.Trim());
                        textBox.Text = string.Empty;
                        module.IsInputVisible = false;
                    }
                }
            }
        }

        private void SelfNourishBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[0].IsInputVisible = true;
            }
        }

        private void SelfNourishInputCancel_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[0].IsInputVisible = false;
                viewModel.Modules[0].NewTaskText = string.Empty;
            }
        }

        //private void ToggleTask(TaskModel task)
        //{
        //    task.IsCompleted = !task.IsCompleted;
        //    // 只对当前模块排序
        //    var module = _viewModel.Modules.FirstOrDefault(m => m.Tasks.Contains(task));
        //    if (module != null)
        //    {
        //        var sorted = module.Tasks.OrderBy(t => t.IsCompleted).ToList();
        //        module.Tasks.Clear();
        //        foreach (var t in sorted)
        //            module.Tasks.Add(t);
        //    }
        //    _viewModel.UpdateProgress();
        //}
    }
}
