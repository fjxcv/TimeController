using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TimeController.Models;
using System.Windows.Threading;

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

        private ObservableCollection<TaskModel> _rewardTasks = new();
        public ObservableCollection<TaskModel> RewardTasks
        {
            get => _rewardTasks;
            set
            {
                if (_rewardTasks != value)
                {
                    _rewardTasks = value;
                    OnPropertyChanged(nameof(RewardTasks));
                }
            }
        }

        private string _newRewardTaskText = string.Empty;
        public string NewRewardTaskText
        {
            get => _newRewardTaskText;
            set
            {
                if (_newRewardTaskText != value)
                {
                    _newRewardTaskText = value;
                    OnPropertyChanged(nameof(NewRewardTaskText));
                }
            }
        }

        public ICommand ToggleTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand DeleteRewardTaskCommand { get; }

        public CasualModeViewModel()
        {
            Modules.Add(new ModuleViewModel { Name = "自我滋养", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "创造表达", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "生活杂务", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "人际连接", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "长期备忘" });

            ToggleTaskCommand = new RelayCommand<TaskModel>(ToggleTask);
            DeleteTaskCommand = new RelayCommand<TaskModel>(DeleteTask);
            DeleteRewardTaskCommand = new RelayCommand<TaskModel>(DeleteRewardTask);

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

        // CasualModeViewModel.cs中的ToggleTask方法
        public void ToggleTask(TaskModel task)
        {
            task.IsCompleted = !task.IsCompleted;
            // 移除了手动排序，由属性变化事件触发
        }

        public void DeleteRewardTask(TaskModel task)
        {
            RewardTasks.Remove(task);
        }

        public void AddRewardTask(string taskName)
        {
            if (!string.IsNullOrWhiteSpace(taskName))
            {
                RewardTasks.Add(new TaskModel { Name = taskName });
                NewRewardTaskText = string.Empty;
            }
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
                var newTask = new TaskModel { Name = taskName };
                module.Tasks.Add(newTask);

                var sortedTasks = module.Tasks.OrderBy(t => t.IsCompleted).ToList();
                for (int i = 0; i < sortedTasks.Count; i++)
                {
                    int currentIndex = module.Tasks.IndexOf(sortedTasks[i]);
                    if (currentIndex != i)
                    {
                        module.Tasks.Move(currentIndex, i);
                    }
                }

                module.NewTaskText = string.Empty;
                module.IsInputVisible = false;
                UpdateProgress();
            }
        }


        // CasualModeViewModel.cs 中的 UpdateProgress 方法
        public void UpdateProgress()
        {
            // 排除长期备忘模块（索引4）
            var activeModules = Modules.Take(4).ToList();
            int totalTasks = activeModules.Sum(m => m.Tasks.Count);
            int completedTasks = activeModules.Sum(m => m.Tasks.Count(t => t.IsCompleted));
            Progress = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0;
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
        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (TaskModel task in e.NewItems)
                {
                    task.PropertyChanged += Task_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (TaskModel task in e.OldItems)
                {
                    task.PropertyChanged -= Task_PropertyChanged;
                }
            }
        }

        // ModuleViewModel.cs 中的 Task_PropertyChanged 方法
        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskModel.IsCompleted))
            {
                SortTasks();

                // 仅在非长期备忘模块中触发进度更新
                if (Name != "长期备忘")
                {
                    (Application.Current.MainWindow?.DataContext as CasualModeViewModel)?.UpdateProgress();
                }
            }
        }

        private void SortTasks()
        {
            var sorted = Tasks.OrderBy(t => t.IsCompleted).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                int currentIndex = Tasks.IndexOf(sorted[i]);
                if (currentIndex != i)
                {
                    Tasks.Move(currentIndex, i);
                }
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

        private string _newRewardTaskText = string.Empty;
        public string NewRewardTaskText
        {
            get => _newRewardTaskText;
            set
            {
                if (_newRewardTaskText != value)
                {
                    _newRewardTaskText = value;
                    OnPropertyChanged(nameof(NewRewardTaskText));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}