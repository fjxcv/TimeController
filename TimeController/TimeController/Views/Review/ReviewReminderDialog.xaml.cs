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
using System.Windows.Shapes;

namespace TimeController.Views.Review
{
    /// <summary>
    /// ReviewReminderDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ReviewReminderDialog : Window
    {
        public bool ShouldNavigate { get; private set; } = false;

        public ReviewReminderDialog()
        {
            InitializeComponent();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            ShouldNavigate = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ShouldNavigate = false;
            this.Close();
        }
    }
}
