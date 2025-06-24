using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using TimeController.ViewModels;
using TimeController.Models;
using System.Collections.ObjectModel;

namespace TimeController.Views.StrongGoalMonth
{
    /// <summary>
    /// MonthView.xaml 的交互逻辑
    /// </summary>
    public partial class MonthView : Page
    {
        private MonthViewModel _viewModel;

        public MonthView()
        {
            InitializeComponent();
            _viewModel = new MonthViewModel();
            DataContext = _viewModel;

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel.CheckTodayTasks();
        }

        private void CalendarScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            foreach (var card in FindVisualChildren<DateCard>(CalendarScrollViewer))
            {
                if (card.IsExpanded)
                {
                    card.IsExpanded = false;
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    yield return result;
                }
                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}