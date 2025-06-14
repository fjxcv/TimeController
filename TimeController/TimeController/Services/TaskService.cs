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

        public event Action<TaskModel>? TaskSaved;

        public TaskService(TaskDbContext context)
        {
            _context = context;
        }

        public async Task<List<TaskModel>> GetAllCourseTasksAsync()
        {
            // 从数据库中获取所有 IsCourseTask = true 的任务
            return await _context.Task
                .Where(t => t.IsCourseTask)
                .ToListAsync();
        }


        // 按周获取课程
        public async Task<List<TaskModel>> GetCourseTasksForWeekAsync(DateTime referenceDate)
        {
            // 计算参考日期所在周的周一
            DateTime monday = referenceDate.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);

        public IDisposable BeginTransaction()
        {
            _currentTransaction = _context.Database.BeginTransaction();
            return _currentTransaction;
        }

            // 返回该周的课程任务
            return await _context.Task
                .Where(t => t.IsCourseTask && t.PlannedDate >= monday && t.PlannedDate <= sunday)
                .ToListAsync();
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

            Debug.WriteLine($"[GetTasksForDate] Date range = {start:yyyy-MM-dd HH:mm:ss} ～ {end:yyyy-MM-dd HH:mm:ss}");
           
            // 3. 构造查询
            var query = _context.Task
                .Where(t => t.PlannedDate >= start && t.PlannedDate < end);

            // 4. 打印 EF 要执行的 SQL
            Debug.WriteLine("[GetTasksForDate] SQL =\n" + query.ToQueryString());

            // 5. 真正执行
            var list = await query.ToListAsync();

            // 6. 再打印一下结果明细
            Debug.WriteLine($"[GetTasksForDate] Fetched {list.Count} items:");
            foreach (var t in list)
                Debug.WriteLine($"    - {t.Name} | {t.Status} | {t.PlannedDate:yyyy-MM-dd HH:mm:ss}");

            return list;

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

            //return await _context.Task
            //    .Where(t => t.PlannedDate.Date >= startDate.Date && t.PlannedDate.Date <= endDate.Date)
            //    .ToListAsync();

            return tasks;
        }

        public Task<List<TaskModel>> GetAllTasksAsync()
        {
            return _context.Task.OrderByDescending(t => t.PlannedDate).ToListAsync();
        }

        //更新任务状态
        public async Task UpdateTaskAsync(TaskModel task)
        {
            var conn = _context.Database.GetDbConnection();
            Debug.WriteLine($"[DB File] {conn.DataSource}");

            Debug.WriteLine($"🔨 Enter UpdateTaskAsync for {task.Name}, Id={task.Id}");
            if (task.Id == 0)
            {
                await _context.Task.AddAsync(task);
            }
            else
            {
                _context.Task.Update(task);
            }

            await _context.SaveChangesAsync();

            TaskSaved?.Invoke(task);

            Debug.WriteLine("🛎️ TaskSaved.Invoke 已调用完毕");

            //调试输出
            Debug.WriteLine($"[更新任务] {task.Name}, 状态={task.Status}, 计划时间={task.PlannedDate:yyyy-MM-dd}");
        }

        public async Task<IEnumerable<TaskModel>> GetAllPendingTasksAsync()
        {
            return await _context.Task
                .Where(t => t.Status == MyTaskStatus.Pending)
                .ToListAsync() ?? new List<TaskModel>(); // 保证不是 null
        }

        // 种子数据
        public async Task ResetTaskDataAsync()
        {
            // 清空旧数据
            var all = await _context.Task.ToListAsync();
            _context.Task.RemoveRange(all);
            await _context.SaveChangesAsync();

            var today = DateTime.Today;
            var tasks = new List<TaskModel>
            {
                // 已完成
                new TaskModel {
                    Name = "完成项目文档",
                    Status = MyTaskStatus.Completed,
                    PlannedDate = today,
                    IsAllDay = true,
                    Mode = TaskMode.Strong
                },
                new TaskModel {
                    Name = "团队进度汇报会议",
                    Status = MyTaskStatus.Completed,
                    PlannedDate = today.AddDays(-1),
                    IsAllDay = false,
                    StartTime = TimeSpan.FromHours(10),
                    EndTime   = TimeSpan.FromHours(11),
                    Mode = TaskMode.Strong
                },

                //未完成（Pending）
                new TaskModel {
                    Name = "代码审查",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today,
                    IsAllDay = false,
                    StartTime = TimeSpan.FromHours(14),
                    EndTime   = TimeSpan.FromHours(15),
                    Mode = TaskMode.Strong
                },
                new TaskModel {
                    Name = "明日需求评审",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today.AddDays(1),
                    IsAllDay = false,
                    StartTime = TimeSpan.FromHours(16),
                    EndTime   = TimeSpan.FromHours(17),
                    Mode = TaskMode.Strong
                },
                new TaskModel {
                    Name = "学习英语词汇",
                    Status = MyTaskStatus.Pending,
                    PlannedDate = today,
                    IsAllDay = true,
                    Mode = TaskMode.Strong
                },

                // —— 已推迟 —— 
                new TaskModel {
                    Name = "复习数学",
                    Status = MyTaskStatus.Postponed,
                    PlannedDate = today.AddDays(-2),
                    Reason = "时间安排问题",
                    PostponeDate= today.AddDays(2),
                    PostponedAt = today.AddDays(-2).AddHours(9),
                    Mode = TaskMode.Strong
                },
                new TaskModel {
                    Name = "读《设计模式》",
                    Status = MyTaskStatus.Postponed,
                    PlannedDate = today.AddDays(-3),
                    Reason = "外部干扰",
                    PostponeDate= today.AddDays(3),
                    PostponedAt = today.AddDays(-3).AddHours(15),
                    Mode = TaskMode.Strong
                },

                // —— 已放弃 —— 
                new TaskModel {
                    Name = "练习钢琴",
                    Status = MyTaskStatus.Abandoned,
                    PlannedDate = today.AddDays(-1),
                    Reason = "动机缺失",
                    AbandonedAt = today.AddDays(-1).AddHours(20),
                    Mode = TaskMode.Strong
                },
                new TaskModel {
                    Name = "周末远足",
                    Status = MyTaskStatus.Abandoned,
                    PlannedDate = today.AddDays(2),
                    Reason = "外部干扰",
                    AbandonedAt = today.AddDays(0).AddHours(18),
                    Mode = TaskMode.Strong
                }
            };

            _context.Task.AddRange(tasks);
            await _context.SaveChangesAsync();
        }





    }
}