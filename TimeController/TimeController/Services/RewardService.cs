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
            // 返回所有奖励
            return await _context.Rewards.ToListAsync();
        }

        public async Task AddRewardAsync(RewardModel reward)
        {
            // 新增一个奖励，默认 IsClaimed 为 false
            reward.IsClaimed = false;
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
            // 把“最终奖励”改为所有已勾选(IsClaimed)的第一个
            return await _context.Rewards
                                 .FirstOrDefaultAsync(r => r.IsClaimed);
        }

        public async Task SetFinalRewardAsync(RewardModel? reward)
        {
            // 先把之前所有的 IsClaimed 置为 false
            var claimed = await _context.Rewards
                                        .Where(r => r.IsClaimed)
                                        .ToListAsync();
            foreach (var r in claimed)
            {
                r.IsClaimed = false;
                _context.Rewards.Update(r);
            }

            // 如果新传入了一个 reward，就把它的 IsClaimed 置 true
            if (reward != null)
            {
                var selected = await _context.Rewards
                                             .FirstOrDefaultAsync(r => r.Id == reward.Id);
                if (selected != null)
                {
                    selected.IsClaimed = true;
                    _context.Rewards.Update(selected);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
