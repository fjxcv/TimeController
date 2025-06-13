using System.Windows;
using System.Diagnostics;
using TimeController.ViewModels;
using System.Windows.Controls;
using TimeController.Models;
using TimeController.ViewModels;
using TimeController.Views.StrongGoalWeek;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.Views.StrongGoalWeek
{
    public partial class ImportScheduleWindow : Window
    {
        private readonly ImportScheduleViewModel _viewModel;

        public ImportScheduleWindow()
        {
            InitializeComponent();
            _viewModel = new ImportScheduleViewModel();
            DataContext = _viewModel;

            // 找到 XAML 定义的 DatePicker 控件
            if (this.FindName("StartDatePicker") is DatePicker datePicker)
            {
                // 设置日期范围
                datePicker.DisplayDateStart = DateTime.Today.AddYears(-1);
                datePicker.DisplayDateEnd = DateTime.Today.AddYears(1);
            }
            // 订阅课程导入成功事件
            _viewModel.CoursesImported += (sender, count) => {
                // 短暂延迟后关闭窗口，让用户看到成功消息
                Task.Delay(1500).ContinueWith(_ => {
                    Dispatcher.Invoke(() => {
                        this.Close();
                    });
                });
            };
        }

        // 通过链接导入
        private void Import_Click(object sender, RoutedEventArgs e)
        {
            // Assuming InputBox is a TextBox defined in the XAML file
            TextBox InputBox = this.FindName("InputBox") as TextBox;

            if (InputBox == null)
            {
                MessageBox.Show("未找到名为 'InputBox' 的控件，请检查 XAML 文件。");
                return;
            }

            string input = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入链接或源码！");
                return;
            }

            // 打开浏览器
            Process.Start(new ProcessStartInfo
            {
                FileName = input,
                UseShellExecute = true
            });

            //TODO:导入逻辑
        }
    }
}

