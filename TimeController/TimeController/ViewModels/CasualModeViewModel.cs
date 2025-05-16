using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TimeController.Models;

namespace TimeController.ViewModels
{
    public class CasualModeViewModel : INotifyPropertyChanged
    {
        private double _progress;
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged(nameof(Progress));
                }
            }
        }

        private ObservableCollection<ModuleViewModel> _modules = new();
        public ObservableCollection<ModuleViewModel> Modules
        {
            get => _modules;
            set
            {
                if (_modules != value)
                {
                    _modules = value;
                    OnPropertyChanged(nameof(Modules));
                }
            }
        }

        public ICommand ToggleTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public CasualModeViewModel()
        {
            Modules.Add(new ModuleViewModel { Name = "自我滋养", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "创造表达", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "生活杂务", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "人际连接", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "长期备忘" });

            ToggleTaskCommand = new RelayCommand<TaskModel>(ToggleTask);
            DeleteTaskCommand = new RelayCommand<TaskModel>(DeleteTask);

            foreach (var module in Modules)
            {
                module.Tasks.CollectionChanged += (_, __) => UpdateProgress();
            }
        }

        public void DeleteTask(TaskModel task)
        {
            var module = Modules.FirstOrDefault(m => m.Tasks.Contains(task));
            if (module != null)
            {
                module.Tasks.Remove(task);
                UpdateProgress();
            }
        }

        public void ToggleTask(TaskModel task)
        {
            task.IsCompleted = !task.IsCompleted;
            var module = Modules.FirstOrDefault(m => m.Tasks.Contains(task));
            if (module != null)
            {
                int currentIndex = module.Tasks.IndexOf(task);
                int newIndex = task.IsCompleted
                    ? module.Tasks.Count - 1
                    : module.Tasks.TakeWhile(t => !t.IsCompleted).Count();

                if (currentIndex != newIndex)
                {
                    module.Tasks.Move(currentIndex, newIndex);
                }
            }
            UpdateProgress();
        }

        public void UpdateProgress()
        {
            int totalTasks = Modules.Sum(m => m.Tasks.Count);
            int completedTasks = Modules.Sum(m => m.Tasks.Count(t => t.IsCompleted));
            Progress = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;
        }

        public void AddTask(ModuleViewModel module, string taskName)
        {
            if (module.Name != "长期备忘" && module.Tasks.Count >= module.MaxTasks)
            {
                MessageBox.Show($"每个模块最多只能添加{module.MaxTasks}个任务！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(taskName))
            {
                module.Tasks.Add(new TaskModel { Name = taskName });
                module.NewTaskText = string.Empty;
                module.IsInputVisible = false;
                UpdateProgress();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ModuleViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        private string _newTaskText = string.Empty;
        public string NewTaskText
        {
            get => _newTaskText;
            set
            {
                if (_newTaskText != value)
                {
                    _newTaskText = value;
                    OnPropertyChanged(nameof(NewTaskText));
                }
            }
        }

        private int _maxTasks;
        public int MaxTasks
        {
            get => _maxTasks;
            set
            {
                if (_maxTasks != value)
                {
                    _maxTasks = value;
                    OnPropertyChanged(nameof(MaxTasks));
                }
            }
        }

        private bool _isInputVisible;
        public bool IsInputVisible
        {
            get => _isInputVisible;
            set
            {
                if (_isInputVisible != value)
                {
                    _isInputVisible = value;
                    OnPropertyChanged(nameof(IsInputVisible));
                }
            }
        }

        public ICommand ClearNewTaskCommand { get; }

        private ObservableCollection<TaskModel> _tasks = new();
        public ObservableCollection<TaskModel> Tasks
        {
            get => _tasks;
            set
            {
                if (_tasks != value)
                {
                    _tasks = value;
                    OnPropertyChanged(nameof(Tasks));
                }
            }
        }

        public ModuleViewModel()
        {
            ClearNewTaskCommand = new RelayCommand<object>(_ => NewTaskText = string.Empty);
            Tasks.CollectionChanged += Tasks_CollectionChanged;
        }

        private void Tasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SortTasks();
        }

        public void SortTasks()
        {
            var sorted = Tasks.OrderBy(t => t.IsCompleted).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int currentIndex = Tasks.IndexOf(sorted[i]);
                if (currentIndex != i)
                    Tasks.Move(currentIndex, i);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class NewEntry : INotifyPropertyChanged
    {
        public ModuleViewModel Parent { get; }

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }

        public NewEntry(ModuleViewModel parent) => Parent = parent;

        public void Commit()
        {
            if (!string.IsNullOrWhiteSpace(Text))
            {
                Parent.Tasks.Add(new TaskModel { Name = Text });
                Text = string.Empty;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
