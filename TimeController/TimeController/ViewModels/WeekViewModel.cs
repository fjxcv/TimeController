using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TimeController.Models;
using System.Collections.ObjectModel;

namespace TimeController.ViewModels
{
    public class WeekViewModel : INotifyPropertyChanged
    {
        private DateTime _currentDate;
        private int _currentWeek;
        public ObservableCollection<TaskModel> Tasks { get; } = new();
        public DateTime CurrentDate
        {
            get => _currentDate;
            set
            {
                if (_currentDate != value)
                {
                    _currentDate = value;
                    OnPropertyChanged();
                    UpdateWeekText();
                    UpdateMonthText();
                }
            }
        }

        public string MonthText { get; private set; }
        public string WeekText { get; private set; }

        public ICommand PreviousWeekCommand { get; }
        public ICommand NextWeekCommand { get; }
        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }

        public WeekViewModel()
        {
            _currentDate = DateTime.Today;
            _currentWeek = GetWeekOfMonth(_currentDate);

            UpdateMonthText();
            UpdateWeekText();

            PreviousWeekCommand = new RelayCommand(_ => NavigateWeek(-7));
            NextWeekCommand = new RelayCommand(_ => NavigateWeek(7));
            PreviousMonthCommand = new RelayCommand(_ => NavigateMonth(-1));
            NextMonthCommand = new RelayCommand(_ => NavigateMonth(1));
        }

        private void NavigateWeek(int offset)
        {
            CurrentDate = CurrentDate.AddDays(offset);
            _currentWeek = GetWeekOfMonth(CurrentDate);
        }

        private void NavigateMonth(int offset)
        {
            CurrentDate = CurrentDate.AddMonths(offset);
            _currentWeek = GetWeekOfMonth(CurrentDate);
        }

        private int GetWeekOfMonth(DateTime date)
        {
            var firstDay = new DateTime(date.Year, date.Month, 1);
            var firstDayOfWeek = ((int)firstDay.DayOfWeek + 6) % 7;
            var dayOfMonth = date.Day;
            return (dayOfMonth + firstDayOfWeek + 6) / 7;
        }

        private void UpdateMonthText()
        {
            MonthText = $"{_currentDate.Month}月份";
            OnPropertyChanged(nameof(MonthText));
        }

        private void UpdateWeekText()
        {
            WeekText = $"第{_currentWeek}周";
            OnPropertyChanged(nameof(WeekText));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
