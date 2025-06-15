using System;
using System.Collections.Generic;
using System.Linq;
using TimeController.Models;

namespace TimeController.Services
{
    public class DatabaseService
    {
        private readonly ITaskService _taskService;

        public ITaskService TaskService => _taskService;


        public DatabaseService(ITaskService taskService = null)
        {
            _taskService = taskService ?? App.Services.GetService(typeof(ITaskService)) as ITaskService;
        }

        public async Task SaveCourses(List<Course> courses, DateTime semesterStartDate)
        {
            if (_taskService == null)
            {
                throw new InvalidOperationException("任务服务不可用");
            }

            // 创建任务列表，避免一一保存
            var allTasks = new List<TaskModel>();

            // 确保开学日期是周一
            DateTime semesterFirstMonday = semesterStartDate;
            while (semesterFirstMonday.DayOfWeek != DayOfWeek.Monday)
            {
                semesterFirstMonday = semesterFirstMonday.AddDays(-1);
            }

            foreach (var course in courses)
            {
                // 确保 WeekPattern 不为空
                if (string.IsNullOrWhiteSpace(course.WeekPattern))
                {
                    course.WeekPattern = "1-16"; // 默认为1-16周
                }

                Console.WriteLine($"正在保存课程 {course.Name}，周次模式: {course.WeekPattern}");

                // 解析周次模式
                HashSet<int> weeks;
                try
                {
                    weeks = TimeController.ViewModels.AddCourseViewModel.ParseWeekPattern(course.WeekPattern);
                    Console.WriteLine($"解析周次: {string.Join(", ", weeks)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"周次解析失败: {ex.Message}, 默认为第1周");
                    weeks = new HashSet<int> { 1 };
                }

                // 针对每个周次创建独立的任务
                foreach (int week in weeks)
                {
                    var task = CreateTaskForWeek(course, week, semesterFirstMonday);
                    allTasks.Add(task);
                }
            }

            // 批量保存所有任务
            int batchSize = 50; // 每批处理的任务数
            for (int i = 0; i < allTasks.Count; i += batchSize)
            {
                var batch = allTasks.Skip(i).Take(batchSize);
                foreach (var task in batch)
                {
                    await _taskService.UpdateTaskAsync(task);
                }
            }
        }

        // 新增方法：为特定周次创建任务
        private TaskModel CreateTaskForWeek(Course course, int week, DateTime semesterFirstMonday)
        {
            // 解析星期几
            DayOfWeek courseDayOfWeek = ParseDayOfWeek(course.DayOfWeek);

            // 计算课程在指定周次的日期
            int dayOfWeekOffset = ((int)courseDayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            int daysFromStart = (week - 1) * 7 + dayOfWeekOffset;
            DateTime taskDate = semesterFirstMonday.AddDays(daysFromStart);

            // 计算 WeekDay 值 (0=周一, 1=周二...)
            int weekDay = dayOfWeekOffset;

            // 构建完整的Note
            string note = $"教师:{course.Teacher}, 地点:{course.Location}, 周次模式:{course.WeekPattern}";

            return new TaskModel
            {
                Name = course.Name,
                Note = note,
                Type = TaskType.学习学业,
                Mode = TaskMode.Strong,
                PlannedDate = taskDate,
                IsAllDay = false,
                StartTime = course.StartTime,
                EndTime = course.EndTime,
                Status = MyTaskStatus.Pending,
                IsReminderEnabled = true,
                CreatedAt = DateTime.Now,
                IsCourseTask = true,
                WeekDay = weekDay
            };
        }

        // 为特定周次创建任务
        private TaskModel CreateWeeklyTask(Course course, int week, DateTime semesterFirstMonday)
        {
            // 解析星期几
            DayOfWeek courseDayOfWeek = ParseDayOfWeek(course.DayOfWeek);

            // 计算课程在当前周的日期
            int dayOfWeekOffset = ((int)courseDayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            int daysFromStart = (week - 1) * 7 + dayOfWeekOffset;
            DateTime taskDate = semesterFirstMonday.AddDays(daysFromStart);

            // 计算 WeekDay 值 (0=周一, 1=周二...)
            int weekDay = dayOfWeekOffset;

            Console.WriteLine($"第 {week} 周 {course.Name} 课程: 星期{course.DayOfWeek}，日期 {taskDate:yyyy-MM-dd}");

            // 构建完整的Note
            string note = $"教师:{course.Teacher}, 地点:{course.Location}, 周次模式:{course.WeekPattern}";

            return new TaskModel
            {
                Name = course.Name,
                Note = note,
                Type = TaskType.学习学业,
                Mode = TaskMode.Strong,
                PlannedDate = taskDate,
                IsAllDay = false,
                StartTime = course.StartTime,
                EndTime = course.EndTime,
                Status = MyTaskStatus.Pending,
                IsReminderEnabled = true,
                CreatedAt = DateTime.Now,
                IsCourseTask = true,
                WeekDay = weekDay
            };
        }


        private TaskModel ConvertCourseToTask(Course course, DateTime semesterStartDate)
        {
            // 确保开学日期是周一
            DateTime firstDayOfSemester = semesterStartDate;
            while (firstDayOfSemester.DayOfWeek != DayOfWeek.Monday)
            {
                firstDayOfSemester = firstDayOfSemester.AddDays(-1);
            }

            // 解析星期几
            DayOfWeek courseDayOfWeek = ParseDayOfWeek(course.DayOfWeek);

            // 计算课程日期 (该课程在第一周的对应星期几)
            int daysToAdd = ((int)courseDayOfWeek - (int)DayOfWeek.Monday) % 7;
            DateTime taskDate = firstDayOfSemester.AddDays(daysToAdd);

            // 添加调试输出
            Console.WriteLine($"课程 {course.Name} 转换: 星期{course.DayOfWeek} => {taskDate:yyyy-MM-dd}");
            // 添加调试输出
            Console.WriteLine($"课程 {course.Name} 转换: 星期{course.DayOfWeek} => {taskDate:yyyy-MM-dd}");
            Console.WriteLine($"课程时间: {course.StartTime} - {course.EndTime}");
            return new TaskModel
            {
                Name = course.Name,
                Note = $"教师:{course.Teacher}, 地点:{course.Location}, 周次模式：{course.WeekPattern}",
                Type = TaskType.学习学业,
                Mode = TaskMode.Strong,
                PlannedDate = taskDate,
                IsAllDay = false,
                StartTime = course.StartTime,
                EndTime = course.EndTime,
                Status = MyTaskStatus.Pending,
                IsReminderEnabled = true,
                CreatedAt = DateTime.Now
            };
        }



        // 解析星期几文本为 DayOfWeek 枚举
        private DayOfWeek ParseDayOfWeek(string dayText)
        {
            return dayText.Trim() switch
            {
                "周一" or "星期一" or "1" or "一" => DayOfWeek.Monday,
                "周二" or "星期二" or "2" or "二" => DayOfWeek.Tuesday,
                "周三" or "星期三" or "3" or "三" => DayOfWeek.Wednesday,
                "周四" or "星期四" or "4" or "四" => DayOfWeek.Thursday,
                "周五" or "星期五" or "5" or "五" => DayOfWeek.Friday,
                "周六" or "星期六" or "6" or "六" => DayOfWeek.Saturday,
                "周日" or "星期日" or "周天" or "星期天" or "7" or "日" or "天" => DayOfWeek.Sunday,
                _ => throw new ArgumentException($"无法解析星期几: {dayText}")
            };
        }

        // 获取下一个指定星期几的日期
        private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
        {
            int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }
    }
}
