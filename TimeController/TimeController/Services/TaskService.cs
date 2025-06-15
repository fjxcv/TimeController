using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using TimeController.Models;
using TimeController.ViewModels;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;


namespace TimeController.Services
{
    public class TaskService : ITaskService
    {
        private readonly TaskDbContext _context;
        private IDbContextTransaction? _currentTransaction; 
        public TaskService(TaskDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskModel>> GetAllCourseTasksAsync()
        {
            // 从数据库中获取所有 IsCourseTask = true 的任务
            return await _context.Tasks
                .Where(t => t.IsCourseTask)
                .ToListAsync();
        }


        // 按周获取课程
        public async Task<List<TaskModel>> GetCourseTasksForWeekAsync(DateTime referenceDate)
        {
            try
            {
                // 获取学期第一周的周一（如果有设置）
                DateTime? semesterStartDate = await GetSemesterStartDateAsync();
                if (!semesterStartDate.HasValue)
                {
                    Console.WriteLine("无法获取学期开始日期，无法计算当前周次");
                    return new List<TaskModel>();
                }

                // 获取学期第一周的周一
                DateTime semesterFirstMonday = semesterStartDate.Value;
                while (semesterFirstMonday.DayOfWeek != DayOfWeek.Monday)
                {
                    semesterFirstMonday = semesterFirstMonday.AddDays(-1);
                }

                // 计算当前是学期第几周
                DateTime currentWeekMonday = referenceDate.Date;
                while (currentWeekMonday.DayOfWeek != DayOfWeek.Monday)
                {
                    currentWeekMonday = currentWeekMonday.AddDays(-1);
                }

                // 修改这里，使用 Ceiling 而不是 Round 来确保计算正确
                int currentWeekNumber = (int)Math.Ceiling((currentWeekMonday - semesterFirstMonday).TotalDays / 7.0) + 1;

                // 如果结果小于1，默认为第1周
                currentWeekNumber = Math.Max(1, currentWeekNumber);

                Console.WriteLine($"学期开始日期: {semesterFirstMonday:yyyy-MM-dd}, 当前周一: {currentWeekMonday:yyyy-MM-dd}, 当前是学期第 {currentWeekNumber} 周");

                // 获取所有课程任务
                var allCourseTasks = await GetAllCourseTasksAsync();
                Console.WriteLine($"总共获取到 {allCourseTasks.Count} 个课程任务");

                // 筛选出应该在当前周显示的课程
                var currentWeekCourses = new List<TaskModel>();

                foreach (var task in allCourseTasks)
                {
                    // 从Note中提取周次模式
                    string weekPattern = null;
                    if (!string.IsNullOrEmpty(task.Note) && task.Note.Contains("周次模式:"))
                    {
                        int startIndex = task.Note.IndexOf("周次模式:") + "周次模式:".Length;
                        int endIndex = task.Note.IndexOf(",", startIndex);
                        if (endIndex == -1) // 如果是最后一部分
                            endIndex = task.Note.Length;

                        if (startIndex < endIndex)
                            weekPattern = task.Note.Substring(startIndex, endIndex - startIndex).Trim();
                    }

                    Console.WriteLine($"处理课程: {task.Name}, 周次模式: {weekPattern ?? "未设置"}");

                    // 如果没有周次模式，默认为第一周
                    if (string.IsNullOrEmpty(weekPattern))
                    {
                        if (currentWeekNumber == 1)
                        {
                            currentWeekCourses.Add(task);
                            Console.WriteLine($"课程 {task.Name} 没有周次模式，但当前是第1周，所以添加此课程");
                        }
                        else
                        {
                            Console.WriteLine($"课程 {task.Name} 没有周次模式，默认只在第1周显示，当前是第{currentWeekNumber}周，不添加");
                        }
                        continue;
                    }

                    try
                    {
                        // 解析周次模式
                        var weeks = AddCourseViewModel.ParseWeekPattern(weekPattern);
                        Console.WriteLine($"课程 {task.Name} 的周次模式 {weekPattern} 解析结果: {string.Join(",", weeks)}");

                        // 如果当前周在周次列表中，添加该课程
                        if (weeks.Contains(currentWeekNumber))
                        {
                            currentWeekCourses.Add(task);
                            Console.WriteLine($"当前第{currentWeekNumber}周在课程 {task.Name} 的周次列表中，添加此课程");
                        }
                        else
                        {
                            Console.WriteLine($"当前第{currentWeekNumber}周不在课程 {task.Name} 的周次列表中，不添加");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"解析周次模式 '{weekPattern}' 失败: {ex.Message}");
                        // 解析失败时，如果是第一周就添加
                        if (currentWeekNumber == 1)
                        {
                            currentWeekCourses.Add(task);
                            Console.WriteLine($"周次解析失败，默认在第1周显示");
                        }
                    }
                }

                Console.WriteLine($"本周共筛选出 {currentWeekCourses.Count} 个课程任务");
                return currentWeekCourses;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取本周课程任务时出错: {ex.Message}\n{ex.StackTrace}");
                return new List<TaskModel>();
            }
        }



        // 获取学期开始日期
        private async Task<DateTime?> GetSemesterStartDateAsync()
        {
            try
            {
                // 尝试从 App.Current.Properties 获取学期开始日期
                if (App.Current.Properties.Contains("SemesterStartDate"))
                {
                    var dateString = App.Current.Properties["SemesterStartDate"] as string;
                    if (DateTime.TryParse(dateString, out DateTime result))
                    {
                        Console.WriteLine($"从应用程序属性获取到学期开始日期: {result:yyyy-MM-dd}");
                        return result;
                    }
                }

                // 尝试从数据库中获取第一个课程任务的日期作为参考
                var firstCourseTask = await _context.Tasks
                    .Where(t => t.IsCourseTask)
                    .OrderBy(t => t.PlannedDate)
                    .FirstOrDefaultAsync();

                if (firstCourseTask != null)
                {
                    // 找到课程的实际日期，然后回推到学期开始的周一
                    DateTime date = firstCourseTask.PlannedDate;
                    // 根据星期几偏移回到该周的周一
                    DateTime monday = date.AddDays(-firstCourseTask.WeekDay);

                    Console.WriteLine($"从第一个课程任务推导学期开始日期: {monday:yyyy-MM-dd}");
                    return monday;
                }

                // 如果没有课程任务，返回当前日期
                Console.WriteLine("没有找到学期开始日期参考，使用当前日期");
                return DateTime.Today;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取学期开始日期时出错: {ex.Message}");
                // 如果出错，返回当前日期
                return DateTime.Today;
            }
        }

        public IDisposable BeginTransaction()
        {
            _currentTransaction = _context.Database.BeginTransaction();
            return _currentTransaction;
        }

        public void CommitTransaction()
        {
            _currentTransaction?.Commit();
            _currentTransaction = null;
        }

        public void RollbackTransaction()
        {
            _currentTransaction?.Rollback();
            _currentTransaction = null;
        }

        public async Task<List<TaskModel>> GetTasksForDate(DateTime date)
        {
            // 取出当天零点到次日零点之间的所有任务
            var start = date.Date;
            var end = start.AddDays(1);

            return await _context.Tasks
                .Where(t =>
                    t.PlannedDate >= start &&
                    t.PlannedDate < end
                )
                .ToListAsync();
        }

        public async Task DeleteTaskAsync(TaskModel task)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAllCourseInstancesAsync(TaskModel courseTask)
        {
            if (!courseTask.IsCourseTask)
                return; // 如果不是课程任务，则不执行操作

            // 从课程名称和Note（包含教师和地点信息）标识相同的课程
            string courseName = courseTask.Name;
            string courseNote = courseTask.Note;
            int weekDay = courseTask.WeekDay;

            // 查找所有拥有相同名称、Note和星期几的课程任务
            var allInstancesOfCourse = await _context.Task
                .Where(t => t.IsCourseTask
                       && t.Name == courseName
                       && t.Note == courseNote
                       && t.WeekDay == weekDay)
                .ToListAsync();

            Console.WriteLine($"找到 {allInstancesOfCourse.Count} 个相同的课程实例");

            // 从数据库中删除所有这些课程实例
            _context.Task.RemoveRange(allInstancesOfCourse);
            await _context.SaveChangesAsync();
        }


        public async Task<List<TaskModel>> GetTasksForDateRange(DateTime startDate, DateTime endDate)
        {
            var tasks = await _context.Tasks
                .Where(t => t.PlannedDate.Date >= startDate.Date && t.PlannedDate.Date <= endDate.Date)
                .ToListAsync();

            // 这里是调试代码，输出加载到的任务数量和状态
            Debug.WriteLine($"[周复盘] 加载到的任务数量: {tasks.Count}");
            foreach (var task in tasks)
            {
                Debug.WriteLine($"任务状态: {task.Name} - {task.Status}");
            }

            return await _context.Tasks
                .Where(t => t.PlannedDate.Date >= startDate.Date && t.PlannedDate.Date <= endDate.Date)
                .ToListAsync();
        }

        public Task<List<TaskModel>> GetAllTasksAsync()
        {
            return _context.Tasks.OrderByDescending(t => t.PlannedDate).ToListAsync();
        }

        //更新任务状态
        public async Task UpdateTaskAsync(TaskModel task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            //调试输出
            Debug.WriteLine($"[更新任务] {task.Name}, 状态={task.Status}, 计划时间={task.PlannedDate:yyyy-MM-dd}");
        }

        public async Task<IEnumerable<TaskModel>> GetAllPendingTasksAsync()
        {
            return await _context.Tasks
                .Where(t => t.Status == MyTaskStatus.Pending)
                .ToListAsync() ?? new List<TaskModel>(); // 保证不是 null
        }


        //种子数据
        public async Task ResetTaskDataAsync()
        {
            // 清空旧数据
            var all = await _context.Tasks.ToListAsync();
            _context.Tasks.RemoveRange(all);
            await _context.SaveChangesAsync();

            var today = DateTime.Today;
            var tasks = new List<TaskModel>
            {
                new TaskModel
                {
                    Name = "完成项目文档",
                    Status = MyTaskStatus.Completed,
                    PlannedDate = today,
                    IsAllDay = true
                },
                new TaskModel
                {
                    Name = "代码审查",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today,
                    IsAllDay = false,
                    StartTime = TimeSpan.FromHours(14),
                    EndTime = TimeSpan.FromHours(15),

                },
                new TaskModel
                {
                    Name = "项目进度汇报",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today.AddDays(-3),
                    IsAllDay = true
                },

                // 被推迟的任务
                new TaskModel
                {
                    Name = "复习数学",
                    Status = MyTaskStatus.Postponed,
                    PlannedDate = today.AddDays(-2),
                    Reason = "时间安排问题",  // 来自预设列表
                    PostponeDate = today.AddDays(2) // 模拟未来推迟
                },

                // 被放弃的任务
                new TaskModel
                {
                    Name = "练习钢琴",
                    Status = MyTaskStatus.Abandoned,
                    PlannedDate = today.AddDays(-1),
                    Reason = "动机缺失" // 来自预设列表
                }
            };

            _context.Tasks.AddRange(tasks);
            await _context.SaveChangesAsync();

        }



    }
}