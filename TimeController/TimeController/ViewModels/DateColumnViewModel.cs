using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TimeController.ViewModels
{
    public class DateColumnViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private string _weekDayText;
        private string _dateText;
        private bool _isCurrentMonth;
        private ObservableCollection<WeekViewModel.TaskBlock> _allDayTasks;
        public bool ShouldShowMoreButton => AllDayTasks?.Count > 2;


        public int Index { get; set; }

        public string WeekDayText
        {
            get => _weekDayText;
            set
            {
                if (_weekDayText != value)
                {
                    _weekDayText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DateText
        {
            get => _dateText;
            set
            {
                if (_dateText != value)
                {
                    _dateText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCurrentMonth
        {
            get => _isCurrentMonth;
            set
            {
                if (_isCurrentMonth != value)
                {
                    _isCurrentMonth = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<WeekViewModel.TaskBlock> AllDayTasks
        {
            get => _allDayTasks;
            set
            {
                if (_allDayTasks != value)
                {
                    _allDayTasks = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShouldShowMoreButton));
                }
            }
        }

<<<<<<< HEAD
=======
        public bool ShouldShowMoreButton => AllDayTasks?.Count > 2;

>>>>>>> a5523a6 (临时保存：切换到自己分支之前的未完成工作)
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        // 在 DateColumnViewModel.cs 中添加
        public void RefreshAllDayTasksView()
        {
<<<<<<< HEAD
            OnPropertyChanged(nameof(AllDayTasks));
=======
            if (AllDayTasksView == null) return;

            AllDayTasksView.Filter = item =>
            {
                var block = item as WeekViewModel.TaskBlock;
                if (block == null) return false;
                int idx = AllDayTasks.IndexOf(block);
                return idx < 2 || IsExpanded;
            };
            AllDayTasksView.Refresh();
>>>>>>> a5523a6 (临时保存：切换到自己分支之前的未完成工作)
            OnPropertyChanged(nameof(ShouldShowMoreButton));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}
