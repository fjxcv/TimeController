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
using System.Windows.Media.Animation;

namespace TimeController.Views.CasualMode
{
    /// <summary>
    /// CasualModeView.xaml 的交互逻辑
    /// </summary>
    public partial class CasualModeView : Page

    {
        private Random _rand = new Random(); // 用于烟花动画的随机数生成器

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

            // 播放奖励音效和烟花动画
            PlayFireworksAndSound();
        }

        private void PlayFireworksAndSound()
        {
            // 播放音效
            if (RewardSound != null)
            {
                RewardSound.Stop(); // 确保音效重置
                RewardSound.Play();
            }

            // 简单的烟花动画
            if (FireworksCanvas != null)
            {
                FireworksCanvas.Children.Clear(); // 清除之前的烟花粒子

                for (int i = 0; i < 50; i++) // 创建 50 个小圆点作为烟花粒子
                {
                    Ellipse fireworkParticle = new Ellipse
                    {
                        Width = _rand.Next(3, 8),
                        Height = _rand.Next(3, 8),
                        Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, (byte)_rand.Next(256), (byte)_rand.Next(256), (byte)_rand.Next(256))),
                        Opacity = 1.0
                    };

                    double startX = _rand.NextDouble() * FireworksCanvas.ActualWidth;
                    double startY = _rand.NextDouble() * FireworksCanvas.ActualHeight;

                    Canvas.SetLeft(fireworkParticle, startX);
                    Canvas.SetTop(fireworkParticle, startY);
                    FireworksCanvas.Children.Add(fireworkParticle);

                    // 动画：位置随机移动并逐渐消失
                    System.Windows.Media.Animation.DoubleAnimation xAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        To = startX + (_rand.NextDouble() - 0.5) * 100, // 随机水平移动
                        Duration = TimeSpan.FromSeconds(1.5),
                        AutoReverse = false
                    };
                    System.Windows.Media.Animation.DoubleAnimation yAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        To = startY + (_rand.NextDouble() - 0.5) * 100, // 随机垂直移动
                        Duration = TimeSpan.FromSeconds(1.5),
                        AutoReverse = false
                    };
                    System.Windows.Media.Animation.DoubleAnimation fadeAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        To = 0,
                        Duration = TimeSpan.FromSeconds(1.5),
                        AutoReverse = false
                    };

                    System.Windows.Media.Animation.Storyboard storyboard = new System.Windows.Media.Animation.Storyboard();
                    storyboard.Children.Add(xAnimation);
                    storyboard.Children.Add(yAnimation);
                    storyboard.Children.Add(fadeAnimation);

                    System.Windows.Media.Animation.Storyboard.SetTarget(xAnimation, fireworkParticle);
                    System.Windows.Media.Animation.Storyboard.SetTargetProperty(xAnimation, new PropertyPath("(Canvas.Left)"));
                    System.Windows.Media.Animation.Storyboard.SetTarget(yAnimation, fireworkParticle);
                    System.Windows.Media.Animation.Storyboard.SetTargetProperty(yAnimation, new PropertyPath("(Canvas.Top)"));
                    System.Windows.Media.Animation.Storyboard.SetTarget(fadeAnimation, fireworkParticle);
                    System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));

                    // 动画完成后移除粒子
                    storyboard.Completed += (s, ev) => FireworksCanvas.Children.Remove(fireworkParticle);
                    storyboard.Begin();
                }
            }
        }

        private TextBox? _currentEditingTextBox;
        private TaskModel? _currentEditingTaskModel;

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