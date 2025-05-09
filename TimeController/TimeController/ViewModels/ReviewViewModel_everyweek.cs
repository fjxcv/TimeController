using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TimeController.Models;
using TimeController.Services;

namespace TimeController.ViewModels
{
    public class ReviewViewModel_everyweek : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private bool _isEverydayPage = false;

        public bool IsEverydayPage 
        {
            get => _isEverydayPage;
            set
            {
                if (_isEverydayPage != value)
                {
                    _isEverydayPage = value;
                    OnPropertyChanged(nameof(IsEverydayPage));
                }
            }
        }

        public ICommand NavigateToEverydayCommand { get; }
        public ICommand NavigateToEveryweekCommand { get; }

        public ReviewViewModel_everyweek(INavigationService navigationService)
        {
            _navigationService = navigationService;
            NavigateToEverydayCommand = new RelayCommand(_ => _navigationService.NavigateTo("Everyday"));
            NavigateToEveryweekCommand = new RelayCommand(_ => { }); // 当前页面，不跳转
            IsEverydayPage = false;  // 确保设置为false
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
