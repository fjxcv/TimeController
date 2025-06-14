using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using TimeController.ViewModels;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;


namespace TimeController.Views.Review
{
    /// <summary>
    /// ReviewView_everyweek.xaml 的交互逻辑
    /// </summary>
    public partial class ReviewView_everyweek : Page
    {
        public ReviewView_everyweek(ReviewViewModel_everyweek vm)
        {
            InitializeComponent();
            DataContext = vm;

        }

    }
}
