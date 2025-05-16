using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeController.Models;

namespace TimeController.Services
{
    public class TaskService : ITaskService
    {
        private readonly TaskDbContext _context;

        public TaskService(TaskDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskModel>> GetTasksForDate(DateTime date)
        {
            return await _context.Tasks
                .Where(t => t.PlannedDate.Date == date.Date)
                .ToListAsync();
        }

        public async Task<List<TaskModel>> GetTasksForDateRange(DateTime startDate, DateTime endDate)
        {
            return await _context.Tasks
                .Where(t => t.PlannedDate.Date >= startDate.Date && t.PlannedDate.Date <= endDate.Date)
                .ToListAsync();
        }

        //更新任务状态
        public async Task UpdateTaskAsync(TaskModel task)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();
        }

        //种子数据
        public async Task SeedTestDataAsync()
        {
            if (await _context.Tasks.AnyAsync()) return; // 避免重复初始化

            var today = DateTime.Today;
            var tasks = new List<TaskModel>
            {
                new TaskModel
                {
                    Name = "整理工作笔记",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today.AddDays(-2), // 两天前
                    IsAllDay = true
                },
                new TaskModel
                {
                    Name = "项目进度汇报",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today.AddDays(-1), // 昨天
                    IsAllDay = false,
                    StartTime = today.AddDays(-1).AddHours(15),
                    EndTime = today.AddDays(-1).AddHours(16)
                },
                new TaskModel
                {
                    Name = "更新项目计划",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today.AddDays(-1), // 昨天
                    IsAllDay = true
                },
                new TaskModel
                {
                    Name = "客户需求分析",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today, // 今天
                    IsAllDay = false,
                    StartTime = today.AddHours(9),
                    EndTime = today.AddHours(11)
                },
                new TaskModel
                {
                    Name = "代码审查",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today, // 今天
                    IsAllDay = false,
                    StartTime = today.AddHours(14),
                    EndTime = today.AddHours(15)
                },
                new TaskModel
                {
                    Name = "准备周报",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today, // 今天
                    IsAllDay = true
                }
            };

            _context.Tasks.AddRange(tasks);
            await _context.SaveChangesAsync();
        }


    }
}