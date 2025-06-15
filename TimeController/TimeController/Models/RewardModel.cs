using System.ComponentModel;

namespace TimeController.Models
{
    public class RewardModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        private bool _isClaimed;
        public bool IsClaimed
        {
            get => _isClaimed;
            set
            {
                if (_isClaimed != value)
                {
                    _isClaimed = value;
                    OnPropertyChanged(nameof(IsClaimed));
                }
            }
        }

        private bool _isFinal;
        public bool IsFinal
        {
            get => _isFinal;
            set
            {
                if (_isFinal != value)
                {
                    _isFinal = value;
                    OnPropertyChanged(nameof(IsFinal));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}