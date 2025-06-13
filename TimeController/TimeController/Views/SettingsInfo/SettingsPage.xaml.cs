using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls;
using TimeController.ViewModels;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;


namespace TimeController.Views.SettingsInfo
{
    /// <summary>
    /// SettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsPage : Page
    {
        public SettingsPage(SettingsPageViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
