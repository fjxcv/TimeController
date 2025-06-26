using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TimeController.ViewModels
{
    public class AboutPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> TutorialSteps { get; } // 新手教程步骤列表
        public ObservableCollection<string> PhilosophyLines { get; } // 项目理念内容
        public ObservableCollection<string> Acknowledgements { get; } // 致谢信息

        public AboutPageViewModel()
        {
            // 教程内容
            TutorialSteps = new ObservableCollection<string>
            {
                "🐟【咸鱼日常模式】",
                "1. 默认进入咸鱼模式，顶部有本周进度条（默认 4 个任务达成奖励，可在设置更改）",
                "2. 点击空白处输入任务，回车即可添加",
                "3. 可打钩标记完成，再次点击撤销",
                "4. 鼠标悬浮右侧出现删除按钮",
                "5. 可查看长期备忘 + 本周复盘结果",
                "6. 每周完成任务将自动清零重新统计",
                "7. 点击进度条旁的⭐图标可自定义每周奖励内容",
                "8. 每日 18:00 默认弹出今日复盘提醒，可在设置中关闭或修改时间",
                "",
                "🎯【强目标管理模式】",
                "1. 导航栏进入强目标模式",
                "2. 周视图展示全天任务 + 每天时间轴（0:00–24:00）",
                "3. 月视图展示每日任务数量，点击可查看和添加",
                "4. 每日复盘：完成/未完成列表，支持推迟或放弃",
                "5. 每周复盘：图表展示执行情况并生成 6 项建议",
                "6. 竖行区域点击出现加号，再次点击添加时间段任务",
                "7. 强管理任务支持点击卡片编辑内容，悬浮后可完成或删除",
                "8. 支持手动导入课程任务",
                "9. 支持通过 Excel 表格自动识别导入课程任务",
                "10. 强管理任务开启提醒后，到点将弹窗提示"
            };

            // 系统理念
            PhilosophyLines = new ObservableCollection<string>
            {
                "1. 双模式设计：",
                "   - 咸鱼日常：低门槛、周奖励",
                "   - 强目标管理：结构化视图 + 复盘分析",
                "",
                "2. 温和驱动力：",
                "   - 用“完成感”替代“强迫感”",
                "",
                "3. 数据智能反馈：",
                "   - 系统自动识别推迟任务、行为模式，生成建议",
                "",
                "4. 非理性友好：",
                "   - 为拖延者和注意力分散者设计，鼓励渐进式管理"
            };

            // 感谢致辞
            Acknowledgements = new ObservableCollection<string>
            {
                "Inkore.UI.WPF.Modern — 现代 UI 控件库",
                "项目成员：张欣茹、曹心如、邱舒桐、彭湉欣"
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}