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
                    MessageBox.Show("课程名称最多为15个字！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                // 添加调试信息
                Debug.WriteLine($"正在添加课程: {newCourse.Name}, 星期: {newCourse.DayOfWeek}, 时间: {newCourse.StartTime}-{newCourse.EndTime}");

                // 检查课程时间冲突
                var weekViewModel = App.AppHost.Services.GetRequiredService<WeekViewModel>();
                if (weekViewModel != null)
                {
                    // 强制重新加载所有课程数据，而不仅是当前周的
                    var taskService = App.AppHost.Services.GetRequiredService<ITaskService>();
                    if (taskService != null)
                    {
                        // 异步操作要等待结果
                        var allCourseTasks = Task.Run(async () => await taskService.GetAllCourseTasksAsync()).GetAwaiter().GetResult();

                        // 清除并重建课程任务块
                        weekViewModel.CourseTaskBlocks.Clear();
                        foreach (var task in allCourseTasks)
                        {
                            // 把课程添加到CourseTaskBlocks
                            var courseBlock = ConvertTaskModelToTaskBlock(task);
                            weekViewModel.CourseTaskBlocks.Add(courseBlock);
                        }

                        Debug.WriteLine($"强制加载后课程数量: {weekViewModel.CourseTaskBlocks.Count}");
                    }

                    // 打印调试信息：所有加载的课程
                    Debug.WriteLine("已加载的所有课程:");
                    foreach (var course in weekViewModel.CourseTaskBlocks)
                    {
                        Debug.WriteLine($"  - {course.Name}, 星期: {course.Column}, 时间: {course.StartTime}-{course.EndTime}, 备注: {course.Note}");
                    }

                    // 自行实现冲突检测（不依赖于 WeekViewModel.CheckCourseTimeConflicts）
                    var conflicts = CheckCourseConflicts(newCourse, weekViewModel.CourseTaskBlocks);

                    if (conflicts.Count > 0)
                    {
                        // 构建冲突信息
                        var conflictNames = string.Join("\n- ", conflicts.Select(c => c.Name));
                        Debug.WriteLine($"自行检测到冲突课程: {conflicts.Count} 个");
                        MessageBox.Show(
                            $"课程时间冲突：\n新课程与以下已有课程在时间上冲突:\n- {conflictNames}\n\n请修改上课时间以避免冲突。",
                            "课程时间冲突",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return; // 有冲突，不关闭窗口
                    }
                }

                // 无冲突，设置结果并关闭窗口
                ResultCourse = newCourse;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建课程时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
        }

        // 实现课程与课程冲突检测
        private List<WeekViewModel.TaskBlock> CheckCourseConflicts(Course newCourse, IEnumerable<WeekViewModel.TaskBlock> existingCourses)
        {
            var conflicts = new List<WeekViewModel.TaskBlock>();

            // 获取新课程对应的星期索引
            int dayIndex = GetDayIndex(newCourse.DayOfWeek);

            Debug.WriteLine($"检查课程冲突 - 新课程: {newCourse.Name}, 星期索引: {dayIndex}, 时间: {newCourse.StartTime}-{newCourse.EndTime}, 周次: {newCourse.WeekPattern}");

            // 预先解析新课程的周次模式
            HashSet<int> newCourseWeeks;
            try
            {
                newCourseWeeks = AddCourseViewModel.ParseWeekPattern(newCourse.WeekPattern);
                Debug.WriteLine($"新课程周次: [{string.Join(", ", newCourseWeeks)}]");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析新课程周次时出错: {ex.Message}");
                return conflicts; // 周次格式错误，返回空冲突列表
            }

            // 检查每个现有课程
            foreach (var course in existingCourses)
            {
                // 只检查同一天的课程
                if (course.Column == dayIndex)
                {
                    Debug.WriteLine($"比较现有课程: {course.Name}, 星期: {course.Column}, 时间: {course.StartTime}-{course.EndTime}");

                    // 时间区间有重叠的判断逻辑
                    bool hasTimeOverlap = !(newCourse.EndTime <= course.StartTime || newCourse.StartTime >= course.EndTime);

                    if (hasTimeOverlap)
                    {
                        Debug.WriteLine($"时间重叠，检查周次...");

                        // 从课程注释中提取周次模式
                        string existingWeekPattern = ExtractWeekPatternFromNote(course.Note);
                        Debug.WriteLine($"现有课程周次模式: {existingWeekPattern}");

                        // 解析现有课程的周次
                        HashSet<int> existingWeeks;
                        try
                        {
                            existingWeeks = AddCourseViewModel.ParseWeekPattern(existingWeekPattern);
                            Debug.WriteLine($"现有课程周次: [{string.Join(", ", existingWeeks)}]");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"解析现有课程周次时出错: {ex.Message}，假设所有周都有课");
                            // 解析失败时，假设所有周都有课
                            existingWeeks = new HashSet<int>(Enumerable.Range(1, 20)); // 假设最多20周
                        }

                        // 检查是否有周次重叠
                        bool hasWeekOverlap = newCourseWeeks.Overlaps(existingWeeks);
                        Debug.WriteLine($"周次是否重叠: {hasWeekOverlap}");

                        if (hasWeekOverlap)
                        {
                            Debug.WriteLine($"检测到冲突课程: {course.Name}");
                            conflicts.Add(course);
                        }
                        else
                        {
                            Debug.WriteLine($"时间重叠但周次不冲突，跳过");
                        }
                    }
                }
            }

            Debug.WriteLine($"冲突检测结果: 找到 {conflicts.Count} 个冲突");
            return conflicts;
        }

        private WeekViewModel.TaskBlock ConvertTaskModelToTaskBlock(TaskModel task)
        {
            return new WeekViewModel.TaskBlock
            {
                Id = task.Id,
                Name = task.Name,
                Note = task.Note,
                Type = task.Type,
                StartTime = task.StartTime ?? TimeSpan.Zero,
                EndTime = task.EndTime ?? TimeSpan.Zero,
                Column = task.WeekDay,
                Row = task.StartTime?.Hours ?? 0,
                RowSpan = (task.StartTime.HasValue && task.EndTime.HasValue)
                    ? Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours) + 1
                    : 1,
                IsCourse = true,
                Brush = new SolidColorBrush(Color.FromRgb(204, 229, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };
        }


        // 从课程注释中提取周次模式
        private string ExtractWeekPatternFromNote(string note)
        {
            if (string.IsNullOrEmpty(note))
                return "1";  // 默认值

            // 打印原始注释用于调试
            Debug.WriteLine($"提取周次模式自: {note}");

            // 标准格式："教师:XX, 地点:XX，周次:1,3,5-10"
            int weekIndex = note.IndexOf("周次:");
            if (weekIndex >= 0)
            {
                // 提取周次后面的部分
                string pattern = note.Substring(weekIndex + 3).Trim();

                // 查找第一个可能终止周次模式的字符
                int endIndex = -1;

                // 检查各种可能的分隔符位置
                foreach (var separator in new[] { '\n', '\r', ']', ')', '}', '，', '。' })
                {
                    int idx = pattern.IndexOf(separator);
                    if (idx >= 0 && (endIndex == -1 || idx < endIndex))
                    {
                        endIndex = idx;
                    }
                }

                // 如果找到了终止字符，截取到该位置
                if (endIndex >= 0)
                {
                    pattern = pattern.Substring(0, endIndex).Trim();
                }

                // 替换可能包含的中文逗号为英文逗号
                pattern = pattern.Replace('，', ',');

                Debug.WriteLine($"提取的周次模式: {pattern}");
                return pattern;
            }

            // 如果没有找到"周次:"标记，尝试匹配数字格式
            // 注意：这里的正则表达式应该只在没有找到"周次:"标记时使用
            // 避免从其他内容（如地址）中错误地提取数字
            if (!note.Contains("周次:"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(note, @"(\d+(-\d+)?)(,\d+(-\d+)?)*");
                if (match.Success && match.Index == 0) // 确保匹配从开头开始
                {
                    string pattern = match.Value;
                    Debug.WriteLine($"通过正则表达式提取的周次模式: {pattern}");
                    return pattern;
                }
            }

            Debug.WriteLine("未找到周次模式，使用默认值1");
            return "1";  // 默认值
        }

        // 获取星期几对应的索引 (0=周一, 1=周二...)
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
