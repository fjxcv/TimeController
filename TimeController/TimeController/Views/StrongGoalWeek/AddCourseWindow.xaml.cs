using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TimeController.Models;
using TimeController.ViewModels;
using iNKORE.UI.WPF.Modern.Controls;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.Views.StrongGoalWeek
{
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

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(_viewModel.Name))
            {
                MessageBox.Show("课程名称不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_viewModel.WeekPattern))
            {
                System.Windows.MessageBox.Show("上课周次不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 创建结果对象
            ResultCourse = new Course
            {
                Name = _viewModel.Name,
                DayOfWeek = _viewModel.DayOfWeek,
                StartTime = _viewModel.Course.StartTime,
                EndTime = _viewModel.Course.EndTime,
                Location = _viewModel.Location,
                Teacher = _viewModel.Teacher,
                WeekPattern = _viewModel.WeekPattern
            };

            DialogResult = true;
        }


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
