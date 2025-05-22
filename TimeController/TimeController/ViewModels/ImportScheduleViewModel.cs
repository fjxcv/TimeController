using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeController.Models;
using TimeController.Services;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows;

namespace TimeController.ViewModels
{
    public class ImportScheduleViewModel
    {
        private string _inputUrl;
        public string InputUrl
        {
            get => _inputUrl;
            set
            {
                _inputUrl = value;
                OnPropertyChanged(nameof(InputUrl));
            }
        }

        private readonly DatabaseService _dbService = new DatabaseService();
        public ICommand ImportFromUrlCommand { get; }
        public ICommand ImportFromFileCommand { get; }

        public ImportScheduleViewModel()
        {
            ImportFromUrlCommand = new RelayCommand(_ => ImportFromUrl());
            ImportFromFileCommand = new RelayCommand(_ => ImportFromFile());
        }

        /*private void ImportFromUrl()
        {
            // 弹出输入框，提示用户输入链接
            string inputUrl = Microsoft.VisualBasic.Interaction.InputBox("请输入教务系统链接：", "教务系统导入", "https://example.com");

            // 检查用户是否点击了取消或关闭按钮
            if (string.IsNullOrEmpty(inputUrl))
            {
                MessageBox.Show("未输入链接，操作已取消！");
                return;
            }

            try
            {
                // 解析课程信息
                var courses = ScheduleParser.ParseFromUrl(inputUrl);

                // 验证课程信息
                if (ValidateCourses(courses))
                {
                    // 保存课程信息到数据库
                    _dbService.SaveCourses(courses);
                    MessageBox.Show($"成功导入 {courses.Count} 门课程！");

                    // 跳转到网页
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(inputUrl)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}");
            }
        }*/

        private void ImportFromUrl()
        {
            string inputUrl = Microsoft.VisualBasic.Interaction.InputBox("请输入教务系统链接：", "教务系统导入", "https://example.com");

            if (string.IsNullOrEmpty(inputUrl))
            {
                MessageBox.Show("未输入链接，操作已取消！");
                return;
            }

            try
            {
                var courses = ScheduleParser.ParseFromUrl(inputUrl);

                if (ValidateCourses(courses))
                {
                    _dbService.SaveCourses(courses);
                    MessageBox.Show($"成功导入 {courses.Count} 门课程！");
                    Console.WriteLine($"Trying to open: {inputUrl}"); // 调试输出

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(inputUrl)
                    {
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}");
            }
        }

        private void ImportFromFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel 97-2003 Files (*.xls)|*.xls|Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv",
                Title = "选择课表文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    List<Course> courses;
                    string extension = System.IO.Path.GetExtension(filePath).ToLower();

                    if (extension == ".xls" || extension == ".xlsx")
                    {
                        courses = ScheduleParser.ParseExcel(filePath);
                    }
                    else if (extension == ".csv")
                    {
                        courses = ScheduleParser.ParseCsv(filePath);
                    }
                    else
                    {
                        MessageBox.Show("不支持的文件格式！");
                        return;
                    }

                    if (ValidateCourses(courses))
                    {
                        _dbService.SaveCourses(courses);
                        MessageBox.Show($"成功导入 {courses.Count} 门课程！");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败：{ex.Message}");
                    // 可以在这里添加日志记录或更详细的错误处理
                }
            }
        }

        private bool ValidateCourses(List<Course> courses)
        {
            foreach (var course in courses)
            {
                if (course.StartTime >= course.EndTime)
                {
                    MessageBox.Show($"课程 {course.Name} 时间错误：开始时间不能晚于结束时间！");
                    return false;
                }
            }
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
