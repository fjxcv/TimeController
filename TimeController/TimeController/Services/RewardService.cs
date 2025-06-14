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

        public async Task<RewardModel?> GetFinalRewardAsync()
        {
            return await _context.Rewards.FirstOrDefaultAsync(r => r.IsFinal);
        }

        public async Task SetFinalRewardAsync(RewardModel? reward)
        {
            // 首先，清除所有现有奖励的IsFinal标记
            var currentFinal = await _context.Rewards.FirstOrDefaultAsync(r => r.IsFinal);
            if (currentFinal != null)
            {
                currentFinal.IsFinal = false;
                _context.Rewards.Update(currentFinal);
            }

            // 如果传入了新的奖励，则设置其IsFinal标记
            if (reward != null)
            {
                var selectedReward = await _context.Rewards.FirstOrDefaultAsync(r => r.Id == reward.Id);
                if (selectedReward != null)
                {
                    selectedReward.IsFinal = true;
                    _context.Rewards.Update(selectedReward);
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}