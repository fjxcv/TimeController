using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;
using TimeController.Models;
using Microsoft.Extensions.DependencyInjection;
using TimeController.Services;
using TimeController.Helpers;
using System.Diagnostics;
using System.Windows.Data;
using System.Data;
using System.Windows;
using Microsoft.Windows.Input;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace TimeController.ViewModels
{
    public class WeekViewModel : INotifyPropertyChanged
    {
        private DateTime _currentDate;
        private readonly INavigationService _navService;
        private readonly ITaskService _taskService;
        public IEnumerable<TaskBlock> AllDayTaskBlocks => TaskBlocks.Where(t => t.IsAllDay);
        public IEnumerable<TaskBlock> TimedTaskBlocks => TaskBlocks.Where(t => !t.IsAllDay);

        public ITaskService TaskService => _taskService;
        public ObservableCollection<TaskModel> Tasks { get; set; } = new ObservableCollection<TaskModel>();
        public ObservableCollection<TaskBlock> TaskBlocks { get; } = new ObservableCollection<TaskBlock>();
        // 课程任务块集合
        public ObservableCollection<TaskBlock> CourseTaskBlocks { get; } = new ObservableCollection<TaskBlock>();
        public ObservableCollection<DateColumnViewModel> DateColumns { get; } = new ObservableCollection<DateColumnViewModel>();
        public ICommand ReviewCommand { get; }

        public event Action<TaskModel>? SaveRequested;
        // 添加一个确认删除的事件
        public event Func<TaskBlock, Task<bool>> DeleteConfirmationRequested;

        public ICommand RemoveTaskBlockCommand { get; private set; }
        public ICollectionView TimedTaskBlocksView { get; private set; }

        public WeekViewModel(ITaskService taskService, INavigationService navService)
        {
            _taskService = taskService;
            _navService = navService;

            ToggleColumnExpandCommand = new RelayCommand<int>(ToggleColumnExpand);

            //进入复盘
            ReviewCommand = new RelayCommand(_ =>
            {
                var frame = AppFrame.Instance;
                if (frame != null)
                {
                    _navService.NavigateTo(frame, "Everyday");
                }
            });

            Initialize();

        }
        public string MonthText { get; private set; }
        public string WeekText { get; private set; }

        public ICommand PreviousWeekCommand { get; private set; }
        public ICommand NextWeekCommand { get; private set; }
        public ICommand PreviousMonthCommand { get; private set; }
        public ICommand NextMonthCommand { get; private set; }
        public ICommand ToggleCompleteCommand { get; }

        public WeekViewModel()
              : this(
                // 从全局 ServiceProvider 拿到 ITaskService
                App.AppHost.Services.GetRequiredService<ITaskService>(),
                // 从全局 ServiceProvider 拿到 INavigationService
                App.AppHost.Services.GetRequiredService<INavigationService>())
        {
            // 这里不用再写其它逻辑，所有初始化都在主构造里完成
        }


        private void Initialize()
        {
            _currentDate = DateTime.Today;
            UpdateMonthText();
            UpdateWeekText();

            PreviousWeekCommand = new RelayCommand(_ => NavigateWeek(-7));
            NextWeekCommand = new RelayCommand(_ => NavigateWeek(7));
            PreviousMonthCommand = new RelayCommand(_ => NavigateMonth(-1));
            NextMonthCommand = new RelayCommand(_ => NavigateMonth(1));
            TimedTaskBlocksView = CollectionViewSource.GetDefaultView(TaskBlocks);
            TimedTaskBlocksView.Filter = obj => obj is TaskBlock block && !block.IsAllDay;

            SaveRequested += OnTaskSaved;
            RemoveTaskBlockCommand = new RelayCommand<TaskBlock>(RemoveTaskBlock);
            ToggleColumnExpandCommand = new RelayCommand<int>(ToggleColumnExpand);

            DateTime monday = GetCurrentWeekMonday();

            DateColumns.Clear();
            // 初始化日期列
            for (int i = 0; i < 7; i++)
            {
                DateColumns.Add(new DateColumnViewModel
                {
                    Index = i,
                    Date = monday.AddDays(i),
                    WeekDayText = GetWeekDayText(i),
                    AllDayTasks = AllDayTaskBlocksPerDay[i],
                    IsExpanded = ExpandedColumns[i]
                });
            }

            if (Properties.Settings.Default.SemesterWeeks > 0)
            {
                _semesterWeeks = Properties.Settings.Default.SemesterWeeks;
            }

            if (Properties.Settings.Default.SemesterStartDate > DateTime.MinValue)
            {
                SemesterStartDate = Properties.Settings.Default.SemesterStartDate;
                Debug.WriteLine($"[DEBUG] Properties.Settings.Default.SemesterStartDate={Properties.Settings.Default.SemesterStartDate}");
            }

            // 更新日期文本和月份状态
            UpdateDateColumns();

            // 先立即刷新一次学期周文本
            UpdateSemesterWeekText();

            // 如果服务可用，加载任务
            if (_taskService != null)
            {
                LoadTasksForCurrentWeek();
                // 异步检查是否有课程任务，如果有则再刷新一次（双保险）
                Task.Run(async () => {
                    try
                    {
                        var courseTasks = await _taskService.GetAllCourseTasksAsync();
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                            UpdateSemesterWeekText();
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"检查课程任务时出错: {ex.Message}");
                    }
                });
            }
        }

        // 检查特定列是否需要显示下拉按钮（全天任务数大于3个）
        public bool[] ShouldShowMoreButtonForColumn => Enumerable.Range(0, 7)
            .Select(i => AllDayTaskBlocksPerDay[i].Count > 2).ToArray();

        // 获取星期几对应的索引（0=周一）
        private string GetWeekDayText(int dayIndex)
        {
            switch (dayIndex)
            {
                case 0: return "一";
                case 1: return "二";
                case 2: return "三";
                case 3: return "四";
                case 4: return "五";
                case 5: return "六";
                case 6: return "日";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// 将课程转换为任务模型 - 根据重复规则创建
        /// </summary>
        private TaskModel ConvertCourseToTaskModel(Course course)
        {
            // 星期几对应的索引
            int dayIndex = GetDayIndex(course.DayOfWeek);

            // 创建任务模型的基础信息
            var taskModel = new TaskModel
            {
                Name = course.Name,
                Note = $"教师:{course.Teacher}, 地点:{course.Location}，周次:{course.WeekPattern}",
                Type = TaskType.学习学业,
                Mode = TaskMode.Strong,
                // 占位日期，实际会根据当前周动态计算
                PlannedDate = DateTime.Today,
                IsAllDay = false,
                StartTime = course.StartTime,
                EndTime = course.EndTime,
                Status = MyTaskStatus.Pending,
                IsReminderEnabled = true,
                CreatedAt = DateTime.Now,
                // 添加课程特殊标记
                IsCourseTask = true,
                WeekDay = dayIndex // 存储星期几(0=周一)
            };

            return taskModel;
        }


        /// <summary>
        /// 判断任务是否为课程（基于 Note 字段中是否包含周次模式信息）
        /// </summary>
        public bool IsCourseByNote(TaskModel task)
        {
            // 检查任务类型必须是学习学业
            if (task.Type != TaskType.学习学业) return false;

            // 检查 Note 字段是否为空
            if (string.IsNullOrEmpty(task.Note)) return false;

            // 检查 Note 字段是否包含教师、地点和周次信息
            return task.Note.Contains("教师:") &&
                   task.Note.Contains("地点:") &&
                   task.Note.Contains("周次:");
        }

        /// <summary>
        /// 一步完成课程的添加和保存
        /// </summary>
        public async Task<List<TaskModel>> AddAndSaveCourse(Course course)
        {
            // 先检查是否与数据库中的所有课程冲突
            var (hasConflict, conflictCourses) = await CheckCourseTimeConflictsWithDatabase(course);

            // 如果有冲突，抛出异常并返回冲突信息
            if (hasConflict)
            {
                var conflictNames = string.Join("\n- ", conflictCourses.Select(c => c.Name));
                throw new InvalidOperationException($"新课程与以下已有课程时间冲突:\n- {conflictNames}\n\n请修改上课时间以避免冲突。");
            }

            // 1. 转换为任务模型
            TaskModel taskModel = ConvertCourseToTaskModel(course);

            // 2. 保存到数据库
            if (_taskService != null)
            {
                await _taskService.UpdateTaskAsync(taskModel);
                Console.WriteLine($"课程 {course.Name} 已保存到数据库，ID={taskModel.Id}");
            }

            // 3. 添加到任务列表
            Tasks.Add(taskModel);

            // 4. 刷新当前视图
            LoadTasksForCurrentWeek();

            // 5. 返回包含单个任务模型的列表
            return new List<TaskModel> { taskModel };
        }

        /// <summary>
        /// 检查新添加的课程是否与数据库中所有课程冲突
        /// </summary>
        public async Task<(bool hasConflict, List<Course> conflicts)> CheckCourseTimeConflictsWithDatabase(Course newCourse)
        {
            var conflicts = new List<Course>();

            try
            {
                // 添加详细日志
                Debug.WriteLine($"===== 开始检测课程冲突 =====");
                Debug.WriteLine($"新课程: {newCourse.Name}, 星期: {newCourse.DayOfWeek}, 时间: {newCourse.StartTime}-{newCourse.EndTime}, 周次: {newCourse.WeekPattern}");

                // 只有当课程有有效的时间范围时才检查冲突
                if (newCourse.StartTime < newCourse.EndTime)
                {
                    // 获取课程对应的星期几索引
                    int dayIndex = GetDayIndex(newCourse.DayOfWeek);
                    Debug.WriteLine($"课程对应的星期索引: {dayIndex}");

                    // 从数据库获取所有课程任务
                    var allCourses = await _taskService.GetAllCourseTasksAsync();
                    Debug.WriteLine($"数据库中共找到 {allCourses.Count} 门课程");

                    // 预先解析新课程的周次模式
                    HashSet<int> newCourseWeeks;
                    try
                    {
                        newCourseWeeks = ParseWeekPattern(newCourse.WeekPattern);
                        Debug.WriteLine($"新课程周次: [{string.Join(", ", newCourseWeeks)}]");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"解析新课程周次时出错: {ex.Message}，假设所有周都有课");
                        newCourseWeeks = new HashSet<int>(Enumerable.Range(1, SemesterWeeks)); // 默认为所有周
                    }

                    // 检查所有课程任务
                    int courseCount = 0;
                    foreach (var courseTask in allCourses)
                    {
                        courseCount++;
                        // 只检查同一天的课程
                        if (courseTask.WeekDay == dayIndex)
                        {
                            Debug.WriteLine($"检查第 {courseCount} 门课程: {courseTask.Name}, WeekDay: {courseTask.WeekDay}, 时间: {courseTask.StartTime?.ToString() ?? "无"}-{courseTask.EndTime?.ToString() ?? "无"}");

                            // 跳过无效时间段
                            if (!courseTask.StartTime.HasValue || !courseTask.EndTime.HasValue)
                            {
                                Debug.WriteLine($"跳过: 课程 {courseTask.Name} 时间未设置");
                                continue;
                            }

                            if (courseTask.StartTime >= courseTask.EndTime)
                            {
                                Debug.WriteLine($"跳过: 课程 {courseTask.Name} 时间段无效");
                                continue;
                            }

                            // 时间区间有重叠 - 核心判断逻辑修正
                            bool hasTimeOverlap = Math.Max(newCourse.StartTime.TotalMinutes, courseTask.StartTime.Value.TotalMinutes) <
                                                 Math.Min(newCourse.EndTime.TotalMinutes, courseTask.EndTime.Value.TotalMinutes);

                            Debug.WriteLine($"时间是否重叠: {hasTimeOverlap}");
                            Debug.WriteLine($"  新课程: {newCourse.StartTime}-{newCourse.EndTime}");
                            Debug.WriteLine($"  已有课程: {courseTask.StartTime}-{courseTask.EndTime}");

                            if (hasTimeOverlap)
                            {
                                // 从课程注释中提取周次模式
                                string existingWeekPattern = ExtractWeekPatternFromNote(courseTask.Note);
                                Debug.WriteLine($"现有课程 {courseTask.Name} 的周次模式: {existingWeekPattern}");

                                // 解析现有课程的周次
                                HashSet<int> existingWeeks;
                                try
                                {
                                    existingWeeks = ParseWeekPattern(existingWeekPattern);
                                    Debug.WriteLine($"现有课程周次: [{string.Join(", ", existingWeeks)}]");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"解析现有课程周次时出错: {ex.Message}，假设所有周都有课");
                                    // 解析失败时，假设所有周都有课
                                    existingWeeks = new HashSet<int>(Enumerable.Range(1, SemesterWeeks));
                                }

                                // 检查是否有周次重叠
                                bool hasWeekOverlap = existingWeeks.Any(week => newCourseWeeks.Contains(week));
                                Debug.WriteLine($"周次是否重叠: {hasWeekOverlap}");

                                if (hasWeekOverlap)
                                {
                                    // 将任务转换为Course对象以便返回冲突信息
                                    var course = new Course
                                    {
                                        Name = courseTask.Name,
                                        DayOfWeek = GetDayOfWeekString(courseTask.WeekDay),
                                        StartTime = courseTask.StartTime.Value,
                                        EndTime = courseTask.EndTime.Value,
                                        Location = ExtractLocationFromNote(courseTask.Note),
                                        Teacher = ExtractTeacherFromNote(courseTask.Note),
                                        WeekPattern = existingWeekPattern
                                    };

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
                }
                else
                {
                    Debug.WriteLine($"课程时间无效: {newCourse.StartTime} - {newCourse.EndTime}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"冲突检测过程中出错: {ex.Message}\n{ex.StackTrace}");
            }

            Debug.WriteLine($"课程冲突检测结果: 找到 {conflicts.Count} 个冲突");
            Debug.WriteLine($"===== 冲突检测结束 =====");

            return (conflicts.Count > 0, conflicts);
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

        /// <summary>
        /// 解析周次模式，例如"1,3,5-10"
        /// </summary>
        private HashSet<int> ParseWeekPattern(string pattern)
        {
            var weeks = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(pattern))
            {
                // 处理空模式
                return weeks;
            }

            try
            {
                var parts = pattern.Split(new char[] { ',', '，', ' ', '、' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    if (part.Contains('-'))
                    {
                        var range = part.Split('-');
                        if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                        {
                            // 确保范围合法
                            if (start > end)
                            {
                                int temp = start;
                                start = end;
                                end = temp;
                            }

                            // 添加范围内的所有周次
                            for (int i = start; i <= end; i++)
                            {
                                if (i > 0) // 确保周次有效
                                {
                                    weeks.Add(i);
                                }
                            }
                        }
                    }
                    else if (int.TryParse(part, out int week) && week > 0)
                    {
                        weeks.Add(week);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"解析周次模式时出错: {ex.Message}");
            }

            // 如果解析结果为空，返回默认周次1
            if (weeks.Count == 0)
            {
                weeks.Add(1);
            }

            return weeks;
        }


        /// <summary>
        /// 获取当前周的周一日期
        /// </summary>
        private DateTime GetCurrentWeekMonday()
        {
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
            {
                monday = monday.AddDays(-1);
            }
            return monday;
        }

        /// <summary>
        /// 获取星期几对应的索引 (0=周一, 1=周二...)
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

        //学期开始日
        private DateTime? _semesterStartDate;

        public DateTime? SemesterStartDate
        {
            get => _semesterStartDate;
            set
            {
                _semesterStartDate = value;
                OnPropertyChanged();

                // 保存到设置
                Properties.Settings.Default.SemesterStartDate = value ?? DateTime.MinValue;
                Properties.Settings.Default.Save();
            }
        }

        //学期周数
        private string _semesterWeekText = "";
        public string SemesterWeekText
        {
            get => _semesterWeekText;
            private set
            {
                if (_semesterWeekText != value)
                {
                    _semesterWeekText = value;
                    OnPropertyChanged();
                }
            }
        }

        // 更新学期周数
        public void UpdateSemesterWeekText()
        {
            Debug.WriteLine($"[DEBUG] UpdateSemesterWeekText called, _semesterStartDate={_semesterStartDate}");
            if (!_semesterStartDate.HasValue)
            {
                SemesterWeekText = "";
                return;
            }

            // 获取本周一
            DateTime currentMonday = CurrentDate;
            while (currentMonday.DayOfWeek != DayOfWeek.Monday)
            {
                currentMonday = currentMonday.AddDays(-1);
            }

            // 获取学期第一周的周一
            DateTime semesterMonday = _semesterStartDate.Value;
            while (semesterMonday.DayOfWeek != DayOfWeek.Monday)
            {
                semesterMonday = semesterMonday.AddDays(-1);
            }

            // 计算相差的周数
            int weeksDiff = (int)Math.Round((currentMonday - semesterMonday).TotalDays / 7) + 1;

            // 只有当前周在学期周数范围内且在学期开始日期之后时才显示学期周数
            if (weeksDiff > 0 && weeksDiff <= SemesterWeeks)
            {
                SemesterWeekText = $"学期第 {weeksDiff} 周";
            }
            else
            {
                SemesterWeekText = ""; // 如果超出学期周数或在学期开始日期之前，则不显示
            }
            Debug.WriteLine($"[DEBUG] SemesterWeekText set to: {SemesterWeekText}");
        }

        /// <summary>
        /// 检查编辑中的任务是否与现有任务或课程冲突（排除自身）
        /// </summary>
        /// <param name="editedTask">正在被编辑的任务</param>
        /// <returns>冲突检测结果与冲突列表</returns>
        public (bool hasConflict, List<TaskBlock> conflicts) CheckEditingTaskConflicts(TaskModel editedTask)
        {
            var conflicts = new List<TaskBlock>();

            // 如果是全天任务或没有设置时间，则不需要检查冲突
            if (editedTask.IsAllDay || !editedTask.StartTime.HasValue || !editedTask.EndTime.HasValue)
                return (false, conflicts);

            // 首先检查与其他任务冲突
            var (hasTaskConflict, taskConflicts) = CheckTimeConflicts(editedTask);

            // 然后检查与课程冲突
            var (hasCourseConflict, courseConflicts) = CheckCourseConflicts(editedTask);

            // 过滤掉任务自身（以ID为标识）
            var filteredTaskConflicts = taskConflicts.Where(t => t.Id != editedTask.Id).ToList();

            // 合并冲突列表
            conflicts.AddRange(filteredTaskConflicts);
            conflicts.AddRange(courseConflicts);

            Debug.WriteLine($"任务编辑冲突检测 - 任务: {editedTask.Name}, ID: {editedTask.Id}, 发现任务冲突: {filteredTaskConflicts.Count}, 课程冲突: {courseConflicts.Count}");

            return (conflicts.Count > 0, conflicts);
        }

        /// <summary>
        /// 处理特定的编辑任务冲突，如果有课程冲突则拒绝保存
        /// </summary>
        /// <param name="editedTask">编辑后的任务</param>
        /// <param name="conflicts">冲突列表</param>
        /// <returns>是否应继续保存任务</returns>
        public async Task<bool> HandleEditingTaskConflicts(TaskModel editedTask, List<TaskBlock> conflicts)
        {
            // 如果没有冲突，则可以继续保存
            if (conflicts.Count == 0)
                return true;

            // 分离课程冲突和任务冲突
            var courseConflicts = conflicts.Where(c => c.IsCourse).ToList();
            var taskConflicts = conflicts.Where(c => !c.IsCourse).ToList();

            // 如果有课程冲突，拒绝保存
            if (courseConflicts.Any())
            {
                // 编辑任务冲突不能覆盖课程，直接拒绝保存
                Debug.WriteLine($"编辑任务与课程冲突，拒绝保存: {editedTask.Name}, ID: {editedTask.Id}");
                return false;
            }

            // 对于任务冲突，按照原有逻辑处理（删除冲突任务并保存编辑后的任务）
            if (taskConflicts.Any())
            {
                using (_taskService?.BeginTransaction())
                {
                    try
                    {
                        await ProcessTaskConflicts(taskConflicts);
                        _taskService?.CommitTransaction();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _taskService?.RollbackTransaction();
                        HandleError(ex);
                        await ReloadTasks();
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 尝试更新编辑后的任务，如果与课程冲突则拒绝保存
        /// </summary>
        /// <param name="editedTask">编辑后的任务</param>
        /// <param name="originalTask">原始任务（用于在冲突时恢复）</param>
        /// <returns>是否成功保存</returns>
        public async Task<bool> TryUpdateEditedTask(TaskModel editedTask, TaskModel originalTask)
        {
            // 如果没有实际修改时间相关属性，直接保存
            if (editedTask.IsAllDay == originalTask.IsAllDay &&
                editedTask.StartTime == originalTask.StartTime &&
                editedTask.EndTime == originalTask.EndTime &&
                editedTask.PlannedDate == originalTask.PlannedDate)
            {
                // 直接保存非时间相关的修改
                await _taskService.UpdateTaskAsync(editedTask);
                return true;
            }

            // 检查冲突
            var (hasConflict, conflicts) = CheckEditingTaskConflicts(editedTask);

            // 分离课程冲突
            var courseConflicts = conflicts.Where(c => c.IsCourse).ToList();

            // 如果有课程冲突，拒绝保存
            if (courseConflicts.Any())
            {
                Debug.WriteLine($"编辑任务与课程冲突，拒绝保存: {editedTask.Name}, ID: {editedTask.Id}");

                // 恢复任务的原始时间属性
                editedTask.IsAllDay = originalTask.IsAllDay;
                editedTask.StartTime = originalTask.StartTime;
                editedTask.EndTime = originalTask.EndTime;
                editedTask.PlannedDate = originalTask.PlannedDate;

                // 如果用户修改了其他属性（如名称、备注等），仍然保存这些修改
                await _taskService.UpdateTaskAsync(editedTask);

                // 返回false表示时间修改未保存
                return false;
            }

            // 处理与其他任务的冲突
            var taskConflicts = conflicts.Where(c => !c.IsCourse).ToList();
            if (taskConflicts.Any())
            {
                // 对于其他任务冲突，可以选择删除冲突任务后保存
                using (_taskService?.BeginTransaction())
                {
                    try
                    {
                        await ProcessTaskConflicts(taskConflicts);
                        await _taskService.UpdateTaskAsync(editedTask);
                        _taskService?.CommitTransaction();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _taskService?.RollbackTransaction();
                        HandleError(ex);

                        // 恢复任务的原始时间属性
                        editedTask.IsAllDay = originalTask.IsAllDay;
                        editedTask.StartTime = originalTask.StartTime;
                        editedTask.EndTime = originalTask.EndTime;
                        editedTask.PlannedDate = originalTask.PlannedDate;

                        await ReloadTasks();
                        return false;
                    }
                }
            }

            // 无冲突，直接保存
            await _taskService.UpdateTaskAsync(editedTask);
            return true;
        }


        // 更新日期列的日期文本和月份状态
        private void UpdateDateColumns()
        {
            // 获取该周周一
            DateTime monday = GetCurrentWeekMonday();
            int currentMonth = CurrentDate.Month;

            for (int i = 0; i < 7; i++)
            {
                DateTime currentDay = monday.AddDays(i);
                var column = DateColumns[i];

                column.Date = currentDay;

                // 更新日期文本
                if (currentDay.Month != currentMonth)
                {
                    column.DateText = $"{currentDay.Month}月{currentDay.Day}";
                    column.IsCurrentMonth = false;
                }
                else
                {
                    column.DateText = currentDay.Day.ToString();
                    column.IsCurrentMonth = true;
                }
            }
        }

        // 在 CurrentDate 属性发生变化时调用此方法
        private void OnCurrentDateChanged()
        {
            UpdateDateColumns();
            UpdateSemesterWeekText();
            LoadTasksForCurrentWeek();
        }

        // 全天任务
        public ObservableCollection<TaskBlock>[] AllDayTaskBlocksPerDay { get; } =
    Enumerable.Range(0, 7).Select(_ => new ObservableCollection<TaskBlock>()).ToArray();

        // 添加保存任务块
        private void OnTaskSaved(TaskModel task)
        {
            // 只需调用 AddTask，所有定位和添加逻辑都在 AddTask 里完成
            AddTask(task);
        }

        // 添加任务到视图
        private void AddTaskToView(TaskModel task)
        {
            // 计算本周一
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            // 计算任务在本周的列（0=周一，1=周二...）
            int column = (task.PlannedDate - monday).Days;
            if (column < 0 || column > 6) return; // 不在当前周的任务不显示

            // 检查IsCourseTask属性
            if (task.IsCourseTask)
            {
                var courseBlock = new TaskBlock
                {
                    Name = task.Name,
                    Note = task.Note,
                    Type = task.Type,
                    StartTime = task.StartTime ?? TimeSpan.Zero,
                    EndTime = task.EndTime ?? TimeSpan.Zero,
                    // 设置蓝色背景
                    Brush = new SolidColorBrush(Color.FromRgb(204, 229, 255)),  // 浅蓝色背景 #CCE5FF
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)), // 蓝色边框 #0078D4
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Column = column,
                    Row = task.StartTime.HasValue ? task.StartTime.Value.Hours : 0,
                    RowSpan = (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
                              ? Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours) + 1 : 1,
                    Id = task.Id,
                    IsCourse = true
                };
                CourseTaskBlocks.Add(courseBlock);
            }
            else
            {
                var taskBlock = new TaskBlock
                {
                    Name = task.Name,
                    Note = task.Note,
                    Type = task.Type,
                    IsAllDay = task.IsAllDay,
                    StartTime = task.StartTime ?? TimeSpan.Zero,
                    EndTime = task.EndTime ?? TimeSpan.Zero,
                    Brush = GetBrushForTaskType(task.Type),
                    Column = column,
                    Row = task.StartTime.HasValue ? task.StartTime.Value.Hours : 0,
                    RowSpan = (!task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
                              ? Math.Max(1, (int)(task.EndTime.Value - task.StartTime.Value).TotalHours) + 1 : 1,
                    Id = task.Id,
                    IsCourse = false
                };
                TaskBlocks.Add(taskBlock);
            }
            OnPropertyChanged(nameof(AllDayTaskBlocks));
        }

        // 添加任务的事件，用于通知View层处理冲突
        public event Action<TaskModel, List<TaskBlock>>? ConflictDetected;

        // 添加任务方法，增加冲突处理逻辑
        public async void AddTask(TaskModel task, bool forceAdd = false)
        {
            // 非强制添加模式下，先检查时间冲突
            if (!forceAdd && !task.IsAllDay && task.StartTime.HasValue && task.EndTime.HasValue)
            {
                Console.WriteLine($"开始检查任务冲突: {task.Name}, 日期: {task.PlannedDate:yyyy-MM-dd}");

                // 检查与任务的冲突
                var (hasTaskConflict, taskConflicts) = CheckTimeConflicts(task);

                // 检查与课程的冲突
                var (hasCourseConflict, courseConflicts) = CheckCourseConflicts(task);

                // 合并冲突列表
                var allConflicts = taskConflicts.Concat(courseConflicts).ToList();

                if (allConflicts.Any())
                {
                    // 触发冲突事件，但不添加任务
                    if (ConflictDetected != null)
                    {
                        // 通知View层处理冲突并阻止添加
                        ConflictDetected(task, allConflicts);
                        return; // 不继续执行添加，直接返回
                    }
                }
            }

            // 无冲突或强制添加，执行正常的添加流程
            Tasks.Add(task);

            // 数据库持久化
            if (_taskService != null)
            {
                await _taskService.UpdateTaskAsync(task);
            }

            // 只有当任务在当前周才添加到视图
            DateTime monday = CurrentDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

            DateTime sunday = monday.AddDays(6);

            if (task.PlannedDate >= monday && task.PlannedDate <= sunday)
            {
                AddTaskToView(task);
                // 关键修改：手动添加全天任务到 AllDayTaskBlocksPerDay
                if (task.IsAllDay)
                {
                    int column = (task.PlannedDate - monday).Days;
                    if (column >= 0 && column < 7)
                    {
                        // 查找刚刚添加的任务块
                        var addedBlock = TaskBlocks.LastOrDefault(b => b.Id == task.Id);
                        if (addedBlock != null)
                        {
                            // 直接添加到对应天的全天任务集合
                            AllDayTaskBlocksPerDay[column].Add(addedBlock);
                            Console.WriteLine($"手动添加全天任务到第{column}列: {task.Name}");
                        }
                    }
                }
            }
            OnPropertyChanged(nameof(TimedTaskBlocks));
            OnPropertyChanged(nameof(AllDayTaskBlocks));
            OnPropertyChanged(nameof(AllDayTaskBlocksPerDay));

            // 更新对应日期列
            if (task.IsAllDay)
            {
                int column = (task.PlannedDate - monday).Days;
                if (column >= 0 && column < 7)
                {
                    DateColumns[column].RefreshAllDayTasksView();
                    OnPropertyChanged(nameof(ShouldShowMoreButtonForColumn));
                }
            }
            // 确保 TimedTaskBlocksView 也被刷新
            TimedTaskBlocksView.Refresh();
        }

        // 处理任务与任务冲突、课程与任务冲突
        public async Task HandleConflictAndAddTask(TaskModel newTask, List<TaskBlock> conflicts)
        {
            using (_taskService?.BeginTransaction())
            {
                try
                {
                    var taskConflicts = conflicts.Where(c => !c.IsCourse).ToList();
                    var courseConflicts = conflicts.Where(c => c.IsCourse).ToList();

                    await ProcessTaskConflicts(taskConflicts);
                    ProcessCourseConflicts(newTask, courseConflicts);
                    await AddNewTaskWithRefresh(newTask);

                    _taskService?.CommitTransaction();
                }
                catch (Exception ex)
                {
                    _taskService?.RollbackTransaction();
                    HandleError(ex);
                    await ReloadTasks();
                }
            }
        }

        // 添加错误处理方法
        private void HandleError(Exception ex)
        {
            Console.WriteLine($"处理冲突时出错: {ex.Message}\n{ex.StackTrace}");
            MessageBox.Show($"处理冲突时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 添加任务重新加载方法
        private async Task ReloadTasks()
        {
            await Task.Delay(100); // 短暂延迟以确保数据库操作完成
            LoadTasksForCurrentWeek();
        }

        // 处理任务与任务冲突
        private async Task ProcessTaskConflicts(List<TaskBlock> conflicts)
        {
            foreach (var conflict in conflicts)
            {
                var removed = TaskBlocks.FirstOrDefault(b => b.Id == conflict.Id);
                if (removed != null) TaskBlocks.Remove(removed);

                if (conflict.IsAllDay && conflict.Column >= 0 && conflict.Column < 7)
                {
                    var allDayList = AllDayTaskBlocksPerDay[conflict.Column];
                    var toRemove = allDayList.FirstOrDefault(b => b.Id == conflict.Id);
                    if (toRemove != null) allDayList.Remove(toRemove);
                }

                var taskToRemove = Tasks.FirstOrDefault(t => t.Id == conflict.Id);
                if (taskToRemove != null && _taskService != null)
                {
                    Tasks.Remove(taskToRemove);
                    await _taskService.DeleteTaskAsync(taskToRemove);
                    Debug.WriteLine($"Deleted conflicting task: {taskToRemove.Id}");
                }
            }
        }

        // 处理课程与任务冲突
        private void ProcessCourseConflicts(TaskModel newTask, List<TaskBlock> conflicts)
        {
            if (conflicts.Any())
            {
                var conflictNames = string.Join(", ", conflicts.Select(c => c.Name));
                newTask.Note = $"{(newTask.Note ?? "").Trim()}\n[与课程冲突: {conflictNames}]";
                Debug.WriteLine($"Course conflicts detected: {conflictNames}");
            }
        }

        // 添加任务并刷新UI
        private async Task AddNewTaskWithRefresh(TaskModel task)
        {
            Tasks.Add(task);
            if (_taskService != null) await _taskService.UpdateTaskAsync(task);

            // 刷新UI
            var monday = GetCurrentWeekMonday();
            if (task.PlannedDate >= monday && task.PlannedDate <= monday.AddDays(6))
            {
                AddTaskToView(task);

                if (task.IsAllDay)
                {
                    int col = (task.PlannedDate - monday).Days;
                    if (col >= 0 && col < 7)
                    {
                        var addedBlock = TaskBlocks.FirstOrDefault(b => b.Id == task.Id);
                        if (addedBlock != null)
                        {
                            AllDayTaskBlocksPerDay[col].Add(addedBlock);
                            DateColumns[col].RefreshAllDayTasksView();
                        }
                    }
                }
            }

            TimedTaskBlocksView.Refresh();
            OnPropertyChanged(nameof(AllDayTaskBlocks));
            OnPropertyChanged(nameof(ShouldShowMoreButtonForColumn));
        }

        // 检查与课程的冲突
        public (bool hasConflict, List<TaskBlock> conflicts) CheckCourseConflicts(TaskModel newTask)
        {
            var conflicts = new List<TaskBlock>();

            if (newTask.StartTime.HasValue && newTask.EndTime.HasValue && newTask.StartTime.Value < newTask.EndTime.Value)
            {
                // 计算本周一
                DateTime monday = CurrentDate.Date;
                while (monday.DayOfWeek != DayOfWeek.Monday)
                    monday = monday.AddDays(-1);

                int column = (newTask.PlannedDate - monday).Days;

                // 只检查有效列范围
                if (column >= 0 && column <= 6)
                {
                    // 添加调试日志
                    Console.WriteLine($"检查课程冲突 - 任务: {newTask.Name}, 日期: {newTask.PlannedDate:yyyy-MM-dd}, 时间: {newTask.StartTime} - {newTask.EndTime}");

                    // 检查课程冲突
                    foreach (var block in CourseTaskBlocks)
                    {
                        if (block.Column == column)
                        {
                            // 跳过无效时间段
                            if (block.StartTime >= block.EndTime)
                            {
                                Console.WriteLine($"跳过无效时间课程: {block.Name}, 时间: {block.StartTime} - {block.EndTime}");
                                continue;
                            }

                            // 时间区间有重叠
                            bool hasOverlap = !(newTask.EndTime.Value <= block.StartTime || newTask.StartTime.Value >= block.EndTime);

                            if (hasOverlap)
                            {
                                Console.WriteLine($"检测到冲突课程: {block.Name}, 时间: {block.StartTime} - {block.EndTime}");
                                conflicts.Add(block);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"课程冲突检测结果: 找到 {conflicts.Count} 个冲突");
            return (conflicts.Count > 0, conflicts);
        }

        private Brush GetBrushForTaskType(TaskType type)
        {
            switch (type)
            {
                case TaskType.学习学业:
                    return Brushes.LightBlue;
                case TaskType.自我提升:
                    return Brushes.LightGreen;
                case TaskType.项目实践任务:
                    return Brushes.LightPink;
                case TaskType.其它:
                    return Brushes.LightYellow;
                case TaskType.未分类:
                    return Brushes.MediumPurple;
                default:
                    return Brushes.LightGray;
            }
        }

        //任务块类
        public class TaskBlock : INotifyPropertyChanged
        {
            public string Name { get; set; }
            public string Note { get; set; }
            public TaskType Type { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
            public Brush Brush { get; set; }
            public bool IsAllDay { get; set; }
            public bool IsCourse { get; set; }
            public Brush BorderBrush { get; set; }
            public Thickness BorderThickness { get; set; } = new Thickness(1);
            public CornerRadius CornerRadius { get; set; } = new CornerRadius(4);

            // 定位属性
            public int Column { get; set; } // 星期几（0=周一，1=周二...）
            public int Row { get; set; }    // 对应小时行（0=全天，1=0:00, 2=1:00...）
            public int RowSpan { get; set; } // 跨多少小时

            public int Id { get; set; }//Id

            private MyTaskStatus _status;
            public MyTaskStatus Status
            {
                get => _status;
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }


        //当前日期
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (_currentDate != value)
                {
                    _currentDate = value;
                    OnPropertyChanged();
                    UpdateMonthText();
                    UpdateWeekText();
                    OnCurrentDateChanged();
                }
            }
        }

        private int _semesterWeeks = 18; // 默认值
        public int SemesterWeeks
        {
            get => _semesterWeeks;
            set
            {
                if (_semesterWeeks != value && value >= 1)
                {
                    _semesterWeeks = value;
                    OnPropertyChanged();

                    // 保存到设置
                    Properties.Settings.Default.SemesterWeeks = value;
                    Properties.Settings.Default.Save();

                    // 立即更新学期周文本
                    UpdateSemesterWeekText();
                }
            }
        }

        //删除课程和任务
        private async void RemoveTaskBlock(TaskBlock block)
        {
            if (block == null) return;

            try
            {
                // 请求确认
                if (DeleteConfirmationRequested != null)
                {
                    bool confirmed = await DeleteConfirmationRequested(block);
                    if (!confirmed)
                        return; // 用户取消删除
                }

                Console.WriteLine($"开始删除任务: {block.Name}, ID: {block.Id}, 类型: {(block.IsAllDay ? "全天任务" : "分时任务")}");

                // 1. 首先找到对应的 TaskModel，确保有正确的引用
                var taskToDelete = Tasks.FirstOrDefault(t => t.Id == block.Id);
                if (taskToDelete == null)
                {
                    Console.WriteLine($"警告：找不到ID为 {block.Id} 的任务模型，无法删除");

                    // 如果内存中找不到，尝试强制从数据库删除（确保ID匹配）
                    if (_taskService != null && block.Id > 0)
                    {
                        try
                        {
                            // 创建一个临时任务对象用于删除
                            var tempTask = new TaskModel { Id = block.Id };
                            await _taskService.DeleteTaskAsync(tempTask);
                            Console.WriteLine($"已尝试强制从数据库删除ID={block.Id}的任务");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"强制删除失败: {ex.Message}");
                        }
                    }

                    // 无论如何，继续UI更新
                }
                else
                {
                    // 2. 从数据库删除任务
                    if (_taskService != null)
                    {
                        try
                        {
                            // 判断是否为课程任务，如果是，则询问是否删除所有周次
                            if (block.IsCourse)
                            {
                                var result = MessageBox.Show(
                                    $"是否删除课程 \"{block.Name}\" 的所有周次？\n\n选择\"是\"将删除所有周次的课程\n选择\"否\"仅删除当前周课程",
                                    "删除课程",
                                    MessageBoxButton.YesNoCancel,
                                    MessageBoxImage.Question);

                                if (result == MessageBoxResult.Cancel)
                                {
                                    // 用户取消删除操作
                                    return;
                                }
                                else if (result == MessageBoxResult.Yes)
                                {
                                    // 删除所有周次的课程
                                    Console.WriteLine($"删除所有周次的课程: {taskToDelete.Name}");
                                    await _taskService.DeleteAllCourseInstancesAsync(taskToDelete);
                                }
                                else // No
                                {
                                    // 仅删除当前周的课程
                                    Console.WriteLine($"仅删除当前周课程: {taskToDelete.Name}, ID: {taskToDelete.Id}");
                                    await _taskService.DeleteTaskAsync(taskToDelete);
                                }
                            }
                            else
                            {
                                // 普通任务直接删除
                                Console.WriteLine($"删除数据库任务 ID={taskToDelete.Id}, Name={taskToDelete.Name}");
                                await _taskService.DeleteTaskAsync(taskToDelete);
                            }

                            Console.WriteLine($"数据库删除任务成功：ID = {taskToDelete.Id}");
                        }
                        catch (Exception ex)
                        {
                            // 数据库删除失败时，显示错误信息并中断操作
                            Console.WriteLine($"数据库删除任务失败：{ex.Message}");
                            MessageBox.Show($"无法从数据库删除任务: {ex.Message}", "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
                            return; // 关键：如果数据库删除失败，不继续UI更新
                        }

                        // 3. 从内存集合中移除
                        Tasks.Remove(taskToDelete);
                        Console.WriteLine($"从内存集合移除任务：{taskToDelete.Name}，剩余任务数：{Tasks.Count}");
                    }
                    else
                    {
                        Console.WriteLine("错误: _taskService 为 null，无法从数据库删除任务");
                        MessageBox.Show("任务服务不可用，无法删除任务", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // 4. 从UI集合中移除相应的TaskBlock
                bool uiRemoved = false;

                if (block.IsCourse)
                {
                    uiRemoved = CourseTaskBlocks.Remove(block);
                    Console.WriteLine($"从课程集合移除: {uiRemoved}");
                }
                else if (block.IsAllDay)
                {
                    // 从全天任务集合中移除
                    int column = block.Column;
                    if (column >= 0 && column < 7)
                    {
                        bool removed = AllDayTaskBlocksPerDay[column].Remove(block);
                        Console.WriteLine($"从全天任务集合移除结果: {removed}");
                        uiRemoved = uiRemoved || removed;

                        // 更新该列的视图
                        DateColumns[column].RefreshAllDayTasksView();
                    }
                    // 确保从总TaskBlocks中也移除
                    uiRemoved = TaskBlocks.Remove(block) || uiRemoved;
                    Console.WriteLine($"从总任务集合移除结果: {uiRemoved}");
                }
                else
                {
                    // 普通分时任务
                    uiRemoved = TaskBlocks.Remove(block);
                    Console.WriteLine($"从分时任务集合移除结果: {uiRemoved}");
                }

                if (!uiRemoved)
                {
                    Console.WriteLine("警告: 任务从UI中移除失败");
                }

                // 5. 强制刷新所有相关视图
                OnPropertyChanged(nameof(AllDayTaskBlocks));
                OnPropertyChanged(nameof(AllDayTaskBlocksPerDay));
                OnPropertyChanged(nameof(ShouldShowMoreButtonForColumn));
                OnPropertyChanged(nameof(TimedTaskBlocks));
                TimedTaskBlocksView.Refresh();

                // 关键步骤：强制从数据库重新加载当前周任务，确保视图与数据库同步
                await Task.Delay(100); // 短暂延迟以确保数据库操作完成
                LoadTasksForCurrentWeek();

                Console.WriteLine("任务删除操作完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除任务过程中出错: {ex.Message}");
                MessageBox.Show($"删除任务时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // （课程）悬停进来：显示按钮
        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Grid g && g.FindName("ActionButtons") is UIElement btns)
                btns.Visibility = Visibility.Visible;
        }

        // （课程）悬停离开：隐藏按钮
        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Grid g && g.FindName("ActionButtons") is UIElement btns)
                btns.Visibility = Visibility.Collapsed;
        }


        // 修改 LoadTasksForCurrentWeek 方法，确保只加载数据库中的最新数据
        public async void LoadTasksForCurrentWeek()
        {
            try
            {
                // 清空
                TaskBlocks.Clear();
                CourseTaskBlocks.Clear();
                for (int i = 0; i < 7; i++)
                    AllDayTaskBlocksPerDay[i].Clear();

                // 周范围
                DateTime monday = GetCurrentWeekMonday();
                DateTime sunday = monday.AddDays(6);

                // 拉数据
                var weekTasks = (await _taskService.GetTasksForDateRange(monday, sunday))
                            .Where(t => t.Mode == TaskMode.Strong)
                            .ToList();
                var courseTasks = (await _taskService.GetCourseTasksForWeekAsync(CurrentDate))
                                    .Where(t => t.Mode == TaskMode.Strong)  // 课程一般也是 Strong
                                    .ToList();

                Tasks.Clear();
                foreach (var t in weekTasks.Concat(courseTasks))
                    Tasks.Add(t);

                // ① 先插入分时与课程任务
                foreach (var t in Tasks.Where(t => !t.IsAllDay))
                    AddTaskToView(t);

                // ② 再为每个全天 TaskModel 新建一个带 Brush 的 TaskBlock
                foreach (var t in Tasks.Where(t => t.IsAllDay))
                {
                    int col = (t.PlannedDate - monday).Days;
                    if (col < 0 || col > 6) continue;

                    var allDayBlock = new TaskBlock
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Note = t.Note,
                        Type = t.Type,
                        Status = t.Status,
                        IsAllDay = true,
                        Column = col,
                        Row = 0,
                        RowSpan = 1,
                        Brush = GetBrushForTaskType(t.Type)
                    };
                    AllDayTaskBlocksPerDay[col].Add(allDayBlock);
                }

                // ③ 刷新各列全天视图（触发折叠/展开）
                foreach (var colVm in DateColumns)
                    colVm.RefreshAllDayTasksView();

                // ④ 刷新 UI
                OnPropertyChanged(nameof(TaskBlocks));
                OnPropertyChanged(nameof(CourseTaskBlocks));
                OnPropertyChanged(nameof(AllDayTaskBlocksPerDay));
                OnPropertyChanged(nameof(ShouldShowMoreButtonForColumn));
                TimedTaskBlocksView.Refresh();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载任务时发生错误: {ex.Message}");
            }
        }

        // 检查分时任务时间冲突
        public (bool hasConflict, List<TaskBlock> conflicts) CheckTimeConflicts(TaskModel newTask)
        {
            var conflicts = new List<TaskBlock>();

            if (newTask.StartTime.HasValue && newTask.EndTime.HasValue && newTask.StartTime.Value < newTask.EndTime.Value)
            {
                // 计算本周一
                DateTime monday = CurrentDate.Date;
                while (monday.DayOfWeek != DayOfWeek.Monday)
                    monday = monday.AddDays(-1);

                int column = (newTask.PlannedDate - monday).Days;

                // 只检查有效列范围
                if (column >= 0 && column <= 6)
                {
                    // 添加调试日志
                    Console.WriteLine($"检查时间冲突 - 任务: {newTask.Name}, 日期: {newTask.PlannedDate:yyyy-MM-dd}, 时间: {newTask.StartTime} - {newTask.EndTime}");

                    // 检查同一天的分时任务是否有冲突
                    foreach (var block in TaskBlocks)
                    {
                        if (!block.IsAllDay && block.Column == column)
                        {
                            // 跳过无效时间段
                            if (block.StartTime >= block.EndTime)
                            {
                                Console.WriteLine($"跳过无效时间任务: {block.Name}, 时间: {block.StartTime} - {block.EndTime}");
                                continue;
                            }

                            // 时间区间有重叠
                            bool hasOverlap = !(newTask.EndTime.Value <= block.StartTime || newTask.StartTime.Value >= block.EndTime);

                            if (hasOverlap)
                            {
                                Console.WriteLine($"检测到冲突任务: {block.Name}, ID: {block.Id}, 时间: {block.StartTime} - {block.EndTime}");
                                conflicts.Add(block);
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"时间冲突检测结果: 找到 {conflicts.Count} 个冲突");
            return (conflicts.Count > 0, conflicts);
        }

        //导航栏周
        private void NavigateWeek(int offset)
        {
            CurrentDate = CurrentDate.AddDays(offset);
            LoadTasksForCurrentWeek();
        }

        //导航栏月
        private void NavigateMonth(int offset)
        {
            CurrentDate = CurrentDate.AddMonths(offset);
            LoadTasksForCurrentWeek();
        }

        // 获取当前日期所在的月份
        private void UpdateMonthText()
        {

            var month = _currentDate.Month;
            MonthText = $"{month}月份";
            OnPropertyChanged(nameof(MonthText));
        }

        // 计算月份中的周数
        private void UpdateWeekText()
        {
            // 获取当前月的第一天
            var firstDayOfMonth = new DateTime(_currentDate.Year, _currentDate.Month, 1);

            // 获取当前日期所在周的周一
            var currentMonday = CurrentDate.Date;
            while (currentMonday.DayOfWeek != DayOfWeek.Monday)
            {
                currentMonday = currentMonday.AddDays(-1);
            }

            // 获取本月第一个周一
            var firstMonday = firstDayOfMonth;
            // 如果月初不是周一，向后找到第一个周一
            while (firstMonday.DayOfWeek != DayOfWeek.Monday)
            {
                firstMonday = firstMonday.AddDays(1);
            }

            // 如果第一个周一比月初晚，且月初不是周一，那么第一周应该从上个月的某个周一开始
            // 所以需要判断第一天属于哪一周
            if (firstMonday > firstDayOfMonth && firstDayOfMonth.DayOfWeek != DayOfWeek.Monday)
            {
                firstMonday = firstMonday.AddDays(-7);
            }

            // 计算当前日期所在周是本月的第几周
            int weekOfMonth = ((currentMonday - firstMonday).Days / 7) + 1;

            // 如果当前周的周一在本月第一天之前，说明这是跨月周，月份数需要调整
            if (currentMonday < firstDayOfMonth)
            {
                weekOfMonth = 1;
            }

            WeekText = $"第{weekOfMonth}周";
            OnPropertyChanged(nameof(WeekText));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // 存储每天的展开状态
        public bool[] ExpandedColumns { get; set; } = new bool[7];

        // 获取特定列的全天任务数量
        public int GetAllDayTaskCountForColumn(int column)
        {
            return TaskBlocks.Count(t => t.IsAllDay && t.Column == column);
        }

        // 切换展开状态的命令
        public ICommand ToggleColumnExpandCommand { get; private set; }

        // 切换某一列的展开状态
        private void ToggleColumnExpand(int column)
        {
            if (column < 0 || column >= DateColumns.Count) return;

            // 切换展开状态
            DateColumns[column].IsExpanded = !DateColumns[column].IsExpanded;

            // 刷新全天任务视图过滤
            DateColumns[column].RefreshAllDayTasksView();

            // 通知界面更新“更多”按钮的可见性
            OnPropertyChanged(nameof(DateColumns));
        }


    }
}