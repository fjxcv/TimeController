using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
using TimeController.Models;

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
            DataContext = new CasualModeViewModel();
        }



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

        private void CreateExpressionBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[1].IsInputVisible = true;
            }
        }

        private void CreateExpressionInputCancel_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[1].IsInputVisible = false;
                viewModel.Modules[1].NewTaskText = string.Empty;
            }
        }

        private void LifeChoresBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[2].IsInputVisible = true;
            }
        }

        private void LifeChoresInputCancel_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[2].IsInputVisible = false;
                viewModel.Modules[2].NewTaskText = string.Empty;
            }
        }

        private void InterpersonalConnectionBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[3].IsInputVisible = true;
            }
        }

        private void InterpersonalConnectionInputCancel_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[3].IsInputVisible = false;
                viewModel.Modules[3].NewTaskText = string.Empty;
            }
        }

        private void LongTermMemoInputCancel_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[4].IsInputVisible = false;
                viewModel.Modules[4].NewTaskText = string.Empty;
            }
        }
        private void LongTermMemoBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as CasualModeViewModel;
            if (viewModel != null)
            {
                viewModel.Modules[4].IsInputVisible = true;
            }
        }
        // 转换器代码（添加到项目任意位置）
        public class InverseBooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return (value is bool boolValue && boolValue) ? Visibility.Collapsed : Visibility.Visible;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private void TaskNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && sender is TextBox textBox)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                var task = textBox.DataContext as TaskModel;
                task.IsEditing = false;
            }
        }
        private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element &&
                element.DataContext is TaskModel task)
            {
                task.IsEditing = true;
            }
        }
    }
}
