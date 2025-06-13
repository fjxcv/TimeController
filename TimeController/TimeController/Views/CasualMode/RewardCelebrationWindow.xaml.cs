using System.Windows;

namespace TimeController.Views.CasualMode
{
    public partial class RewardCelebrationWindow : Window
    {
        public RewardCelebrationWindow()
        {
            InitializeComponent();
        }

        private void ClaimRewardButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 