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

        public DbSet<RewardModel> Rewards { get; set; }

        public TaskDbContext(DbContextOptions<TaskDbContext> options)
            : base(options)
        {

        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaskModel>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<TaskModel>()
                .Property(t => t.Status)
                .HasConversion<string>();

            modelBuilder.Entity<TaskModel>()
                .Property(t => t.Mode)
                .HasConversion<string>();

            modelBuilder.Entity<TaskModel>()
                .Property(t => t.Type)
                .HasConversion<string>();
            modelBuilder.Entity<RewardModel>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<RewardModel>()
                .HasData(
                    new RewardModel { Id = 1, Title = "吃顿好的", IsClaimed = false },
                    new RewardModel { Id = 2, Title = "看想看的电影", IsClaimed = false },
                    new RewardModel { Id = 3, Title = "放纵玩一天", IsClaimed = false }
                );
            base.OnModelCreating(modelBuilder);
        }
    }
}
