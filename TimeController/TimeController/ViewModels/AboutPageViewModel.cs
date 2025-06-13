using System.Collections.ObjectModel;

namespace TimeController.ViewModels
{
    public class AboutPageViewModel
    {
        public ObservableCollection<string> TutorialSteps { get; }
            = new ObservableCollection<string>
        {
            "添加任务：点击右上角“+”按钮，填写任务名称和时间。",
            "切换模式：在导航栏选择“咸鱼模式”或“强目标模式”。",
            "查看复盘：在“复盘”页面查看您的每日/每周任务完成情况。"
        };

        public string Philosophy { get; } =
            "本系统秉承“温和驱动力”理念，帮助您在不焦虑的状态下逐步达成目标，" +
            "平衡生活与工作。无论是轻松的咸鱼模式，还是进阶的强目标模式，" +
            "都由你自由切换、随心使用。";

        public ObservableCollection<(string Label, string Uri)> Links { get; }
            = new ObservableCollection<(string, string)>
        {
            ("查看完整帮助", "https://example.com/help"),
            ("常见问题（FAQ）", "https://example.com/faq"),
            ("联系我们", "mailto:support@example.com")
        };
    }
}
