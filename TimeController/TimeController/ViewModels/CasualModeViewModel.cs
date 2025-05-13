using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;
using TimeController.ViewModels;
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
            // 初始化四个模块
            Modules.Add(new ModuleViewModel { Name = "自我滋养", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "创造表达", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "生活杂务", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "人际连接", MaxTasks = 5 });

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
            // 只对当前模块排序
            var module = Modules.FirstOrDefault(m => m.Tasks.Contains(task));
            if (module != null)
            {
                var sorted = module.Tasks.OrderBy(t => t.IsCompleted).ToList();
                module.Tasks.Clear();
                foreach (var t in sorted)
                    module.Tasks.Add(t);
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
            if (module.Tasks.Count >= module.MaxTasks)
            {
                System.Windows.MessageBox.Show($"每个模块最多只能添加{module.MaxTasks}个任务！", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
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

        public ICommand ClearNewTaskCommand { get; }

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

        public ModuleViewModel()
        {
            ClearNewTaskCommand = new RelayCommand<object>(_ => NewTaskText = string.Empty);
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class CasualTaskItem 
    {
        public string Name { get; set; }
        public string Status { get; set; } // 推迟 / 放弃
    }

    //以下的任务状态，咸鱼只用已完成和待处理
    public enum MyTaskStatus
    {
        Pending,    // 待处理
        Completed,  // 已完成
        Postponed,  // 已推迟
        Abandoned   // 已放弃
    }
    
    public class TaskModel : INotifyPropertyChanged
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

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        private DateTime _plannedDate;
        public DateTime PlannedDate
        {
            get => _plannedDate;
            set
            {
                if (_plannedDate != value)
                {
                    _plannedDate = value;
                    OnPropertyChanged(nameof(PlannedDate));
                }
            }
        }

        private bool _isAllDay;
        public bool IsAllDay
        {
            get => _isAllDay;
            set
            {
                if (_isAllDay != value)
                {
                    _isAllDay = value;
                    OnPropertyChanged(nameof(IsAllDay));
                }
            }
        }

        private DateTime? _startTime;
        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                if (_startTime != value)
                {
                    _startTime = value;
                    OnPropertyChanged(nameof(StartTime));
                }
            }
        }

        private DateTime? _endTime;
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                if (_endTime != value)
                {
                    _endTime = value;
                    OnPropertyChanged(nameof(EndTime));
                }
            }
        }

        private MyTaskStatus _status = MyTaskStatus.Pending;
        public MyTaskStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }



}

// 定义一个输入行模型
public class NewEntry : INotifyPropertyChanged
{
    public ModuleViewModel Parent { get; }
    private string _text = "";
    public string Text { get => _text; set { _text = value; OnPropertyChanged(nameof(Text)); } }

    public NewEntry(ModuleViewModel parent) => Parent = parent;

    // 按回车时调用
    public void Commit()
    {
        if (!string.IsNullOrWhiteSpace(Text))
        {
            Parent.Tasks.Add(new TaskModel { Name = Text });
            Text = "";

        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
