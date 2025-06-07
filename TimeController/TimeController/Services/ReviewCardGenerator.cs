using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeController.Helpers;
using TimeController.Models;

namespace TimeController.Services
{
    public class ReviewCardGenerator
    {
        /// <summary>
        /// 生成本周复盘建议卡片
        /// </summary>
        /// <param name="tasksThisWeek">本周所有任务</param>
        /// <param name="taskHistory">全部历史任务（用于判断连续推迟）</param>
        /// 

        public List<ReviewCardModel> GenerateCards(List<TaskModel> tasksThisWeek,List<TaskModel> taskHistory,DateTime weekStart,DateTime weekEnd)
        {
            var cards = new List<ReviewCardModel>();

            // 卡片 1：完成率分析
            cards.Add(GenerateCompletionRateCard(tasksThisWeek));

            // 卡片 2：重复推迟提示
            cards.Add(GenerateRepeatedPostponeCard(taskHistory, weekStart, weekEnd));

            // 卡片 3:任务分配策略建议
            cards.Add(GenerateTaskDistributionCard(tasksThisWeek));
            // 卡片 4: 过度规划提示
            cards.Add(GenerateOverPlanningCard(tasksThisWeek));
            // 卡片 5: 生活任务平衡
            cards.Add(GenerateLifeTaskBalanceCard(tasksThisWeek));
            // 卡片 6: 任务完成情况
            cards.Add(GenerateFinalEncouragementCard(tasksThisWeek));

            return cards;
        }

        private ReviewCardModel GenerateCompletionRateCard(List<TaskModel> tasks)
        {
            var total = tasks.Count;
            var completed = tasks.Count(t => t.Status == MyTaskStatus.Completed);

            string icon = "🕒";
            string title = "本周任务完成情况";
            string message;

            if (total == 0)
            {
                message = "本周没有安排任务，可以考虑轻量开始。";
            }
            else
            {
                double rate = (double)completed / total;
                if (rate >= 0.9)
                    message = "🎉 本周完成率非常高，继续保持！";
                else if (rate >= 0.6)
                    message = "✅ 本周任务率完成良好，可以略作优化。";
                else if (rate >= 0.3)
                    message = "⚠️ 本周任务完成率较低，建议复盘原因。";
                else
                    message = "🚨 完成率偏低，可考虑减少任务数量或优化计划。";
            }

            return new ReviewCardModel(icon, title, message, CardAccentHelper.GetAccentColor(title));
        }

        private ReviewCardModel GenerateRepeatedPostponeCard(List<TaskModel> history,DateTime weekStart,DateTime weekEnd)
        {
            var icon = "🔁";
            var title = "多次推迟提示";
            var repeated = new List<string>();

            // 1. 先筛出本周的所有任务
            var tasksThisWeek = history
                .Where(t =>
                    t.PlannedDate >= weekStart &&
                    t.PlannedDate < weekEnd)
                .ToList();

            // 2. 找出本周被推迟次数 >= 2 的那些
            foreach (var task in tasksThisWeek)
            {
                if (task.PostponedCount >= 2)
                {
                    repeated.Add($"{task.Name}（本周已推迟{task.PostponedCount}次）");
                }
            else
            {
                message = "本周没有任务被连续推迟，干得漂亮！";
            }
            // 3. 生成提示信息
            var message = repeated.Any()
                ? string.Join("\n", repeated)
                : "本周没有任务被多次推迟，干得漂亮！";

            return new ReviewCardModel(icon, title, message, CardAccentHelper.GetAccentColor(title));
        }

        private ReviewCardModel GenerateTaskDistributionCard(List<TaskModel> tasks)
        {
            var icon = "📋";
            var title = "任务分配策略建议";

            int total = tasks.Count;
            int completed = tasks.Count(t => t.Status == MyTaskStatus.Completed);
            int uncompleted = tasks.Count(t => t.Status != MyTaskStatus.Completed);

            string message;

            if (total < 3)
                message = "📌 本周任务安排较少，可以尝试多做一点小挑战。";
            else if (uncompleted >= 5)
                message = "⚠️ 有较多任务未完成，建议精简任务列表，提升执行质量。";
            else
                message = "✅ 任务分配较合理，继续保持当前节奏。";

            return new ReviewCardModel(icon, title, message, CardAccentHelper.GetAccentColor(title));
        }

        private ReviewCardModel GenerateOverPlanningCard(List<TaskModel> tasks)
        {
            var icon = "📁";
            var title = "是否过度计划";

            int planned = tasks.Count;
            int completed = tasks.Count(t => t.Status == MyTaskStatus.Completed);

            string message;

            if (planned > 10 && completed < 4)
                message = "😵‍💫 本周任务计划较多但完成不理想，建议减少计划任务数量。";
            else if (planned >= 8 && completed >= planned * 0.8)
                message = "💪 你计划较多但完成得也很好，干得漂亮！";
            else
                message = "📊 任务计划适中，完成情况合理。";

            return new ReviewCardModel(icon, title, message, CardAccentHelper.GetAccentColor(title));
        }

        private ReviewCardModel GenerateLifeTaskBalanceCard(List<TaskModel> tasks)
        {
            var icon = "🧩";
            var title = "生活类任务反思";

            //生活分类判断
            var lifeTypes = new[] { TaskType.日常任务, TaskType.自我提升, TaskType.其它 };

            int lifeTasks = tasks.Count(t => lifeTypes.Contains(t.Type));

            int total = tasks.Count;

            string message;

            if (total == 0)
            {
                message = "📝 本周没有任务记录，建议设定适当目标。";
            }
            else if (lifeTasks == 0)
            {
                message = "🧘‍ 本周没有生活类任务，建议加入一些放松安排。";
            }
            else if (lifeTasks >= total * 0.6)
            {
                message = "📌 生活任务占比较高，注意兼顾工作目标。";
            }
            else
            {
                message = "✅ 生活与工作安排平衡得很好，继续保持。";
            }

            return new ReviewCardModel(icon, title, message, CardAccentHelper.GetAccentColor(title));
        }


        private ReviewCardModel GenerateFinalEncouragementCard(List<TaskModel> tasks)
        {
            var icon = "🌟";
            var title = "总体完成率鼓励";

            int total = tasks.Count;
            int completed = tasks.Count(t => t.Status == MyTaskStatus.Completed);

            string message;

            if (total == 0)
                message = "🧩 本周未安排任何任务，下周尝试设定一些简单目标吧～";
            else
            {
                double rate = (double)completed / total;

                if (rate >= 0.9)
                    message = "🏆 优秀！完成率高达 90% 以上，你是时间掌控大师！";
                else if (rate >= 0.6)
                    message = "👏 完成率不错，继续加油！";
                else
                    message = "💡 虽然完成不多，但每一个努力都值得鼓励！";  
            }

            return new ReviewCardModel(icon, title, message, CardAccentHelper.GetAccentColor(title));
        }

    }
}
