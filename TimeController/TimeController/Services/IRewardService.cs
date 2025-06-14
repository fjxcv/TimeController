using System.Collections.Generic;
using System.Threading.Tasks;
using TimeController.Models;

namespace TimeController.Services
{
    public interface IRewardService
    {
        Task<List<RewardModel>> GetRewardsAsync();
        Task AddRewardAsync(RewardModel reward);
        Task DeleteRewardAsync(RewardModel reward);
        Task<RewardModel?> GetFinalRewardAsync();
        Task SetFinalRewardAsync(RewardModel reward);
    }
}