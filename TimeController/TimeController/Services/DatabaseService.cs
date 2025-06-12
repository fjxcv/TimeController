using System;
using System.Collections.Generic;
using System.Linq;
using TimeController.Models;

namespace TimeController.Services
{
    public class DatabaseService
    {
        private readonly ITaskService _taskService;

        public DatabaseService(ITaskService taskService = null)
        {
            _taskService = taskService ?? App.Services.GetService(typeof(ITaskService)) as ITaskService;
        }

        public async Task SaveCourses(List<Course> courses, DateTime semesterStartDate)
        {
            Console.WriteLine($"开始保存 {courses.Count} 门课程，开学日期: {semesterStartDate:yyyy-MM-dd}");
            if (_taskService == null)
            {
                throw new InvalidOperationException("任务服务不可用");
            }

            foreach (var course in courses)
            {
                // 将课程转换为任务模型，传入开学日期
                var task = ConvertCourseToTask(course, semesterStartDate);

                // 保存任务到数据库
                await _taskService.UpdateTaskAsync(task);
            }
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
                Note = $"教师:{course.Teacher}, 地点:{course.Location}",
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

        private TaskModel ConvertCourseToTask(Course course)
        {
            // 解析星期几字符串为具体日期
            DateTime taskDate = GetNextWeekday(DateTime.Today, ParseDayOfWeek(course.DayOfWeek));

            return new TaskModel
            {
                Name = course.Name,
                Note = $"教师: {course.Teacher}, 地点: {course.Location}",
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
