using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeController.Models;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


namespace TimeController.Services
{
    public interface ITaskService
    {
        Task<List<TaskModel>> GetTasksForDate(DateTime date);
        Task<List<TaskModel>> GetTasksForDateRange(DateTime startDate, DateTime endDate);

        //过期任务不随日期变动，得到所有过期任务
        Task<IEnumerable<TaskModel>> GetAllPendingTasksAsync();

        //历史任务获取
        Task<List<TaskModel>> GetAllTasksAsync();

        //更新任务状态
        Task UpdateTaskAsync(TaskModel task);

        //种子数据
        Task ResetTaskDataAsync();

        Task DeleteTaskAsync(TaskModel task);

        // 获取所有课程任务（不限日期范围）
        Task<List<TaskModel>> GetAllCourseTasksAsync();

        //根据日期范围获取课程任务
        Task<List<TaskModel>> GetCourseTasksForWeekAsync(DateTime referenceDate);

        // 不返回具体的事务对象，而是直接提供事务功能
        IDisposable BeginTransaction();
        void CommitTransaction();
        void RollbackTransaction();

        event Action<TaskModel> TaskSaved;

    }
}