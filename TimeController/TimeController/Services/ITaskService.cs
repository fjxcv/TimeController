using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeController.Models;

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
        //Task ResetTaskDataAsync();

        Task DeleteTaskAsync(TaskModel task);

        event Action<TaskModel> TaskSaved;

    }
}