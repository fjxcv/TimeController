using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TimeController.Services;

namespace TimeController
{
    public class TaskDbContextFactory : IDesignTimeDbContextFactory<TaskDbContext>
    {
        public TaskDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TaskDbContext>();
            optionsBuilder.UseSqlite("Data Source=task.db"); //注意这里连接字符串要真实有效

            return new TaskDbContext(optionsBuilder.Options);
        }
    }
}
