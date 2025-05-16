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
        

        Task UpdateTaskAsync(TaskModel task);

    }
}