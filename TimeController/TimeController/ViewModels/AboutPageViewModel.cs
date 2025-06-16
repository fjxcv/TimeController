using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TimeController.ViewModels
{
    public class AboutPageViewModel : INotifyPropertyChanged
    {

        /// <summary>
        /// 新手教程列表
        /// </summary>
        public ObservableCollection<string> TutorialSteps { get; }

        /// <summary>
        /// 系统理念文案
        /// </summary>
        public ObservableCollection<string> PhilosophyLines { get; }

        /// <summary>
        /// 致谢列表
        /// </summary>
        public ObservableCollection<string> Acknowledgements { get; }

        public AboutPageViewModel()
        {
            // 新手教程
            TutorialSteps = new ObservableCollection<string>
            {
                "🐟【咸鱼日常模式】",
                "1. 默认启动即进入咸鱼日常模式，顶部有本周进度条（默认完成4个即奖励，可在设置调整）",
                "2. 点击任一区域卡片空白处，输入任务名称并回车添加",
                "3. 点击任务前的复选框可标记完成，再次点击可撤销",
                "4. 鼠标移到任务行中会在右侧出现“×”按钮，点击即可删除该任务",
                "5. 右侧“长期备忘”可查看不限期的任务；在“本周复盘”查看周数据反馈",
                "6. 每周的已完成任务会在新的一周重新计入未完成",
                "",
                "🎯【强目标管理模式】",
                "1. 点击导航栏“强目标管理模式”进入",
                "2. 在“周视图”中，横向展示全天任务和周一至周日的时间轴（0:00–24:00），按需操作：",
                "   - 点击空白区域出现加号，可点击添加时段任务；勾选标记完成",
                "3. 在“月视图”中，显示当月日历网格，每日可显示最多3条任务；点击“∨”可查看全部任务；点击卡片即可添加任务",
                "4. 切换到“复盘”页面，默认进入每日复盘，展示：",
                "   - 已完成/未完成任务列表",
                "   - 今日未处理任务与过期任务，可选择推迟或放弃",
                "5. 每周复盘页面中，展示周任务完成及推迟/放弃的折线图，并给出6项行为建议"
            };

            // 系统理念
            PhilosophyLines = new ObservableCollection<string>
            {

                "1. 双模式设计：",
                "   - 咸鱼日常：低门槛、低负担，通过可视化进度与周奖励激发持续行动",
                "   - 强目标管理：周视图 + 月视图 + 复盘，支持项目冲刺或考试备考场景",
                "",
                "2. 温和驱动力：",
                "   - 不“逼”完成，而是通过“完成感”与正反馈，让行动成为习惯",
                "",
                "3. 数据智能：",
                "   - 复盘模块自动统计完成数、推迟与放弃数，生成个性化建议",
                "",
                "4. 为“非理性用户”而生：",
                "   - 针对拖延与注意力分散，提供“易开始”“易坚持”“易复盘”的交互"
            };

            // 致谢
            Acknowledgements = new ObservableCollection<string>
            {
                "Inkore.UI.WPF.Modern — 现代化控件库",
                "小组的每一个人",
                "戒社、名侦探柯南 — 写代码时的陪伴",
                "所有听我抱怨骂街的朋友们"
            };
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

        #endregion
    }
}
