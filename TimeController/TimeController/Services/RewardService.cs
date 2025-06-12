using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeController.Models;

namespace TimeController.Services
{
    public class RewardService : IRewardService
    {
        private readonly TaskDbContext _context;
        public RewardService(TaskDbContext context)
        {
            _context = context;
        }

        public async Task<List<RewardModel>> GetRewardsAsync()
        {
            return await _context.Rewards.ToListAsync();
        }

        public async Task AddRewardAsync(RewardModel reward)
        {
            await _context.Rewards.AddAsync(reward);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRewardAsync(RewardModel reward)
        {
            _context.Rewards.Remove(reward);
            await _context.SaveChangesAsync();
        }
    }
}