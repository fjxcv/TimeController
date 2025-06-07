using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using TimeController.Models;

namespace TimeController.ViewModels
{
    public class CasualModeViewModel : INotifyPropertyChanged
    {
        // —— 奖励弹窗相关 —— 
        private bool _isRewardPopupOpen;
        public bool IsRewardPopupOpen
        {
            get => _isRewardPopupOpen;
            set
            {
                if (_isRewardPopupOpen != value)
                {
                    bool wasOpen = _isRewardPopupOpen;
                    _isRewardPopupOpen = value;
                    OnPropertyChanged(nameof(IsRewardPopupOpen));

                    // 从打开状态切换到关闭状态：重置进度
                    if (wasOpen && !_isRewardPopupOpen)
                    {
                        Progress = 0;
                        OnPropertyChanged(nameof(Progress));
                    }
                }
            }
        }

        public ICommand ToggleRewardPopupCommand { get; }
        public ICommand AddRewardTaskCommand { get; }

        // —— 进度与模块 —— 
        private int _progress;
        public int Progress
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

        // —— 奖励任务列表 —— 
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

        // —— 普通任务命令 —— 
        public ICommand ToggleTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand DeleteRewardTaskCommand { get; }

        // 新增：控制普通模块输入框可见性的Command
        public ICommand ToggleInputVisibilityCommand { get; }
        // 新增：添加普通任务的Command
        public ICommand AddTaskCommand { get; }

        // 新增：取消普通任务输入的Command
        public ICommand CancelInputTaskCommand { get; }

        // 新增：当前正在编辑的任务
        private TaskModel? _currentEditingTask;
        public TaskModel? CurrentEditingTask
        {
            get => _currentEditingTask;
            set
            {
                if (_currentEditingTask != value)
                {
                    _currentEditingTask = value;
                    OnPropertyChanged(nameof(CurrentEditingTask));
                }
            }
        }

        // 修改：开始编辑任务的Command
        public ICommand StartEditTaskCommand { get; }
        // 修改：结束编辑任务的Command
        public ICommand EndEditTaskCommand { get; }


        public CasualModeViewModel()
        {
            // 初始化模块
            Modules.Add(new ModuleViewModel { Name = "自我滋养", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "创造表达", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "生活杂务", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "人际连接", MaxTasks = 5 });
            Modules.Add(new ModuleViewModel { Name = "长期备忘" });

            // 普通任务命令
            ToggleTaskCommand = new RelayCommand<TaskModel>(ToggleTask);
            DeleteTaskCommand = new RelayCommand<TaskModel>(task =>
            {
                if (task != null)
                {
                    // 直接删除任务，不检查编辑状态
                    DeleteTask(task);
                }
            });
            DeleteRewardTaskCommand = new RelayCommand<TaskModel>(DeleteRewardTask);

            // 进度监听
            foreach (var module in Modules)
            {
                module.Tasks.CollectionChanged += (sender, e) =>
                {
                    // 处理新增任务的属性订阅
                    if (e.NewItems != null)
                    {
                        foreach (TaskModel task in e.NewItems)
                        {
                            task.PropertyChanged += Task_PropertyChanged;
                        }
                    }
                    // 处理移除任务的属性取消订阅
                    if (e.OldItems != null)
                    {
                        foreach (TaskModel task in e.OldItems)
                        {
                            task.PropertyChanged -= Task_PropertyChanged;
                        }
                    }

                    // 直接触发进度更新
                    UpdateProgress();
                };

                // 为初始化时已有的任务订阅事件
                foreach (var task in module.Tasks)
                {
                    task.PropertyChanged += Task_PropertyChanged;
                }
            }

            // 奖励弹窗命令
            ToggleRewardPopupCommand = new RelayCommand<object>(_ =>
            {
                IsRewardPopupOpen = !IsRewardPopupOpen;
                // 如果弹窗打开，延迟设置焦点到输入框 (此逻辑保留在View)
            });

            // 始终可执行的添加奖励任务
            AddRewardTaskCommand = new RelayCommand<object>(_ =>
            {
                AddRewardTask(NewRewardTaskText);
            });

            // 新增：初始化控制普通模块输入框可见性的Command
            ToggleInputVisibilityCommand = new RelayCommand<ModuleViewModel>(module =>
            {
                if (module != null)
                {
                    // 如果有任务正在编辑，先结束编辑
                    if (CurrentEditingTask != null)
                    {
                        CurrentEditingTask.IsEditing = false;
                        CurrentEditingTask = null;
                    }

                    // 隐藏所有其他模块的输入框
                    foreach (var m in Modules)
                    {
                        if (m != module && m.IsInputVisible)
                        {
                            m.IsInputVisible = false;
                        }
                    }

                    // 切换当前模块的输入框可见性
                    module.IsInputVisible = !module.IsInputVisible;
                }
            });

            // 新增：初始化添加普通任务的Command
            AddTaskCommand = new RelayCommand<ModuleViewModel>(module =>
            {
                if (module != null && !string.IsNullOrWhiteSpace(module.NewTaskText))
                {
                    AddTask(module, module.NewTaskText.Trim());
                    // AddTask方法内部会设置IsInputVisible为false并清空NewTaskText
                }
            });

            // 新增：初始化取消普通任务输入的Command
            CancelInputTaskCommand = new RelayCommand<ModuleViewModel>(module =>
            {
                if (module != null)
                {
                    module.IsInputVisible = false;
                    module.NewTaskText = string.Empty;
                }
            });

            // 修改：初始化开始编辑任务的Command
            StartEditTaskCommand = new RelayCommand<TaskModel>(task =>
            {
                if (task != null)
                {
                    // 隐藏所有模块的输入框
                    foreach (var m in Modules)
                    {
                        m.IsInputVisible = false;
                    }

                    // 如果有其他任务正在编辑，先结束编辑
                    if (CurrentEditingTask != null && CurrentEditingTask != task)
                    {
                        CurrentEditingTask.IsEditing = false;
                    }

                    // 直接进入编辑状态，不设置选中状态
                    task.IsEditing = true;
                    CurrentEditingTask = task;
                }
            });

            // 修改：初始化结束编辑任务的Command
            EndEditTaskCommand = new RelayCommand<TaskModel>(task =>
            {
                if (task != null)
                {
                    // Only perform delete logic if the task is currently in editing mode
                    if (task.IsEditing)
                    {
                        // This command is triggered by the Enter key.
                        // If the text is empty, delete the task.
                        if (string.IsNullOrWhiteSpace(task.Name))
                        {
                            DeleteTask(task);
                        }

                        // In either case (deleted or not), end editing
                        task.IsEditing = false;
                        if (CurrentEditingTask == task)
                        {
                            CurrentEditingTask = null;
                        }
                    }
                    // If not in editing mode, do nothing (this case shouldn't happen with current bindings)
                }
            });

            // 监听所有模块的任务列表变化
            foreach (var module in Modules)
            {
                module.Tasks.CollectionChanged += (sender, e) =>
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
                };
            }
        }

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskModel.IsCompleted))
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 删除此行：SortTasks(); 
                    // 触发进度更新（无论是否长期备忘模块）
                    var viewModel = Application.Current.MainWindow?.DataContext as CasualModeViewModel;
                    viewModel?.UpdateProgress();
                });
                Application.Current.Dispatcher.InvokeAsync(UpdateProgress);
            }
        }
        // 修改后的 DeleteTask 方法
        private void DeleteTask(TaskModel task)
        {
            var module = Modules.FirstOrDefault(m => m.Tasks.Contains(task));
            if (module != null)
            {
                module.Tasks.Remove(task);
                UpdateProgress(); // 直接触发更新
            }
        }

        // 修改后的 ToggleTask 方法
        // CasualModeViewModel.cs
        public void ToggleTask(TaskModel task)
        {
            task.IsCompleted = !task.IsCompleted;
            UpdateProgress(); // 无论任务属于哪个模块，直接触发更新

            // 如果任务完成且正在编辑，结束编辑
            if (task.IsCompleted && task.IsEditing)
            {
                task.IsEditing = false;
                CurrentEditingTask = null;
            }
        }
        // 删除奖励任务
        public void DeleteRewardTask(TaskModel task) =>
            RewardTasks.Remove(task);

        // 添加奖励任务
        public void AddRewardTask(string taskName)
        {
            if (!string.IsNullOrWhiteSpace(taskName))
            {
                RewardTasks.Add(new TaskModel { Name = taskName });
                NewRewardTaskText = string.Empty;
            }
        }

        // 添加普通任务
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
                // 在ViewModel中处理排序
                var sortedTasks = module.Tasks.OrderBy(t => t.IsCompleted).ToList();
                for (int i = 0; i < sortedTasks.Count; i++)
                {
                    int currentIndex = module.Tasks.IndexOf(sortedTasks[i]);
                    if (currentIndex != i)
                    {
                        module.Tasks.Move(currentIndex, i);
                    }
                }

                module.NewTaskText = string.Empty; // 清空输入框文本
                module.IsInputVisible = false; // 添加任务后隐藏输入框
                UpdateProgress();
            }
        }
        public void UpdateProgress()
        {
            int totalCompleted = Modules.Take(4).Sum(m => m.Tasks.Count(t => t.IsCompleted));

            // 计算余数
            int mod = totalCompleted % 4;
            // 如果刚好整除且 >0，就把进度显示为 4（满格），否则按照余数显示
            Progress = (mod == 0 && totalCompleted > 0) ? 4 : mod;
            OnPropertyChanged(nameof(Progress));

            // 当整除且大于0时打开弹窗
            if (totalCompleted > 0 && mod == 0)
            {
                IsRewardPopupOpen = true;
            }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public class ModuleViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(nameof(Name)); } }
        }

        private string _newTaskText = string.Empty;
        public string NewTaskText
        {
            get => _newTaskText;
            set { if (_newTaskText != value) { _newTaskText = value; OnPropertyChanged(nameof(NewTaskText)); } }
        }

        private int _maxTasks;
        public int MaxTasks
        {
            get => _maxTasks;
            set { if (_maxTasks != value) { _maxTasks = value; OnPropertyChanged(nameof(MaxTasks)); } }
        }

        private bool _isInputVisible;
        public bool IsInputVisible
        {
            get => _isInputVisible;
            set { if (_isInputVisible != value) { _isInputVisible = value; OnPropertyChanged(nameof(IsInputVisible)); } }
        }

        public ICommand ClearNewTaskCommand { get; }

        private ObservableCollection<TaskModel> _tasks = new();
        public ObservableCollection<TaskModel> Tasks
        {
            get => _tasks;
            set { if (_tasks != value) { _tasks = value; OnPropertyChanged(nameof(Tasks)); } }
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

        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskModel.IsCompleted))
            {
                // 在UI线程上执行排序和进度更新
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SortTasks();

                    // 仅在非长期备忘模块中触发进度更新
                    if (Name != "长期备忘")
                    {
                        var viewModel = Application.Current.MainWindow?.DataContext as CasualModeViewModel;
                        if (viewModel != null)
                        {
                            viewModel.UpdateProgress();
                        }
                    }
                });
            }
        }

        private void SortTasks()
        {
            // 使用简单的OrderBy进行排序，已完成的任务排在后面
            var sorted = Tasks.OrderBy(t => t.IsCompleted).ToList(); // 使用t.IsCompleted确保false在前
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
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public class NewEntry : INotifyPropertyChanged
    {
        public ModuleViewModel Parent { get; }
        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set { if (_text != value) { _text = value; OnPropertyChanged(nameof(Text)); } }
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
            set { if (_newRewardTaskText != value) { _newRewardTaskText = value; OnPropertyChanged(nameof(NewRewardTaskText)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
