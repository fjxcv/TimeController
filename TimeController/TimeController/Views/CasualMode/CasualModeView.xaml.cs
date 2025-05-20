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

                // 在View加载完成后，为所有ListView订阅PreviewMouseLeftButtonDown事件
                Loaded += (sender, e) =>
                {
                    var listViews = FindVisualChildren<ListView>(this);
                    foreach (var listView in listViews)
                    {
                        listView.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
                    }
                };
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
            if (e.PropertyName == nameof(ModuleViewModel.IsInputVisible))
            {
                if (sender is ModuleViewModel module && module.IsInputVisible)
                {
                    // 当输入框变为可见时，清除所有ListView的选中项
                    ClearAllListViewSelections();
                }
            }
        }

        // 监听ViewModel的属性变化
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is CasualModeViewModel vm)
            {
                if (e.PropertyName == nameof(CasualModeViewModel.IsRewardPopupOpen))
                {
                    if (vm.IsRewardPopupOpen)
                    {
                        // 使用Dispatcher确保在UI更新后获取焦点
                        Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Input,
                            new Action(() =>
                            {
                                try
                                {
                                    if (RewardTaskInput.IsVisible)
                                    {
                                        RewardTaskInput.Focus();
                                        RewardTaskInput.SelectAll();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to set focus on RewardTaskInput: {ex.Message}");
                                }
                            }));
                    }
                }
                else if (e.PropertyName == nameof(CasualModeViewModel.CurrentEditingTask))
                {
                     // 当CurrentEditingTask变化时，清除所有ListView的选中项
                    ClearAllListViewSelections();

                    if (vm.CurrentEditingTask != null)
                    {
                        // 使用Dispatcher确保在UI更新后获取焦点
                        Dispatcher.BeginInvoke(
                            System.Windows.Threading.DispatcherPriority.Input,
                            new Action(() =>
                            {
                                try
                                {
                                    // 查找当前任务项中的TextBox
                                    var listViewItems = FindVisualChildren<ListViewItem>(this);
                                    foreach (var item in listViewItems)
                                    {
                                        var textBox = FindVisualChild<TextBox>(item);
                                        if (textBox != null && textBox.DataContext == vm.CurrentEditingTask)
                                        {
                                            textBox.Focus();
                                            textBox.SelectAll();
                                            break;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to set focus on task TextBox: {ex.Message}");
                                }
                            }));
                    }
                }
            }
        }

        // 新增一个方法来清除所有ListView的选中项
        private void ClearAllListViewSelections()
        {
             var listViews = FindVisualChildren<ListView>(this);
            foreach (var listView in listViews)
            {
                listView.SelectedItem = null;
            }
        }

        // 新增ListView的PreviewMouseLeftButtonDown事件处理程序
        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 确保点击的是ListViewItem
            var listViewItem = FindVisualParent<ListViewItem>(e.OriginalSource as DependencyObject);
            if (listViewItem != null)
            {
                if (DataContext is CasualModeViewModel vm)
                {
                    // 清除所有其他ListView的选中项
                    var allListViews = FindVisualChildren<ListView>(this);
                    foreach (var lv in allListViews)
                    {
                        if (lv != sender)
                        {
                            lv.SelectedItem = null;
                        }
                    }

                    // 隐藏所有模块的输入框
                    foreach (var module in vm.Modules)
                    {
                        module.IsInputVisible = false;
                    }

                    // 结束任何正在进行的任务编辑
                    if (vm.CurrentEditingTask != null)
                    {
                        vm.CurrentEditingTask.IsEditing = false;
                        vm.CurrentEditingTask = null;
                    }
                }
            }
        }

        // 改进的FindVisualParent方法，以便在ListView_PreviewMouseLeftButtonDown中使用
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