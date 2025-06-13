using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using TimeController.Models;

namespace TimeController.ViewModels
{
    public class CasualModeViewModel : INotifyPropertyChanged
    {

        // 记录上次重置时的年和周（默认 0，表示还没重置过）
        private int _lastResetYear = 0;
        private int _lastResetWeek = 0;
        public int LastResetYear
        {
            get => _lastResetYear;
            private set
            {
                if (_lastResetYear != value)
                {
                    _lastResetYear = value;
                    OnPropertyChanged(nameof(LastResetYear));
                }
            }
        }
        public int LastResetWeek
        {
            get => _lastResetWeek;
            private set
            {
                if (_lastResetWeek != value)
                {
                    _lastResetWeek = value;
                    OnPropertyChanged(nameof(LastResetWeek));
                }
            }
        }

        // —— 可配置阈值，每周默认完成4个有奖励 —— 
        private int _rewardThreshold = 4;
        public int RewardThreshold
        {
            get => _rewardThreshold;
            set
            {
                if (_rewardThreshold != value)
                {
                    _rewardThreshold = value;
                    OnPropertyChanged(nameof(RewardThreshold));
                }
            }
        }

        // —— 是否已领取过奖励 —— 
        private bool _hasRewarded = false;
        public bool HasRewarded
        {
            get => _hasRewarded;
            private set
            {
                if (_hasRewarded != value)
                {
                    _hasRewarded = value;
                    OnPropertyChanged(nameof(HasRewarded));
                }
            }
        }


        public ICommand ToggleRewardPopupCommand { get; }
        public ICommand AddRewardTaskCommand { get; }

        // —— 普通任务命令 —— 
        public ICommand ToggleTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand DeleteRewardTaskCommand { get; }

        //控制普通模块输入框可见性的Command
        public ICommand ToggleInputVisibilityCommand { get; }

        // 添加普通任务的Command
        public ICommand AddTaskCommand { get; }

        //取消普通任务输入的Command
        public ICommand CancelInputTaskCommand { get; }

        // 开始编辑任务的Command
        public ICommand StartEditTaskCommand { get; }

        // 结束编辑任务的Command
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
            // 删除任务命令
            DeleteTaskCommand = new RelayCommand<TaskModel>(task =>
            {
                if (task != null)
                {
                    // 直接删除任务，不检查编辑状态
                    DeleteTask(task);
                }
            });
            //普通模块输入框是否可见
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

            //编辑普通任务
            // 初始化添加普通任务的Command
            AddTaskCommand = new RelayCommand<ModuleViewModel>(module =>
            {
                if (module != null && !string.IsNullOrWhiteSpace(module.NewTaskText))
                {
                    AddTask(module, module.NewTaskText.Trim());
                    // AddTask方法内部会设置IsInputVisible为false并清空NewTaskText
                }
            });

            // 初始化取消普通任务输入的Command
            CancelInputTaskCommand = new RelayCommand<ModuleViewModel>(module =>
            {
                if (module != null)
                {
                    module.IsInputVisible = false;
                    module.NewTaskText = string.Empty;
                }
            });

            // 初始化开始编辑任务的Command
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

            // 初始化结束编辑任务的Command
            EndEditTaskCommand = new RelayCommand<TaskModel>(task =>
            {
                if (task != null)
                {
                    if (task.IsEditing)
                    {
                        if (string.IsNullOrWhiteSpace(task.Name))
                        {
                            DeleteTask(task);
                        }
                        task.IsEditing = false;
                        if (CurrentEditingTask == task)
                        {
                            CurrentEditingTask = null;
                        }
                    }
                }
            });

            //奖励弹窗相关

            // 删除奖励任务命令
            DeleteRewardTaskCommand = new RelayCommand<TaskModel>(DeleteRewardTask);
            // 奖励弹窗命令
            ToggleRewardPopupCommand = new RelayCommand<object>(_ =>
            {
                IsRewardPopupOpen = !IsRewardPopupOpen;
            });
            //添加奖励任务
            AddRewardTaskCommand = new RelayCommand<object>(_ =>
            {
                AddRewardTask(NewRewardTaskText);
            });

            // 订阅模块任务改变事件
            foreach (var module in Modules)
            {
                module.Tasks.CollectionChanged += (s, e) =>
                {
                    // 新增任务时订阅属性变化
                    if (e.NewItems != null)
                    {
                        foreach (TaskModel t in e.NewItems)
                        {
                            t.PropertyChanged += Task_PropertyChanged;
                        }
                    }
                    // 移除任务时解绑属性变化
                    if (e.OldItems != null)
                    {
                        foreach (TaskModel t in e.OldItems)
                        {
                            t.PropertyChanged -= Task_PropertyChanged;
                        }
                    }
                    // 任何任务列表变化都要刷新进度
                    UpdateProgress();
                };

                // 初始化时已有任务订阅
                foreach (var t in module.Tasks)
                {
                    t.PropertyChanged += Task_PropertyChanged;
                }
            }

            // 应用启动时先检查一次：如果跨周就重置
            CheckAndPerformWeeklyReset();

            // 启动"每天零点检查"定时器
            StartDailyResetTimer();

        }

        private void CheckAndPerformWeeklyReset()
        {
            DateTime now = DateTime.Now;
            CultureInfo ci = CultureInfo.CurrentCulture;
            // 按照"周一为一周第一天"的规则计算当前周数
            int thisWeek = ci.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            int thisYear = now.Year;

            // 如果当前年-周与上次记录的不同，就说明进入新一周，需要重置
            if (thisYear != LastResetYear || thisWeek != LastResetWeek)
            {
                PerformWeeklyReset();
                LastResetYear = thisYear;
                LastResetWeek = thisWeek;
            }
        }

        //清除已完成的任务 + 重置进度和奖励状态
        private void PerformWeeklyReset()
        {
            // 1. 遍历前 4 个模块，移除所有 IsCompleted == true 的任务
            foreach (var module in Modules.Take(4))
            {
                var completedTasks = module.Tasks.Where(t => t.IsCompleted).ToList();
                foreach (var t in completedTasks)
                {
                    module.Tasks.Remove(t);
                }
            }

            // 2. 重置进度条和奖励标记
            Progress = 0;
            OnPropertyChanged(nameof(Progress));

            HasRewarded = false;

        }


        // —— 定时器：每天零点触发一次 CheckAndPerformWeeklyReset() —— 
        private DispatcherTimer _dailyTimer;
        private void StartDailyResetTimer()
        {
            DateTime now = DateTime.Now;
            // 下一个零点时刻
            DateTime nextMidnight = now.Date.AddDays(1);
            TimeSpan untilMidnight = nextMidnight - now;

            // 第一个短定时器，等到当天零点
            var startTimer = new DispatcherTimer { Interval = untilMidnight };
            startTimer.Tick += (s, e) =>
            {
                startTimer.Stop();
                // 零点到，先检查一次
                CheckAndPerformWeeklyReset();

                // 再启动一个 24 小时周期的定时器
                _dailyTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(24) };
                _dailyTimer.Tick += (s2, e2) => CheckAndPerformWeeklyReset();
                _dailyTimer.Start();
            };
            startTimer.Start();
        }


        // —— 奖励弹窗是否打开 —— 
        private bool _isRewardPopupOpen;
        public bool IsRewardPopupOpen
        {
            get => _isRewardPopupOpen;
            set
            {
                if (_isRewardPopupOpen != value)
                {
                    _isRewardPopupOpen = value;
                    OnPropertyChanged(nameof(IsRewardPopupOpen));

                }
            }
        }

        // —— 进度值 —— 
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
        //弹窗新任务的添加
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

        // 当前正在编辑的任务
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
        private void Task_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TaskModel.IsCompleted))
            {
                // UI 线程调用
                Application.Current.Dispatcher.InvokeAsync(UpdateProgress);
            }
        }

        //切换任务
        public void ToggleTask(TaskModel task)
        {
            task.IsCompleted = !task.IsCompleted;
            UpdateProgress(); // 取消或恢复完成状态时

            // 任务完成且正在编辑，结束编辑
            if (task.IsCompleted && task.IsEditing)
            {
                task.IsEditing = false;
                CurrentEditingTask = null;
            }
        }

        private void DeleteTask(TaskModel task)
        {
            var module = Modules.FirstOrDefault(m => m.Tasks.Contains(task));
            if (module != null)
            {
                module.Tasks.Remove(task);
                UpdateProgress(); // 直接触发更新
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
            // 1. 先检查是否要做本周重置（如果你启用了跨周重置，这里保持不变）
            CheckAndPerformWeeklyReset();

            // 2. 统计前四个模块里当前已完成任务的总数
            int totalCompleted = Modules.Take(4).Sum(m => m.Tasks.Count(t => t.IsCompleted));

            // 3. 如果之前已经发过奖励，但现在"完成数"被撤销导致少于阈值，就把 HasRewarded 置回 false
            if (HasRewarded && totalCompleted < RewardThreshold)
            {
                HasRewarded = false;
            }

            // 4. 如果还没发奖励，且已经达到阈值，就弹窗并设为已发放
            if (!HasRewarded && totalCompleted >= RewardThreshold)
            {
                Progress = RewardThreshold;
                OnPropertyChanged(nameof(Progress));

                OnShowRewardCelebration?.Invoke(); // 只调用事件显示奖励庆祝窗口
                HasRewarded = true;
            }
            else
            {
                // 5. 如果发过奖励但仍 >= 阈值，就保持满格；否则实时显示「已完成数」
                if (HasRewarded && totalCompleted >= RewardThreshold)
                {
                    Progress = RewardThreshold;
                }
                else
                {
                    // HasRewarded == false && totalCompleted < 阈值  的情况
                    Progress = totalCompleted;
                }
                OnPropertyChanged(nameof(Progress));
            }
        }

        public Action? OnShowRewardCelebration; // 新增：用于通知View层显示奖励庆祝窗口

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
            //已完成的任务排在后面
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
