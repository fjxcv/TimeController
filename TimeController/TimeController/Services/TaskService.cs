using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
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
            // 取出当天零点到次日零点之间的所有任务
            var start = date.Date;
            var end = start.AddDays(1);

            return await _context.Task
                .Where(t =>
                    t.PlannedDate >= start &&
                    t.PlannedDate < end
                )
                .ToListAsync();
        }

        public async Task DeleteTaskAsync(TaskModel task)
        {
            _context.Task.Remove(task);
            await _context.SaveChangesAsync();
        }



        public async Task<List<TaskModel>> GetTasksForDateRange(DateTime startDate, DateTime endDate)
        {
            var tasks = await _context.Task
                .Where(t => t.PlannedDate.Date >= startDate.Date && t.PlannedDate.Date <= endDate.Date)
                .ToListAsync();

            // 这里是调试代码，输出加载到的任务数量和状态
            Debug.WriteLine($"[周复盘] 加载到的任务数量: {tasks.Count}");
            foreach (var task in tasks)
            {
                Debug.WriteLine($"任务状态: {task.Name} - {task.Status}");
            }

            return await _context.Task
                .Where(t => t.PlannedDate.Date >= startDate.Date && t.PlannedDate.Date <= endDate.Date)
                .ToListAsync();
        }

        public Task<List<TaskModel>> GetAllTasksAsync()
        {
            return _context.Task.OrderByDescending(t => t.PlannedDate).ToListAsync();
        }

        //更新任务状态
        public async Task UpdateTaskAsync(TaskModel task)
        {
            _context.Task.Update(task);
            await _context.SaveChangesAsync();

            //调试输出
            Debug.WriteLine($"[更新任务] {task.Name}, 状态={task.Status}, 计划时间={task.PlannedDate:yyyy-MM-dd}");
        }

        public async Task<IEnumerable<TaskModel>> GetAllPendingTasksAsync()
        {
            return await _context.Task
                .Where(t => t.Status == MyTaskStatus.Pending)
                .ToListAsync() ?? new List<TaskModel>(); // 保证不是 null
        }


        //种子数据
        public async Task ResetTaskDataAsync()
        {
            // 清空旧数据
            var all = await _context.Task.ToListAsync();
            _context.Task.RemoveRange(all);
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

            _context.Task.AddRange(tasks);
            await _context.SaveChangesAsync();

        }



    }
}