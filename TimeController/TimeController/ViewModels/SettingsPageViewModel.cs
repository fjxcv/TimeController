using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using TimeController.Services;

namespace TimeController.ViewModels
{
    public class SettingsPageViewModel : INotifyPropertyChanged
    {
        private int _rewardThreshold;
        private bool _followSystemTheme;

        // 服务层，用于加载/保存
        private readonly ISettingsService _settingsService;

        public SettingsPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            // 从存储中读取阈值，默认至少 4
            RewardThreshold = Math.Max(4, _settingsService.LoadWeeklyTarget());
            FollowSystemTheme = _settingsService.LoadFollowSystemTheme();

            // 如果跟随系统，立即订阅通知
            if (FollowSystemTheme)
                SubscribeSystemThemeChange();
        }

        /// <summary>
        /// 每周完成多少任务可获得奖励，同时也是设置页输入值
        /// </summary>
        public int RewardThreshold
        {
            get => _rewardThreshold;
            set
            {
                if (value < 1) return;
                if (_rewardThreshold == value) return;
                _rewardThreshold = value;
                OnPropertyChanged();
                // 保存到同一个“WeeklyTarget”字段里
                _settingsService.SaveWeeklyTarget(value);
            }
        }

        public bool FollowSystemTheme
        {
            get => _followSystemTheme;
            set
            {
                if (_followSystemTheme == value) return;
                _followSystemTheme = value;
                OnPropertyChanged();
                _settingsService.SaveFollowSystemTheme(value);

                if (value) SubscribeSystemThemeChange();
                else UnsubscribeSystemThemeChange();
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
