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
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.Tasks
                .Where(t =>
                    (t.Status == MyTaskStatus.Pending || t.Status == MyTaskStatus.Completed) &&
                    t.PlannedDate >= startOfDay &&
                    t.PlannedDate < endOfDay)
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

        public async Task<IEnumerable<TaskModel>> GetAllPendingTasksAsync()
        {
            return await _context.Tasks
                .Where(t => t.Status == MyTaskStatus.Pending)
                .ToListAsync() ?? new List<TaskModel>(); // 保证不是 null
        }


    //    //种子数据
    //    public async Task ResetTaskDataAsync()
    //    {
    //        // 清空旧数据
    //        var all = await _context.Tasks.ToListAsync();
    //        _context.Tasks.RemoveRange(all);
    //        await _context.SaveChangesAsync();

    //        var today = DateTime.Today;
    //        var tasks = new List<TaskModel>
    //{
    //    new TaskModel
    //    {
    //        Name = "完成项目文档",
    //        Status = MyTaskStatus.Completed,
    //        PlannedDate = today,
    //        IsAllDay = true
    //    },
    //    new TaskModel
    //    {
    //        Name = "代码审查",
    //        Status = MyTaskStatus.Pending,
    //        PlannedDate = today,
    //        IsAllDay = false,
    //        StartTime = today.AddHours(14),
    //        EndTime = today.AddHours(15)
    //    },
    //    new TaskModel
    //    {
    //        Name = "项目进度汇报",
    //        Status = MyTaskStatus.Pending,
    //        PlannedDate = today.AddDays(-3),
    //        IsAllDay = true
    //    }
    //};

    //        _context.Tasks.AddRange(tasks);
    //        await _context.SaveChangesAsync();

    //    }



    }
}