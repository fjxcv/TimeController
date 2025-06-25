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
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.Util.Collections;

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

            // 读取设置
            _semesterWeeks = Properties.Settings.Default.SemesterWeeks > 0 ? Properties.Settings.Default.SemesterWeeks : 18;
            if (Properties.Settings.Default.SemesterStartDate > DateTime.MinValue)
            {
                _semesterStartDate = Properties.Settings.Default.SemesterStartDate;
            }

            ImportFromUrlCommand = new RelayCommand(_ => ImportFromUrl(), _ => !IsImporting);
            ImportFromFileCommand = new RelayCommand(_ => ImportFromFile(), _ => !IsImporting);
            DownloadTemplateCommand = new RelayCommand(_ => DownloadTemplate(), _ => !IsImporting);
            OpenHelpCommand = new RelayCommand(_ => OpenHelp());
        }

        //学期周数
        private int _semesterWeeks = 18; // 默认18周
        public int SemesterWeeks
        {
            get => _semesterWeeks;
            set
            {
                if (_semesterWeeks != value && value >= 1)
                {
                    _semesterWeeks = value;
                    OnPropertyChanged();

                    // 将学期周数保存到应用设置中
                    Properties.Settings.Default.SemesterWeeks = value;
                    Properties.Settings.Default.Save();

                    // 立即通知WeekViewModel更新学期周数文本
                    App.Current.Dispatcher.InvokeAsync(() => {
                        // 触发事件通知学期信息更新
                        SemesterInfoUpdated?.Invoke(SemesterStartDate, value);
                        Console.WriteLine($"已将学期周数更新为: {value}，通知WeekViewModel更新显示");
                    });
                }
            }
        }


        // 从教务导入课程
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

                var courses = await Task.Run(() => ScheduleParser.ParseFromUrlAsync(inputUrl));
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

        // 从文件导入课程
        private async void ImportFromFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "所有支持的文件|*.xlsx;*.xls;*.csv|Excel文件 (*.xlsx, *.xls)|*.xlsx;*.xls|CSV文件 (*.csv)|*.csv",
                Title = "选择课表文件"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    // 检查Excel文件是否被占用
                    if (Path.GetExtension(filePath).ToLower() == ".xlsx" || Path.GetExtension(filePath).ToLower() == ".xls")
                    {
                        if (IsFileInUse(filePath))
                        {
                            MessageBox.Show("该Excel文件正在被其他程序占用，请先关闭后重试。", "文件被占用", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    IsImporting = true;
                    ImportStatus = "正在初始化...";

                    // 创建进度报告处理器
                    var progress = new Progress<(int current, int total, string message)>(progressInfo =>
                    {
                        // 更新进度条和状态文本
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ImportProgress = (double)progressInfo.current / progressInfo.total;
                            ImportStatus = progressInfo.message;
                        });
                    });

                    string extension = Path.GetExtension(filePath).ToLower();
                    List<Course> courses;

                    try
                    {
                        if (extension == ".xls" || extension == ".xlsx")
                        {
                            courses = await ScheduleParser.ParseExcelAsync(filePath, progress);
                        }
                        else if (extension == ".csv")
                        {
                            courses = await ScheduleParser.ParseCsvAsync(filePath, progress);
                        }
                        else
                        {
                            throw new NotSupportedException("不支持的文件格式");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Console.WriteLine($"解析文件失败: {ex.Message}\n{ex.StackTrace}");
                            MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            ImportStatus = "导入失败";
                            ImportProgress = 0;
                        });
                        IsImporting = false;
                        return;
                    }

                    // 确保回到UI线程进行UI相关操作
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        ImportStatus = "文件解析完成，处理课程数据...";
                        ImportProgress = 0.95; // 95%进度
                        Console.WriteLine($"文件解析完成，找到 {courses.Count} 门课程");

                        // 处理并保存导入的课程
                        await ProcessImportedCourses(courses);
                        ImportProgress = 1.0; // 100%进度
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Console.WriteLine($"导入失败: {ex.Message}\n{ex.StackTrace}");
                        MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        ImportStatus = "导入失败";
                        ImportProgress = 0;
                    });
                }
                finally
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        IsImporting = false;
                    });
                }
            }
        }


        // 检查文件是否被占用
        private bool IsFileInUse(string filePath)
        {
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // 如果能够打开文件，说明文件没有被占用
                    return false;
                }
            }
            catch (IOException)
            {
                // 如果捕获到IOException，说明文件被占用
                return true;
            }
            catch
            {
                // 其他异常不视为文件被占用
                return false;
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

                // 添加这一行，确保SemesterInfoUpdated事件也被触发
                SemesterInfoUpdated?.Invoke(SemesterStartDate, SemesterWeeks);

                return true;
            }
            catch (Exception)
            {
                // 发生异常时返回失败
                ImportStatus = "添加课程失败";
                return false;
            }
        }

        //传递学期开始日期和周数
        public event Action<DateTime, int> SemesterInfoUpdated;

        //检查导入的文件

        private async Task ProcessImportedCourses(List<Course> courses)
        {
            if (courses.Count == 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("未找到任何课程信息！", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ImportStatus = "未找到课程";
                });
                return;
            }

            // 验证课程数据
            var invalidCourses = new List<string>();
            var invalidWeekPatterns = new List<string>();
            var invalidNameLengths = new List<string>(); // 名称长度验证列表

            foreach (var course in courses)
            {
                if (string.IsNullOrWhiteSpace(course.Name))
                {
                    invalidCourses.Add($"课程名称为空");
                    continue;
                }

                // 验证课程名称长度
                if (course.Name.Length > 15)
                {
                    invalidNameLengths.Add($"课程 \"{course.Name}\" 名称超过15个字符");
                    continue;
                }

                if (course.StartTime >= course.EndTime)
                {
                    invalidCourses.Add($"课程 {course.Name} 的开始时间必须早于结束时间");
                    continue;
                }

                // 验证周次模式是否在学期周数范围内
                if (!string.IsNullOrWhiteSpace(course.WeekPattern))
                {
                    bool hasInvalidWeek = false;

                    // 处理形如"1-16"的范围格式
                    if (course.WeekPattern.Contains("-"))
                    {
                        var parts = course.WeekPattern.Split('-');
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0].Trim(), out int start) &&
                            int.TryParse(parts[1].Trim(), out int end))
                        {
                            if (start < 1 || end > SemesterWeeks)
                            {
                                hasInvalidWeek = true;
                            }
                        }
                    }
                    // 处理形如"1,3,5,7"的列表格式
                    else if (course.WeekPattern.Contains(","))
                    {
                        var weeks = course.WeekPattern.Split(',');
                        foreach (var week in weeks)
                        {
                            if (int.TryParse(week.Trim(), out int weekNum))
                            {
                                if (weekNum < 1 || weekNum > SemesterWeeks)
                                {
                                    hasInvalidWeek = true;
                                    break;
                                }
                            }
                        }
                    }
                    // 处理单个数字
                    else if (int.TryParse(course.WeekPattern.Trim(), out int singleWeek))
                    {
                        if (singleWeek < 1 || singleWeek > SemesterWeeks)
                        {
                            hasInvalidWeek = true;
                        }
                    }

                    if (hasInvalidWeek)
                    {
                        invalidWeekPatterns.Add($"课程 {course.Name} 的周次模式 \"{course.WeekPattern}\" 不在合法范围内（必须在1到{SemesterWeeks}周范围内）");
                    }
                }
            }

            // 合并所有验证错误
            invalidCourses.AddRange(invalidWeekPatterns);
            invalidCourses.AddRange(invalidNameLengths); // 添加名称长度验证错误

            bool shouldContinue = true;
            if (invalidCourses.Count > 0)
            {
                var message = string.Join("\n", invalidCourses);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var result = MessageBox.Show(
                        $"发现 {invalidCourses.Count} 条无效记录：\n{message}\n\n是否仍要导入有效的课程？",
                        "数据验证警告",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                    {
                        ImportStatus = "导入已取消";
                        shouldContinue = false;
                    }
                });

                if (!shouldContinue)
                    return;

                // 如果发现周次不合法的课程，需要移除
                if (invalidWeekPatterns.Count > 0)
                {
                    var invalidCourseNames = invalidWeekPatterns
                        .Select(msg => msg.Substring(0, msg.IndexOf(" 的周次模式")))
                        .ToList();

                    // 移除周次不合法的课程
                    courses.RemoveAll(c => invalidCourseNames.Contains($"课程 {c.Name}"));
                }

                // 移除名称过长的课程
                if (invalidNameLengths.Count > 0)
                {
                    var longNameCourses = invalidNameLengths
                        .Select(msg => msg.Substring(msg.IndexOf("\"") + 1, msg.LastIndexOf("\"") - msg.IndexOf("\"") - 1))
                        .ToList();

                    courses.RemoveAll(c => longNameCourses.Contains(c.Name));
                }

                // 移除其他无效课程
                courses.RemoveAll(c => string.IsNullOrWhiteSpace(c.Name) || c.StartTime >= c.EndTime);
            }

            // 检查是否已存在课程
            bool hasExistingCourses = false;
            try
            {
                var existingCourses = await _taskService.GetAllCourseTasksAsync();
                hasExistingCourses = existingCourses.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查现有课程时出错: {ex.Message}");
            }

            // 如果存在课程，提示用户选择替换还是添加
            bool replaceCourses = false;
            if (hasExistingCourses)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var result = MessageBox.Show(
                        "检测到系统中已有课程数据，是否要替换现有课表？\n\n" +
                        "• 点击\"是\"：删除所有现有课程，仅保留新导入的课程\n" +
                        "• 点击\"否\"：保留现有课程，将新课程添加到现有课表中",
                        "课表导入确认",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    replaceCourses = (result == MessageBoxResult.Yes);
                });
            }

            // 保存有效课程时，同时保存学期周数
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ImportStatus = "正在保存课程...";
            });

            try
            {
                // 这部分是数据库操作，可以在后台线程执行
                await Task.Run(async () =>
                {
                    using (var transaction = _dbService.TaskService.BeginTransaction())
                    {
                        try
                        {
                            // 如果选择替换，先删除所有现有课程
                            if (replaceCourses && hasExistingCourses)
                            {
                                var existingCourses = await _taskService.GetAllCourseTasksAsync();
                                foreach (var courseTask in existingCourses)
                                {
                                    await _taskService.DeleteTaskAsync(courseTask);
                                }
                                Console.WriteLine($"已删除所有现有课程 ({existingCourses.Count} 个)");
                            }

                            // 保存新课程
                            await _dbService.SaveCourses(courses, SemesterStartDate);
                            _dbService.TaskService.CommitTransaction();
                        }
                        catch
                        {
                            _dbService.TaskService.RollbackTransaction();
                            throw;
                        }
                    }
                });

                // 保存学期周数到全局设置 - 这需要在UI线程
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    App.Current.Properties["SemesterWeeks"] = SemesterWeeks.ToString();
                    App.Current.Properties["SemesterStartDate"] = SemesterStartDate.ToString("yyyy-MM-dd");
                    // 关键：保存到磁盘
                    if (App.Current is Application app && app is not null)
                    {
                        if (app is System.Windows.Application wpfApp)
                        {
                            // WPF Application 没有 SavePropertiesAsync
                            Properties.Settings.Default.Save();
                        }
                        else
                        {
                            // 如果有 SavePropertiesAsync 方法
                            var saveMethod = app.GetType().GetMethod("SavePropertiesAsync");
                            if (saveMethod != null)
                                await (Task)saveMethod.Invoke(app, null);
                        }
                    }
                });


                // 根据操作类型显示不同的成功消息 - 这需要在UI线程
                string operationMessage = replaceCourses ? "替换" : "添加";

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"成功{operationMessage} {courses.Count} 门课程！\n开学日期: {SemesterStartDate:yyyy年M月d日}\n学期周数: {SemesterWeeks}周",
                        "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    ImportStatus = $"已{operationMessage} {courses.Count} 门课程";

                    _hasImportedCourses = true;

                    // 触发事件通知已保存课程及相关信息
                    CoursesSavedWithStartDate?.Invoke(SemesterStartDate);
                    SemesterInfoUpdated?.Invoke(SemesterStartDate, SemesterWeeks);
                    CoursesImported?.Invoke(this, courses.Count);
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _hasImportedCourses = false;
                    MessageBox.Show($"保存课程失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    ImportStatus = "保存失败";
                });
            }
        }

        // 下载模板
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

        // 创建模板文件
        private void CreateTemplateFile(string filePath)
        {
            try
            {
                // 使用修改后的创建模板方法，确保周次模式列为文本格式
                using (var workbook = new XSSFWorkbook())
                {
                    ISheet sheet = workbook.CreateSheet("课表模板");

                    // 创建表头样式
                    ICellStyle headerStyle = workbook.CreateCellStyle();
                    headerStyle.FillForegroundColor = IndexedColors.Grey25Percent.Index;
                    headerStyle.FillPattern = FillPattern.SolidForeground;

                    IFont headerFont = workbook.CreateFont();
                    headerFont.Boldweight = (short)FontBoldWeight.Bold;
                    headerStyle.SetFont(headerFont);

                    // 创建表头行
                    IRow headerRow = sheet.CreateRow(0);
                    string[] headers = { "课程名称", "星期几", "开始时间", "结束时间", "上课地点", "教师姓名", "周次模式" };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        ICell cell = headerRow.CreateCell(i);
                        cell.SetCellValue(headers[i]);
                        cell.CellStyle = headerStyle;
                        sheet.AutoSizeColumn(i);
                    }

                    // 创建文本格式样式，特别是针对周次模式列
                    ICellStyle textStyle = workbook.CreateCellStyle();
                    IDataFormat format = workbook.CreateDataFormat();
                    textStyle.DataFormat = format.GetFormat("@");  // '@'表示文本格式

                    // 创建示例数据行
                    IRow exampleRow1 = sheet.CreateRow(1);
                    exampleRow1.CreateCell(0).SetCellValue("高等数学");
                    exampleRow1.CreateCell(1).SetCellValue("周一");
                    exampleRow1.CreateCell(2).SetCellValue("08:00");
                    exampleRow1.CreateCell(3).SetCellValue("09:40");
                    exampleRow1.CreateCell(4).SetCellValue("教学楼A101");
                    exampleRow1.CreateCell(5).SetCellValue("张教授");

                    // 设置整个第一列为文本格式
                    sheet.SetDefaultColumnStyle(0, textStyle);

                    // 设置周次模式单元格为文本格式
                    ICell weekPatternCell1 = exampleRow1.CreateCell(6);
                    weekPatternCell1.SetCellValue("2-4"); 
                    weekPatternCell1.CellStyle = textStyle;

                    IRow exampleRow2 = sheet.CreateRow(2);
                    exampleRow2.CreateCell(0).SetCellValue("数据结构");
                    exampleRow2.CreateCell(1).SetCellValue("周三");
                    exampleRow2.CreateCell(2).SetCellValue("14:00");
                    exampleRow2.CreateCell(3).SetCellValue("15:40");
                    exampleRow2.CreateCell(4).SetCellValue("计算机楼204");
                    exampleRow2.CreateCell(5).SetCellValue("李老师");

                    // 设置周次模式单元格为文本格式
                    ICell weekPatternCell2 = exampleRow2.CreateCell(6);
                    weekPatternCell2.SetCellValue("1,3,5,7,9,11,13,15");
                    weekPatternCell2.CellStyle = textStyle;

                    // 预先将整个周次模式列(第7列)设置为文本格式
                    sheet.SetDefaultColumnStyle(6, textStyle);

                    // 添加说明
                    IRow noteRow1 = sheet.CreateRow(4);
                    noteRow1.CreateCell(0).SetCellValue("填写说明:");

                    string[] notes = {
                        "1. 课程名称必填，且不超过15个字符",
                        "2. 星期几可填写: 周一/星期一/一/1等格式",
                        "3. 时间格式为24小时制: HH:MM",
                        "4. 周次模式可填写: 1-16(表示1到16周), 1,3,5(表示1,3,5周)",
                        "5. 注意：填写周次模式时，在数字前添加英文单引号可防止自动转为日期格式（如：'2-4）"
                    };

                    for (int i = 0; i < notes.Length; i++)
                    {
                        IRow noteRow = sheet.CreateRow(5 + i);
                        noteRow.CreateCell(0).SetCellValue(notes[i]);
                    }

                    // 调整列宽
                    for (int i = 0; i < headers.Length; i++)
                    {
                        sheet.AutoSizeColumn(i);
                    }

                    // 保存文件
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        workbook.Write(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建模板文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // 打开帮助文档
        private void OpenHelp()
        {
            MessageBox.Show(
                "课表导入帮助：\n\n" +
                "1. 文件导入支持Excel和CSV格式\n\n" +
                "2. Excel格式要求：\n" +
                "     第1列：课程名称（必填，不超过15个字符）\n" +
                "     第2列：星期几（如周一、星期二等）\n" +
                "     第3列：开始时间（格式如8:00）\n" +
                "     第4列：结束时间（格式如9:40）\n" +
                "     第5列：上课地点\n" +
                "     第6列：教师姓名\n\n" +
                "3. 您可以下载模板文件作为参考\n\n" +
                "4. 使用模板文件导入时填写说明需删除\n\n",
                "导入帮助",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // 添加进度条属性
        private double _importProgress;
        public double ImportProgress
        {
            get => _importProgress;
            set
            {
                _importProgress = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
