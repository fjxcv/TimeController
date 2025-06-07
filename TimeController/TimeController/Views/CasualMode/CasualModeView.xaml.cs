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
using System.ComponentModel;
using System.Windows.Threading;

namespace TimeController.Views.CasualMode
{
    /// <summary>
    /// CasualModeView.xaml 的交互逻辑
    /// </summary>
    public partial class CasualModeView : Page

    {
        private void RewardPopup_Opened(object? sender, EventArgs e)
        {
            // 在弹窗完全打开后，显式设置焦点到奖励任务输入框
            RewardTaskInput.Focus();
        }
        // Store the currently edited TextBox and its TaskModel to handle LostFocus
        private TextBox? _currentEditingTextBox;
        private TaskModel? _currentEditingTaskModel;

        public CasualModeView()
        {
            InitializeComponent();
            DataContext = new CasualModeViewModel();
            RewardPopup.Opened += RewardPopup_Opened;
            // 订阅ViewModel的属性变化，处理View层的UI操作
            if (DataContext is CasualModeViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
                // 订阅Modules集合的变化，以便为每个模块添加属性变化监听
                vm.Modules.CollectionChanged += Modules_CollectionChanged;
                // 为初始加载的模块订阅属性变化
                foreach (var module in vm.Modules)
                {
                    module.PropertyChanged += Module_PropertyChanged;
                }
            }
        }

        private void Modules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ModuleViewModel module in e.NewItems)
                {
                    module.PropertyChanged += Module_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (ModuleViewModel module in e.OldItems)
                {
                    module.PropertyChanged -= Module_PropertyChanged;
                }
            }
        }

        private void Module_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

        }

        // 监听ViewModel的属性变化
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is CasualModeViewModel vm)
            {
                if (e.PropertyName == nameof(CasualModeViewModel.IsRewardPopupOpen))
                {

                }
                else if (e.PropertyName == nameof(CasualModeViewModel.CurrentEditingTask))
                {
                    // Clean up previous editing state
                    if (_currentEditingTextBox != null)
                    {
                        _currentEditingTextBox.LostFocus -= CurrentEditingTextBox_LostFocus;
                        _currentEditingTextBox = null;
                        _currentEditingTaskModel = null;
                    }

                    // Handle new editing state
                    if (vm.CurrentEditingTask != null)
                    {
                        // Find the ListViewItem for the current editing task
                        ListView? parentListView = null;
                        // Iterate through all ListViews in the main grid
                        foreach (var listView in FindVisualChildren<ListView>(this))
                        {
                            if (listView.ItemsSource is System.Collections.IEnumerable items && items.Cast<object>().Contains(vm.CurrentEditingTask))
                            {
                                parentListView = listView;
                                break;
                            }
                        }

                        if (parentListView != null)
                        {
                            // Ensure the container is generated
                            // We might need to wait for the UI to update before finding the container and TextBox
                            // This is a common challenge with MVVM and UI interactions. Using Dispatcher can help.
                            Dispatcher.InvokeAsync(async () =>
                            {
                                // Add a small delay to allow the UI to update after IsEditing changes
                                await Task.Delay(50);
                                var listViewItem = (ListViewItem)parentListView.ItemContainerGenerator.ContainerFromItem(vm.CurrentEditingTask);
                                if (listViewItem != null)
                                {
                                    // Find the TextBox within the ListViewItem's DataTemplate
                                    var textBox = FindVisualChild<TextBox>(listViewItem);
                                    if (textBox != null)
                                    {
                                        // Store references and subscribe to LostFocus
                                        _currentEditingTextBox = textBox;
                                        _currentEditingTaskModel = vm.CurrentEditingTask;
                                        _currentEditingTextBox.LostFocus += CurrentEditingTextBox_LostFocus;

                                        // Set focus to the TextBox and select all text
                                        _currentEditingTextBox.Focus();
                                        _currentEditingTextBox.SelectAll();
                                    }
                                }
                            });
                        }
                    }
                }
            }
        }

        // Handler for the LostFocus event of the currently edited TextBox
        private void CurrentEditingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Ensure we have a task model and data context, and that the task is currently being edited.
            // The IsEditing check is crucial because the Enter key might have already ended editing or deleted the task.
            if (_currentEditingTaskModel != null && DataContext is CasualModeViewModel vm && _currentEditingTaskModel.IsEditing)
            {
                // Check if the task name is empty or whitespace
                if (string.IsNullOrWhiteSpace(_currentEditingTaskModel.Name))
                {
                    // If empty and still in editing, trigger the EndEditTaskCommand (which now handles deletion for empty tasks)
                    if (vm.EndEditTaskCommand.CanExecute(_currentEditingTaskModel))
                    {
                        vm.EndEditTaskCommand.Execute(_currentEditingTaskModel);
                    }
                }
                // If the name is not empty, the EndEditTaskCommand will not delete it, but will still end editing if called.
                // However, the Enter key binding already handles the non-empty case by calling EndEditTaskCommand.
                // So, we only need to explicitly call EndEditTaskCommand here if we are deleting.
                // If the task was not deleted, the IsEditing property will remain true until EndEditTaskCommand is called by Enter.
                // But we want focus to move away, so simply ending editing here if not deleted might be needed.
                // Let's rely on the Enter key handler to end editing for non-empty tasks.
                // If LostFocus happens on a non-empty task, we do nothing here, and the task remains in editing mode
                // until the user presses Enter.

                // Unsubscribe and clear references after handling LostFocus
                if (_currentEditingTextBox != null)
                {
                    _currentEditingTextBox.LostFocus -= CurrentEditingTextBox_LostFocus;
                }
                _currentEditingTextBox = null;
                _currentEditingTaskModel = null;
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null) return null;
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }
            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    yield return result;
                }
                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}