using System.Windows;
using System.Diagnostics;
using TimeController.ViewModels;
using System.Windows.Controls;

namespace TimeController.Views.StrongGoalWeek
{
    public partial class ImportScheduleWindow : Window
    {
        public ImportScheduleWindow()
        {
            InitializeComponent();
            DataContext = new ImportScheduleViewModel(); // 绑定 ViewModel
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

