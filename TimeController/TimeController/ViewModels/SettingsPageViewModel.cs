using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;
using TimeController.Services;
using TimeController.Models;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace TimeController.ViewModels
{

    public class SettingsPageViewModel : INotifyPropertyChanged
    {
        private ThemeOption _selectedThemeOption;
        private int _rewardThreshold;

        // 服务层，用于加载/保存
        private readonly ISettingsService _settingsService;
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }

        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty == value) return;
                _isDirty = value;
                OnPropertyChanged();
            }
        }

        public SettingsPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            // 读取两项初始值
            RewardThreshold = _settingsService.LoadWeeklyTarget();
            SelectedThemeOption = _settingsService.LoadThemeOption();

            // 一加载就视作“已保存”状态
            IsDirty = false;

            // 根据 SelectedThemeOption 立刻应用一次主题
            if (_selectedThemeOption == ThemeOption.System)
                SubscribeSystemThemeChange();
            else
                ApplyAppTheme(_selectedThemeOption == ThemeOption.Light);

            // 从存储中读取阈值，默认至少 4
            RewardThreshold = Math.Max(4, _settingsService.LoadWeeklyTarget());

            // 初始化命令
            SaveCommand = new RelayCommand<object?>(_ => OnSave());
            ResetCommand = new RelayCommand<object?>(_ => OnReset());

        }

        private void OnSave()
        {
            // 持久化
            _settingsService.SaveWeeklyTarget(RewardThreshold);
            _settingsService.SaveThemeOption(SelectedThemeOption);

            // 提示
            MessageBox.Show(
                "设置已保存！",
                "提示",
                MessageBoxButton.OK,
                MessageBoxImage.Asterisk);
                IsDirty = false;         // 保存后重置“脏”标记
        }

        private void OnReset()
        {
            // 二次确认
            var result = MessageBox.Show(
                "是否重置所有设置为默认？",
                "确认重置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);  // 默认焦点在“否”

            if (result == MessageBoxResult.Yes)
            {
                // 恢复默认值
                RewardThreshold = 4;                  // 你的默认阈值
                SelectedThemeOption = ThemeOption.Light;  // 默认日间模式

                // 持久化重置后的值
                _settingsService.SaveWeeklyTarget(RewardThreshold);
                _settingsService.SaveThemeOption(SelectedThemeOption);

                // 再次提示
                MessageBox.Show(
                    "已重置为默认设置。",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Asterisk);
                    IsDirty = false;         // 保存后重置“脏”标记
            }
        }


        /// <summary>
        /// 每周完成多少任务可获得奖励，同时也是设置页输入值
        /// </summary>
        public int RewardThreshold
        {
            get => _rewardThreshold;
            set
            {
                if (_rewardThreshold == value) return;
                _rewardThreshold = value;
                OnPropertyChanged();
                IsDirty = true;        // 标记为“未保存”
            }
        }

        /// <summary>
        /// 三种主题选项：Light / Dark / System
        /// </summary>
        public ThemeOption SelectedThemeOption
        {
            get => _selectedThemeOption;
            set
            {
                if (_selectedThemeOption == value) return;
                _selectedThemeOption = value;
                OnPropertyChanged();
                IsDirty = true;        // 标记为“未保存”
                _settingsService.SaveThemeOption(value);

                if (value == ThemeOption.System)
                    SubscribeSystemThemeChange();
                else
                {
                    UnsubscribeSystemThemeChange();
                    ApplyAppTheme(value == ThemeOption.Light);
                }
            }
        }


        #region 系统主题变化监听

        private void SubscribeSystemThemeChange()
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            // 首次同步一次
            ApplyAppTheme(GetCurrentSystemIsLight());
        }

        private void UnsubscribeSystemThemeChange()
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // Windows 主题改变时 e.Category 为 General
            if (e.Category == UserPreferenceCategory.General)
                ApplyAppTheme(GetCurrentSystemIsLight());
        }

        private bool GetCurrentSystemIsLight()
        {
            const string key = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            var value = Registry.GetValue(key, "AppsUseLightTheme", 1);
            return Convert.ToInt32(value) == 1;
        }

        private void ApplyAppTheme(bool isLight)
        {
            // 假设你项目里有两个 ResourceDictionary：LightTheme.xaml / DarkTheme.xaml
            var uri = new Uri($"/TimeController;component/Themes/{(isLight ? "Light" : "Dark")}Theme.xaml",
                              UriKind.Relative);
            var dict = new ResourceDictionary { Source = uri };
            // 替换第一个 MergedDictionary
            Application.Current.Resources.MergedDictionaries[0] = dict;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}
