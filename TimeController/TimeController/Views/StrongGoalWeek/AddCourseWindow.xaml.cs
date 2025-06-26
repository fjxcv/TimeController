using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TimeController.Models;
using TimeController.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using TimeController.Services;

namespace TimeController.Views.StrongGoalWeek
{
    // 检查两个集合是否有重叠元素
    public static class HashSetExtensions
    {
        public static bool Overlaps<T>(this HashSet<T> first, HashSet<T> second)
        {
            return first.Any(item => second.Contains(item));
        }
    }
    public partial class AddCourseWindow : Window
    {
        public Course ResultCourse { get; private set; }
        private AddCourseViewModel _viewModel;

        public AddCourseWindow(DateTime semesterStartDate)
        {
            InitializeComponent();
            _viewModel = new AddCourseViewModel(semesterStartDate);
            DataContext = _viewModel;
        }

        // 添加课程按钮
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(_viewModel.Name))
                {
                    MessageBox.Show("课程名称不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_viewModel.Name.Length > 15)
                {
                    MessageBox.Show("课程名称最多为15个字符！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_viewModel.WeekPattern))
                {
                    MessageBox.Show("上课周次不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 创建结果对象
                var newCourse = new Course
                {
                    Name = _viewModel.Name,
                    DayOfWeek = _viewModel.DayOfWeek,
                    StartTime = _viewModel.Course.StartTime,
                    EndTime = _viewModel.Course.EndTime,
                    Location = _viewModel.Location,
                    Teacher = _viewModel.Teacher,
                    WeekPattern = _viewModel.WeekPattern
                };

                // 设置结果并关闭窗口 - 不在这里检查冲突，将冲突检查放到ViewModel中
                ResultCourse = newCourse;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建课程时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
        }

        // 取消按钮
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        // 防止TimePicker的x直接关闭窗口
        protected override void OnClosing(CancelEventArgs e)
        {
            if (Keyboard.FocusedElement is DependencyObject focused &&
                FindParent<TimePicker>(focused) != null)
            {
                // 当前焦点在 TimePicker 里，不允许关闭窗口！
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        // 递归查找父级元素
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typed) return typed;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
