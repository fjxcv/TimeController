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
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.Views.Dialogs
{
    /// <summary>
    /// PostponeDateDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PostponeDateDialog : Window
    {

        public DateTime? SelectedDate { get; set; }

        public PostponeDateDialog()
        {
            InitializeComponent();
            SelectedDate = DateTime.Today;
            DataContext = this;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (DatePicker_Target.SelectedDate.HasValue)
            {
                SelectedDate = DatePicker_Target.SelectedDate;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("请选择推迟日期");
            }
        }
    }
}
