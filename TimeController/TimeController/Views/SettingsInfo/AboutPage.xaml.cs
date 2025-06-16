using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TimeController.ViewModels;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace TimeController.Views.SettingsInfo
{
    /// <summary>
    /// AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage(AboutPageViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
