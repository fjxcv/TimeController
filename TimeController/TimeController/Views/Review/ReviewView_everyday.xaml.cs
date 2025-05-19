using System;
using System.Windows;
using System.Windows.Controls;
using TimeController.Services;
using TimeController.ViewModels;
using TimeController.Views.Dialogs;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;
using TimeController.Models;
using Microsoft.Extensions.DependencyInjection;

namespace TimeController.Views.Review
{
    public partial class ReviewView_everyday : Page
    {
        public ReviewView_everyday(ReviewViewModel_everyday vm)
        {
            InitializeComponent();
            DataContext = vm;

        }

    }
}