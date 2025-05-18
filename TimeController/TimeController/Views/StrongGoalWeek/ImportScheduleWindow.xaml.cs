using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Diagnostics;


namespace TimeController.Views.StrongGoalWeek
{
    /// <summary>
    /// ImportScheduleWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ImportScheduleWindow : Window
    {
        public ImportScheduleWindow()
        {
            InitializeComponent();
        }


        // 通过链接导入
        private void Import_Click(object sender, RoutedEventArgs e)
        {
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

        // 文件导入
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "选择课表文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                // TODO: 解析并导入课表逻辑
                MessageBox.Show("已选择文件：" + filePath + "，请在这里实现解析和导入逻辑。");
            }
        }
    }
}
