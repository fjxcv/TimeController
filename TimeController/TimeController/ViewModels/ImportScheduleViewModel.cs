using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
//using NPOI.Util.Collections;
using TimeController.ViewModels;

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

        /// <summary>
        /// 检查课程是否与现有课程冲突
        /// </summary>
        /// <param name="course">要检查的课程</param>
        /// <returns>冲突课程列表和是否有冲突</returns>
        public async Task<(bool hasConflict, List<Course> conflictCourses)> CheckCourseConflict(Course course)
        {
            var conflicts = new List<Course>();

            try
            {
                // 只有当课程时间有效时才检查冲突
                if (course.StartTime >= course.EndTime)
                {
                    return (false, conflicts);
                }

                Console.WriteLine($"检查课程冲突: {course.Name}, 星期: {course.DayOfWeek}, 时间: {course.StartTime}-{course.EndTime}");

                // 获取课程对应的星期几索引(0=周一)
                int dayIndex = GetDayIndex(course.DayOfWeek);

                // 从数据库获取所有课程任务 - 确保获取所有课程，包括文件导入的和手动添加的
                var existingCourses = await _taskService.GetAllCourseTasksAsync();
                Console.WriteLine($"数据库中共有 {existingCourses.Count} 门课程");

                // 解析新课程的周次模式
                System.Collections.Generic.HashSet<int> newCourseWeeks = AddCourseViewModel.ParseWeekPattern(course.WeekPattern);
                Console.WriteLine($"新课程周次: {string.Join(", ", newCourseWeeks)}");

                // 检查冲突 - 所有课程都检查，不区分来源
                foreach (var existingCourse in existingCourses)
                {
                    // 只检查同一天的课程
                    if (existingCourse.WeekDay == dayIndex)
                    {
                        // 忽略无效时间段
                        if (!existingCourse.StartTime.HasValue || !existingCourse.EndTime.HasValue ||
                            existingCourse.StartTime.Value >= existingCourse.EndTime.Value)
                        {
                            Console.WriteLine($"跳过课程 {existingCourse.Name}：无效时间段");
                            continue;
                        }

                        // 检查时间重叠 - 使用统一的冲突检测逻辑
                        bool hasTimeOverlap = Math.Max(course.StartTime.TotalMinutes, existingCourse.StartTime.Value.TotalMinutes) <
                                            Math.Min(course.EndTime.TotalMinutes, existingCourse.EndTime.Value.TotalMinutes);

                        Console.WriteLine($"与课程 {existingCourse.Name} 时间是否重叠: {hasTimeOverlap}");

                        if (hasTimeOverlap)
                        {
                            // 提取周次模式
                            string existingWeekPattern = ExtractWeekPatternFromNote(existingCourse.Note);
                            Console.WriteLine($"现有课程周次模式: {existingWeekPattern}");

                            System.Collections.Generic.HashSet<int> existingWeeks;
                            try
                            {
                                existingWeeks = AddCourseViewModel.ParseWeekPattern(existingWeekPattern);
                                Console.WriteLine($"解析后的周次: {string.Join(", ", existingWeeks)}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"解析现有课程周次失败: {ex.Message}, 默认为第1周");
                                existingWeeks = new System.Collections.Generic.HashSet<int> { 1 };
                            }

                            // 检查周次重叠
                            bool hasWeekOverlap = existingWeeks.Intersect(newCourseWeeks).Any();
                            Console.WriteLine($"周次是否重叠: {hasWeekOverlap}");

                            if (hasWeekOverlap)
                            {
                                Console.WriteLine($"添加冲突课程: {existingCourse.Name}");
                                // 添加到冲突列表
                                conflicts.Add(new Course
                                {
                                    Name = existingCourse.Name,
                                    DayOfWeek = GetDayOfWeekString(dayIndex),
                                    StartTime = existingCourse.StartTime.Value,
                                    EndTime = existingCourse.EndTime.Value,
                                    Location = ExtractLocationFromNote(existingCourse.Note),
                                    Teacher = ExtractTeacherFromNote(existingCourse.Note),
                                    WeekPattern = existingWeekPattern
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查课程冲突时出错: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            Console.WriteLine($"检测到 {conflicts.Count} 个冲突课程");
            return (conflicts.Count > 0, conflicts);
        }



        /// <summary>
        /// 检查课程列表是否存在冲突
        /// </summary>
        /// <param name="courses">要检查的课程列表</param>
        /// <returns>冲突列表</returns>
        private async Task<List<(Course course, List<Course> conflicts)>> CheckCoursesConflicts(List<Course> courses)
        {
            var result = new List<(Course course, List<Course> conflicts)>();

            Console.WriteLine($"===== 开始检测课程冲突 =====");

            // 检查每个课程是否与现有课程冲突
            foreach (var course in courses)
            {
                var (hasConflict, conflicts) = await CheckCourseConflict(course);
                if (hasConflict)
                {
                    result.Add((course, conflicts));
                    Console.WriteLine($"课程 {course.Name} 与现有课程存在冲突");
                }
            }

            // 调用修改后的内部冲突检测方法
            var internalConflicts = await CheckCoursesInternalConflicts(courses);

            // 合并结果（确保不重复）
            foreach (var (course, conflicts) in internalConflicts)
            {
                bool found = false;
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].course == course)
                    {
                        // 添加新的冲突，避免重复
                        foreach (var conflict in conflicts)
                        {
                            if (!result[i].conflicts.Contains(conflict))
                            {
                                result[i].conflicts.Add(conflict);
                            }
                        }
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    result.Add((course, conflicts));
                }
            }

            Console.WriteLine($"冲突检测结果: 共找到 {result.Count} 个课程存在冲突");
            Console.WriteLine($"===== 课程冲突检测结束 =====");

            return result;
        }

        /// <summary>
        /// 从注释中提取周次模式
        /// </summary>
        private string ExtractWeekPatternFromNote(string note)
        {
            if (string.IsNullOrEmpty(note))
                return "1";  // 默认值

            // 标准格式："教师:XX, 地点:XX，周次:1,3,5-10"
            int weekIndex = note.IndexOf("周次:");
            if (weekIndex >= 0)
            {
                // 提取周次后面的部分
                string pattern = note.Substring(weekIndex + 3).Trim();

                // 如果有其他文本跟在后面，只保留周次部分
                char[] delimiters = new char[] { '，', ',', ' ', '、' };
                foreach (var delimiter in delimiters)
                {
                    int delimiterIndex = pattern.IndexOf(delimiter);
                    if (delimiterIndex >= 0)
                        pattern = pattern.Substring(0, delimiterIndex);
                }

                return pattern;
            }

            // 直接搜索数字模式：1,3,5-10
            var match = System.Text.RegularExpressions.Regex.Match(note, @"(\d+(-\d+)?)(,\d+(-\d+)?)*");
            if (match.Success)
            {
                return match.Value;
            }

            return "1";  // 默认值
        }

        /// <summary>
        /// 从注释中提取教师信息
        /// </summary>
        private string ExtractTeacherFromNote(string note)
        {
            if (string.IsNullOrEmpty(note))
                return "";

            int teacherIndex = note.IndexOf("教师:");
            if (teacherIndex >= 0)
            {
                string teacher = note.Substring(teacherIndex + 3);
                int endIndex = teacher.IndexOf(',');
                if (endIndex < 0) endIndex = teacher.IndexOf('，');
                return endIndex >= 0 ? teacher.Substring(0, endIndex).Trim() : teacher.Trim();
            }
            return "";
        }

        /// <summary>
        /// 从注释中提取地点信息
        /// </summary>
        private string ExtractLocationFromNote(string note)
        {
            if (string.IsNullOrEmpty(note))
                return "";

            int locationIndex = note.IndexOf("地点:");
            if (locationIndex >= 0)
            {
                string location = note.Substring(locationIndex + 3);
                int endIndex = location.IndexOf(',');
                if (endIndex < 0) endIndex = location.IndexOf('，');
                return endIndex >= 0 ? location.Substring(0, endIndex).Trim() : location.Trim();
            }
            return "";
        }

        /// <summary>
        /// 获取星期几索引 (0=周一, 1=周二...)
        /// </summary>
        private int GetDayIndex(string dayOfWeek)
        {
            return dayOfWeek.Trim() switch
            {
                "周一" or "星期一" or "1" or "一" => 0,
                "周二" or "星期二" or "2" or "二" => 1,
                "周三" or "星期三" or "3" or "三" => 2,
                "周四" or "星期四" or "4" or "四" => 3,
                "周五" or "星期五" or "5" or "五" => 4,
                "周六" or "星期六" or "6" or "六" => 5,
                "周日" or "星期日" or "周天" or "星期天" or "7" or "日" or "天" => 6,
                _ => 0
            };
        }

        /// <summary>
        /// 根据索引获取星期几字符串
        /// </summary>
        private string GetDayOfWeekString(int dayIndex)
        {
            return dayIndex switch
            {
                0 => "周一",
                1 => "周二",
                2 => "周三",
                3 => "周四",
                4 => "周五",
                5 => "周六",
                6 => "周日",
                _ => "周一"
            };
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
                // 输出课程信息供调试
                Console.WriteLine($"手动添加课程: {course.Name}, 星期: {course.DayOfWeek}, 时间: {course.StartTime}-{course.EndTime}, 周次: {course.WeekPattern}");

                // 检查课程冲突
                var (hasConflict, conflictCourses) = await CheckCourseConflict(course);

                if (hasConflict)
                {
                    // 构建冲突信息
                    var conflictNames = string.Join("\n- ", conflictCourses.Select(c =>
                        $"{c.Name} ({c.DayOfWeek} {c.StartTime.ToString(@"hh\:mm")}-{c.EndTime.ToString(@"hh\:mm")})"));

                    Console.WriteLine($"检测到冲突课程:\n{conflictNames}");

                    // 显示冲突警告，并提供替换选项
                    var result = MessageBox.Show(
                        $"添加的课程与以下已有课程时间冲突:\n- {conflictNames}\n\n是否要替换已有课程？\n（是：删除冲突课程并添加新课程，否：不保存新课程）",
                        "课程时间冲突",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        // 删除冲突课程
                        Console.WriteLine("用户选择删除冲突课程");
                        await DeleteConflictCourses(conflictCourses);
                    }
                    else
                    {
                        // 用户选择不替换，取消保存
                        Console.WriteLine("用户取消添加课程");
                        ImportStatus = "添加已取消";
                        return false;
                    }
                }

                // 保存课程到数据库
                Console.WriteLine("开始保存新课程到数据库");
                await _dbService.SaveCourses(new List<Course> { course }, SemesterStartDate);
                Console.WriteLine("课程保存成功");

                // 更新导入状态
                ImportStatus = "已添加课程";

                // 标记已导入课程
                _hasImportedCourses = true;
                Console.WriteLine("标记已导入课程");

                // 触发事件通知已保存课程及开学日期
                Console.WriteLine("触发课程保存事件");
                CoursesSavedWithStartDate?.Invoke(SemesterStartDate);

                // 添加这一行，确保SemesterInfoUpdated事件也被触发
                Console.WriteLine("触发学期信息更新事件");
                SemesterInfoUpdated?.Invoke(SemesterStartDate, SemesterWeeks);

                return true;
            }
            catch (Exception ex)
            {
                // 发生异常时返回失败
                ImportStatus = $"添加课程失败: {ex.Message}";
                Console.WriteLine($"添加课程失败: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // 删除冲突课程
        private async Task DeleteConflictCourses(List<Course> conflictCourses)
        {
            try
            {
                // 获取所有课程任务
                var allCourseTasks = await _taskService.GetAllCourseTasksAsync();
                Console.WriteLine($"开始删除 {conflictCourses.Count} 个冲突课程，系统中共有 {allCourseTasks.Count} 个课程任务");

                int deletedCount = 0;
                // 遍历冲突课程列表
                foreach (var conflictCourse in conflictCourses)
                {
                    Console.WriteLine($"处理冲突课程: {conflictCourse.Name}, 星期: {conflictCourse.DayOfWeek}, 时间: {conflictCourse.StartTime}-{conflictCourse.EndTime}");

                    // 获取课程对应的星期几索引
                    int dayIndex = GetDayIndex(conflictCourse.DayOfWeek);

                    // 查找与冲突课程相关的所有任务
                    var tasksToDelete = allCourseTasks.Where(task =>
                        task.Name == conflictCourse.Name &&
                        task.WeekDay == dayIndex &&
                        task.StartTime.HasValue && task.EndTime.HasValue &&
                        // 使用相同的时间重叠检测逻辑
                        Math.Max(conflictCourse.StartTime.TotalMinutes, task.StartTime.Value.TotalMinutes) <
                        Math.Min(conflictCourse.EndTime.TotalMinutes, task.EndTime.Value.TotalMinutes)
                    ).ToList();

                    Console.WriteLine($"找到 {tasksToDelete.Count} 个匹配任务");

                    // 如果没有找到任务，尝试更宽松的匹配
                    if (tasksToDelete.Count == 0)
                    {
                        Console.WriteLine("尝试宽松匹配");
                        tasksToDelete = allCourseTasks.Where(task =>
                            task.Name == conflictCourse.Name &&
                            task.WeekDay == dayIndex &&
                            task.StartTime.HasValue && task.EndTime.HasValue).ToList();

                        Console.WriteLine($"宽松匹配找到 {tasksToDelete.Count} 个任务");
                    }

                    // 删除找到的任务
                    foreach (var task in tasksToDelete)
                    {
                        await _taskService.DeleteTaskAsync(task);
                        deletedCount++;
                        Console.WriteLine($"已删除冲突课程任务: {task.Name}, ID: {task.Id}");
                    }
                }

                Console.WriteLine($"成功删除了 {deletedCount} 个冲突课程任务");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除冲突课程时出错: {ex.Message}\n{ex.StackTrace}");
                throw;
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

            // 检查文件中课程间的冲突
            var internalConflicts = await CheckCoursesInternalConflicts(courses);
            if (internalConflicts.Count > 0)
            {
                // 构建冲突信息
                var conflictMessages = new List<string>();
                foreach (var (course, conflicts) in internalConflicts)
                {
                    var conflictDetails = conflicts.Select(c =>
                        $"  - {c.Name} ({c.DayOfWeek} {c.StartTime.ToString(@"hh\:mm")}-{c.EndTime.ToString(@"hh\:mm")})").ToList();

                    conflictMessages.Add($"导入文件中课程 \"{course.Name}\" 与其他导入课程冲突:\n{string.Join("\n", conflictDetails)}");
                }

                var conflictMessage = string.Join("\n\n", conflictMessages);

                // 显示内部冲突警告
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"导入文件中存在课程时间冲突:\n\n{conflictMessage}\n\n请修正文件后重新导入。",
                        "导入文件课程冲突",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    ImportStatus = "导入已取消";
                });
                return;
            }

            // 询问用户是否要替换所有现有课程
            bool replaceAllCourses = false;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var result = MessageBox.Show(
                $"是否要替换所有现有课程？\n\n选择\"是\"将删除所有现有课程，并导入新课程\n- 选择\"否\"将保留现有课程，仅添加新课程",
                "导入选项",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);


                replaceAllCourses = (result == MessageBoxResult.Yes);
            });

            // 如果选择替换所有课程，先删除所有现有课程
            if (replaceAllCourses)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ImportStatus = "正在删除现有课程...";
                });

                try
                {
                    // 获取所有现有课程
                    var existingCourses = await _taskService.GetAllCourseTasksAsync();

                    // 删除所有现有课程
                    using (var transaction = _taskService.BeginTransaction())
                    {
                        try
                        {
                            foreach (var courseTask in existingCourses)
                            {
                                await _taskService.DeleteTaskAsync(courseTask);
                            }
                            _taskService.CommitTransaction();

                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                ImportStatus = $"已删除 {existingCourses.Count} 门现有课程";
                            });
                        }
                        catch (Exception ex)
                        {
                            _taskService.RollbackTransaction();
                            throw new Exception($"删除现有课程失败: {ex.Message}", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"删除现有课程失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        ImportStatus = "导入失败";
                    });
                    return;
                }
            }
            else
            {
                // 如果不替换全部课程，则需要检查冲突
                var existingConflicts = await CheckCoursesExistingConflicts(courses);
                if (existingConflicts.Count > 0)
                {
                    // 构建冲突信息
                    var conflictMessages = new List<string>();
                    foreach (var (course, conflicts) in existingConflicts)
                    {
                        var conflictDetails = conflicts.Select(c =>
                            $"  - {c.Name} ({c.DayOfWeek} {c.StartTime.ToString(@"hh\:mm")}-{c.EndTime.ToString(@"hh\:mm")})").ToList();

                        conflictMessages.Add($"导入课程 \"{course.Name}\" 与系统中已有课程冲突:\n{string.Join("\n", conflictDetails)}");
                    }

                    var conflictMessage = string.Join("\n\n", conflictMessages);

                    // 显示冲突警告，并提供替换选项
                    bool replaceConflictingCourses = false;
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var result = MessageBox.Show(
                            $"发现课程时间冲突:\n\n{conflictMessage}\n\n是否要替换冲突的课程？\n（是：删除冲突课程并添加新课程，否：跳过冲突课程只添加无冲突的课程）",
                            "课程时间冲突",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        replaceConflictingCourses = (result == MessageBoxResult.Yes);
                    });

                    if (replaceConflictingCourses)
                    {
                        // 用户选择替换，删除所有冲突课程
                        Console.WriteLine("用户选择替换冲突课程");
                        foreach (var (course, conflicts) in existingConflicts)
                        {
                            Console.WriteLine($"处理课程 {course.Name} 的冲突");
                            await DeleteConflictCourses(conflicts);
                        }
                    }
                    else
                    {
                        // 用户选择不替换，从导入列表中移除冲突课程
                        Console.WriteLine("用户选择不替换冲突课程，将从导入列表中移除冲突课程");
                        var coursesToRemove = existingConflicts.Select(c => c.course).ToList();
                        int beforeCount = courses.Count;
                        courses.RemoveAll(c => coursesToRemove.Contains(c));
                        Console.WriteLine($"从导入列表中移除了 {beforeCount - courses.Count} 门冲突课程");

                        if (courses.Count == 0)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                MessageBox.Show("所有导入课程都与现有课程冲突，且您选择了不替换冲突课程，因此没有课程被导入。",
                                    "导入取消", MessageBoxButton.OK, MessageBoxImage.Information);
                                ImportStatus = "导入已取消";
                            });
                            return;
                        }
                    }
                }
            }

            // 保存有效课程
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ImportStatus = "正在保存课程...";
            });

            try
            {
                // 这部分是数据库操作，可以在后台线程执行
                await Task.Run(async () =>
                {
                    try
                    {
                        // 保存新课程
                        await _dbService.SaveCourses(courses, SemesterStartDate);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                });

                // 保存学期周数到全局设置
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

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    string successMessage = replaceAllCourses
                        ? $"成功替换所有课程！导入 {courses.Count} 门新课程。"
                        : $"成功导入 {courses.Count} 门课程！";

                    MessageBox.Show($"{successMessage}\n开学日期: {SemesterStartDate:yyyy年M月d日}\n学期周数: {SemesterWeeks}周",
                        "导入成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    ImportStatus = replaceAllCourses ? $"已替换并导入 {courses.Count} 门课程" : $"已导入 {courses.Count} 门课程";

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


        // 检查导入文件中的课程是否存在内部冲突
        private async Task<List<(Course course, List<Course> conflicts)>> CheckCoursesInternalConflicts(List<Course> courses)
        {
            var result = new List<(Course course, List<Course> conflicts)>();

            // 添加详细日志
            Console.WriteLine($"===== 开始检测内部课程冲突 =====");
            Console.WriteLine($"需要检查 {courses.Count} 门课程的内部冲突");

            // 检查列表内部的课程之间是否冲突
            for (int i = 0; i < courses.Count; i++)
            {
                for (int j = i + 1; j < courses.Count; j++)
                {
                    var course1 = courses[i];
                    var course2 = courses[j];

                    // 检查是否在同一天
                    if (course1.DayOfWeek == course2.DayOfWeek)
                    {
                        Console.WriteLine($"检查同一天的课程: {course1.Name} vs {course2.Name}, 星期: {course1.DayOfWeek}");

                        // 检查时间是否重叠 - 使用与CheckCourseConflict相同的逻辑
                        bool hasTimeOverlap = Math.Max(course1.StartTime.TotalMinutes, course2.StartTime.TotalMinutes) <
                                             Math.Min(course1.EndTime.TotalMinutes, course2.EndTime.TotalMinutes);

                        Console.WriteLine($"时间是否重叠: {hasTimeOverlap}");
                        Console.WriteLine($"  课程1: {course1.StartTime}-{course1.EndTime}");
                        Console.WriteLine($"  课程2: {course2.StartTime}-{course2.EndTime}");

                        if (hasTimeOverlap)
                        {
                            // 解析周次模式
                            HashSet<int> weeks1 = AddCourseViewModel.ParseWeekPattern(course1.WeekPattern);
                            HashSet<int> weeks2 = AddCourseViewModel.ParseWeekPattern(course2.WeekPattern);

                            Console.WriteLine($"周次1: [{string.Join(", ", weeks1)}]");
                            Console.WriteLine($"周次2: [{string.Join(", ", weeks2)}]");

                            // 检查周次重叠
                            bool hasWeekOverlap = weeks1.Intersect(weeks2).Any();
                            Console.WriteLine($"周次是否重叠: {hasWeekOverlap}");

                            if (hasWeekOverlap)
                            {
                                Console.WriteLine($"检测到内部冲突: {course1.Name} 与 {course2.Name}");

                                // 添加到冲突列表中(对course1)
                                bool found = false;
                                for (int k = 0; k < result.Count; k++)
                                {
                                    if (result[k].course == course1)
                                    {
                                        result[k].conflicts.Add(course2);
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    result.Add((course1, new List<Course> { course2 }));
                                }

                                // 添加到冲突列表中(对course2)
                                found = false;
                                for (int k = 0; k < result.Count; k++)
                                {
                                    if (result[k].course == course2)
                                    {
                                        result[k].conflicts.Add(course1);
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    result.Add((course2, new List<Course> { course1 }));
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"内部冲突检测结果: 找到 {result.Count} 个冲突");
            Console.WriteLine($"===== 内部冲突检测结束 =====");

            return result;
        }

        // 检查导入课程与现有课程的冲突
        private async Task<List<(Course course, List<Course> conflicts)>> CheckCoursesExistingConflicts(List<Course> courses)
        {
            var result = new List<(Course course, List<Course> conflicts)>();

            // 检查每个课程是否与现有课程冲突
            foreach (var course in courses)
            {
                var (hasConflict, conflicts) = await CheckCourseConflict(course);
                if (hasConflict)
                {
                    result.Add((course, conflicts));
                }
            }

            return result;
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
