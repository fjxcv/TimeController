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
        // private void RewardPopup_Opened(object? sender, EventArgs e)
        // {
        //     // 在弹窗完全打开后，显式设置焦点到奖励任务输入框
        //     RewardTaskInput.Focus();
        // }
        // Store the currently edited TextBox and its TaskModel to handle LostFocus
        private TextBox? _currentEditingTextBox;
        private TaskModel? _currentEditingTaskModel;

        public CasualModeView()
        {
            InitializeComponent();
            DataContext = new CasualModeViewModel();
            RewardPopup.Opened += RewardPopup_Opened;

            // ViewModel的属性变化，处理View层的UI操作
            if (DataContext is CasualModeViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
                vm.Modules.CollectionChanged += Modules_CollectionChanged;
                // 为初始加载的模块订阅属性变化
                foreach (var module in vm.Modules)
                {
                    module.PropertyChanged += Module_PropertyChanged;
                }
            }
        }

        private void RewardPopup_Opened(object? sender, EventArgs e)
        {
            //确保弹窗完全打开
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (RewardTaskInput != null)
                {
                    RewardTaskInput.Focus();
                    Keyboard.Focus(RewardTaskInput);
                }
            }));
        }

        // private TextBox? _currentEditingTextBox;
        // private TaskModel? _currentEditingTaskModel;

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
            }
        }
        //双击编辑后是空任务点击别处就直接删除
        private void CurrentEditingTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_currentEditingTaskModel != null && DataContext is CasualModeViewModel vm && _currentEditingTaskModel.IsEditing)
            {
               
                if (string.IsNullOrWhiteSpace(_currentEditingTaskModel.Name))
                {
                    if (vm.EndEditTaskCommand.CanExecute(_currentEditingTaskModel))
                    {
                        vm.EndEditTaskCommand.Execute(_currentEditingTaskModel);
                    }
                }  
               
                if (_currentEditingTextBox != null)
                {
                    _currentEditingTextBox.LostFocus -= CurrentEditingTextBox_LostFocus;
                }
                _currentEditingTextBox = null;
                _currentEditingTaskModel = null;
            }
        }
        //
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

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}