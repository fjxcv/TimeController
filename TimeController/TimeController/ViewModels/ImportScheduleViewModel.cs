using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using TimeController.Models;
using TimeController.Services;
using OfficeOpenXml;
using TimeController.Views.StrongGoalWeek;
using TimeController;

using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.ViewModels
{
    public class ImportScheduleViewModel : INotifyPropertyChanged
    {
        private readonly ITaskService _taskService;
        private readonly DatabaseService _dbService;
        private bool _isImporting;
        private string _importStatus;
        private string _templateFilePath;
        public event Action<DateTime> CoursesSavedWithStartDate;
        private DateTime _semesterStartDate = DateTime.Today;
        public ICommand AddCourseCommand { get; }
        // 添加保存事件，传递开学日期
        public DateTime SemesterStartDate
        {
            get => _semesterStartDate;
            set
            {
                _semesterStartDate = value;
                OnPropertyChanged();
            }
        }

        // 跟踪是否已成功导入课程
        private bool _hasImportedCourses;
        public bool HasImportedCourses => _hasImportedCourses;
        // 课程导入成功事件
        public event EventHandler<int> CoursesImported;


        public bool IsImporting
        {
            get => _isImporting;
            set
            {
                _isImporting = value;
                OnPropertyChanged();
                // 在导入过程中禁用按钮
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string ImportStatus
        {
            get => _importStatus;
            set
            {
                _importStatus = value;
                OnPropertyChanged();
            }
        }

        public ICommand ImportFromUrlCommand { get; }
        public ICommand ImportFromFileCommand { get; }
        public ICommand DownloadTemplateCommand { get; }
        public ICommand OpenHelpCommand { get; }

        public ImportScheduleViewModel()
        {
            _taskService = App.Services.GetService(typeof(ITaskService)) as ITaskService;
            _dbService = new DatabaseService(_taskService);
            _templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "CourseTemplate.xlsx");

            // 设置开学日期为最近的周一
            DateTime today = DateTime.Today;
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
            SemesterStartDate = today.AddDays(daysUntilMonday);

            ImportFromUrlCommand = new RelayCommand(_ => ImportFromUrl(), _ => !IsImporting);
            ImportFromFileCommand = new RelayCommand(_ => ImportFromFile(), _ => !IsImporting);
            DownloadTemplateCommand = new RelayCommand(_ => DownloadTemplate(), _ => !IsImporting);
            OpenHelpCommand = new RelayCommand(_ => OpenHelp());
        }
        

        private async void ImportFromUrl()
        {
            string inputUrl = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入教务系统链接：",
                "教务系统导入",
                "https://example.com");

            if (string.IsNullOrEmpty(inputUrl))
            {
                MessageBox.Show("未输入链接，操作已取消！");
                return;
            }

            try
            {
                IsImporting = true;
                ImportStatus = "正在从链接导入...";

                var courses = await Task.Run(() => ScheduleParser.ParseFromUrl(inputUrl));
                await ProcessImportedCourses(courses);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败：{ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                ImportStatus = "导入失败";
            }
            finally
            {
                IsImporting = false;
            }
        }

        private async void ImportFromFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "所有支持的文件|*.xlsx;*.xls;*.csv|Excel 文件 (*.xlsx, *.xls)|*.xlsx;*.xls|CSV 文件 (*.csv)|*.csv",
                Title = "选择课表文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    IsImporting = true;
                    ImportStatus = "正在解析文件...";

                    List<Course> courses;
                    string extension = Path.GetExtension(filePath).ToLower();

                    courses = await Task.Run(() =>
                    {
                        if (extension == ".xls" || extension == ".xlsx")
                        {
                            return ScheduleParser.ParseExcel(filePath);
                        }
                        else if (extension == ".csv")
                        {
                            return ScheduleParser.ParseCsv(filePath);
                        }
                        else
                        {
                            throw new NotSupportedException("不支持的文件格式");
                        }
                    });

                    await ProcessImportedCourses(courses);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    ImportStatus = "导入失败";
                }
                finally
                {
                    IsImporting = false;
                }
            }
        }

        // 手动添加课程
        public async Task<bool> AddCourseByHand(Course course)
        {
            if (course == null)
                throw new ArgumentNullException(nameof(course));

            try
            {
                // 保存课程到数据库
                await _dbService.SaveCourses(new List<Course> { course }, SemesterStartDate);

                // 更新导入状态
                ImportStatus = "已添加课程";

                // 标记已导入课程
                _hasImportedCourses = true;

                // 触发事件通知已保存课程及开学日期
                CoursesSavedWithStartDate?.Invoke(SemesterStartDate);

                return true;
            }
            catch (Exception)
            {
                // 发生异常时返回失败
                ImportStatus = "添加课程失败";
                return false;
            }
        }

        private async Task ProcessImportedCourses(List<Course> courses)
        {
            if (courses.Count == 0)
            {
                MessageBox.Show("未找到任何课程信息！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                ImportStatus = "未找到课程";
                return;
            }

            // 验证课程数据
            var invalidCourses = new List<string>();
            foreach (var course in courses)
            {
                if (string.IsNullOrWhiteSpace(course.Name))
                {
                    invalidCourses.Add($"课程名称为空");
                    continue;
                }

                if (course.StartTime >= course.EndTime)
                {
                    invalidCourses.Add($"课程 {course.Name} 的开始时间必须早于结束时间");
                    continue;
                }
            }

            if (invalidCourses.Count > 0)
            {
                var message = string.Join("\n", invalidCourses);
                var result = MessageBox.Show(
                    $"发现 {invalidCourses.Count} 条无效记录：\n{message}\n\n是否仍要导入有效的课程？",
                    "数据验证警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    ImportStatus = "导入已取消";
                    return;
                }

                // 移除无效课程
                courses.RemoveAll(c => string.IsNullOrWhiteSpace(c.Name) || c.StartTime >= c.EndTime);
            }

            // 保存有效课程
            ImportStatus = "正在保存课程...";
            try
            {
                await _dbService.SaveCourses(courses, SemesterStartDate);
                MessageBox.Show($"成功导入 {courses.Count} 门课程！\n开学日期: {SemesterStartDate:yyyy年M月d日}",
                    "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                ImportStatus = $"已导入 {courses.Count} 门课程";

                // 触发事件通知已保存课程及开学日期
                CoursesSavedWithStartDate?.Invoke(SemesterStartDate);

                // 触发原有事件
                CoursesImported?.Invoke(this, courses.Count);
                Console.WriteLine($"成功导入 {courses.Count} 门课程，已触发事件");
            }
            catch (Exception ex)
            {
                _hasImportedCourses = false;
                MessageBox.Show($"保存课程失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                ImportStatus = "保存失败";
            }
        }

        private void DownloadTemplate()
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel 文件 (*.xlsx)|*.xlsx",
                Title = "保存课表模板",
                FileName = "课表导入模板.xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // 检查模板文件是否存在
                    if (!File.Exists(_templateFilePath))
                    {
                        // 如果模板不存在，创建一个简单的模板
                        CreateTemplateFile(saveDialog.FileName);
                    }
                    else
                    {
                        // 复制现有模板
                        File.Copy(_templateFilePath, saveDialog.FileName, true);
                    }

                    MessageBox.Show("模板已下载到指定位置，请按模板格式填写课表信息。", "下载成功", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 打开文件
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"下载模板失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateTemplateFile(string filePath)
        {
            using (var package = new OfficeOpenXml.ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("课表模板");

                // 添加表头
                worksheet.Cells[1, 1].Value = "课程名称";
                worksheet.Cells[1, 2].Value = "星期几";
                worksheet.Cells[1, 3].Value = "开始时间";
                worksheet.Cells[1, 4].Value = "结束时间";
                worksheet.Cells[1, 5].Value = "上课地点";
                worksheet.Cells[1, 6].Value = "教师姓名";

                // 添加示例数据
                worksheet.Cells[2, 1].Value = "高等数学";
                worksheet.Cells[2, 2].Value = "周一";
                worksheet.Cells[2, 3].Value = "08:00";
                worksheet.Cells[2, 4].Value = "09:40";
                worksheet.Cells[2, 5].Value = "教学楼A101";
                worksheet.Cells[2, 6].Value = "张教授";

                // 添加说明
                worksheet.Cells[4, 1].Value = "填写说明:";
                worksheet.Cells[5, 1].Value = "1. 课程名称必填";
                worksheet.Cells[6, 1].Value = "2. 星期几可填写: 周一/星期一/一/1等格式";
                worksheet.Cells[7, 1].Value = "3. 时间格式为24小时制: HH:MM";

                // 保存文件
                var fileInfo = new FileInfo(filePath);
                package.SaveAs(fileInfo);
            }
        }

        private void OpenHelp()
        {
            MessageBox.Show(
                "课表导入帮助：\n\n" +
                "1. 文件导入支持Excel和CSV格式\n" +
                "2. Excel格式要求：\n" +
                "   - 第1列：课程名称\n" +
                "   - 第2列：星期几（如周一、星期二等）\n" +
                "   - 第3列：开始时间（格式如8:00）\n" +
                "   - 第4列：结束时间（格式如9:40）\n" +
                "   - 第5列：上课地点\n" +
                "   - 第6列：教师姓名\n\n" +
                "3. 您可以下载模板文件作为参考\n\n" +
                "4. 导入后的课程将作为任务显示在课表中",
                "导入帮助",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
