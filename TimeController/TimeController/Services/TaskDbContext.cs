using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeController.Models;

namespace TimeController.Services
{
    public class TaskDbContext : DbContext
    {
        public DbSet<TaskModel> Task { get; set; }

        public TaskDbContext(DbContextOptions<TaskDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskModel>()
                .Property(t => t.Status)
                .HasConversion<string>();

            modelBuilder.Entity<TaskModel>()
                .Property(t => t.Mode)
                .HasConversion<string>();

            modelBuilder.Entity<TaskModel>()
                .Property(t => t.Type)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
