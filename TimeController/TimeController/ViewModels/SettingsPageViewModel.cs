using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;
using TimeController.Services;
using TimeController.Models;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using Microsoft.Extensions.DependencyInjection;

namespace TimeController.ViewModels
{
    public class SettingsPageViewModel : INotifyPropertyChanged
    {
        private int _rewardThreshold; // 每周任务完成奖励门槛
        private int _dailyReviewPromptOption; // 用户选择的每日复盘提示时间：-1 表示关闭
        private bool _enableDailyReviewPrompt; // 是否启用每日复盘提醒
        private int _dailyReviewPromptHour; // 实际存储的提醒时间（小时）

        private readonly ISettingsService _settingsService; // 设置服务

        public ICommand SaveCommand { get; } // 保存设置命令
        public ICommand ResetCommand { get; } // 重置设置命令

        private bool _isDirty; // 标记设置是否被修改
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

            // 读取用户设置并初始化
            RewardThreshold = Math.Max(4, _settingsService.LoadWeeklyTarget());
            _enableDailyReviewPrompt = _settingsService.LoadEnableDailyReviewPrompt();
            _dailyReviewPromptHour = _settingsService.LoadDailyReviewPromptHour();
            _dailyReviewPromptOption = _enableDailyReviewPrompt ? _dailyReviewPromptHour : -1;

            IsDirty = false; // 默认状态为未更改

            // 初始化保存与重置命令
            SaveCommand = new RelayCommand<object?>(_ => OnSave());
            ResetCommand = new RelayCommand<object?>(_ => OnReset());
        }

        private void OnSave()
        {
            // 保存设置到注册表
            _settingsService.SaveWeeklyTarget(RewardThreshold);
            _settingsService.SaveEnableDailyReviewPrompt(_enableDailyReviewPrompt);
            _settingsService.SaveDailyReviewPromptHour(_dailyReviewPromptHour);

            // 通知咸鱼模式重新加载奖励进度
            var casualVm = App.Services.GetRequiredService<CasualModeViewModel>();
            casualVm.UpdateProgress();

            // 同步设置到运行时变量
            UserSettings.EnableDailyReviewPrompt = _enableDailyReviewPrompt;
            UserSettings.DailyReviewPromptHour = _dailyReviewPromptHour;

            // 弹出提示
            MessageBox.Show("设置已保存！", "提示", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            IsDirty = false; // 状态复原
        }

        private void OnReset()
        {
            // 提示用户确认是否重置
            var result = MessageBox.Show(
                "是否重置所有设置为默认？",
                "确认重置",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                // 重置为默认值
                RewardThreshold = 4;
                _enableDailyReviewPrompt = false;
                _dailyReviewPromptHour = 18;
                DailyReviewPromptOption = -1;

                // 保存重置值
                _settingsService.SaveWeeklyTarget(RewardThreshold);
                _settingsService.SaveEnableDailyReviewPrompt(_enableDailyReviewPrompt);
                _settingsService.SaveDailyReviewPromptHour(_dailyReviewPromptHour);

                UserSettings.EnableDailyReviewPrompt = _enableDailyReviewPrompt;
                UserSettings.DailyReviewPromptHour = _dailyReviewPromptHour;

                // 弹出提示
                MessageBox.Show("已重置为默认设置。", "提示", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                IsDirty = false;
            }
        }

        public int RewardThreshold
        {
            get => _rewardThreshold;
            set
            {
                if (_rewardThreshold == value) return;
                _rewardThreshold = value;
                OnPropertyChanged();
                IsDirty = true;
            }
        }

        public int DailyReviewPromptOption
        {
            get => _dailyReviewPromptOption;
            set
            {
                if (_dailyReviewPromptOption == value) return;
                _dailyReviewPromptOption = value;
                _enableDailyReviewPrompt = value >= 0;
                if (value >= 0)
                    _dailyReviewPromptHour = value;
                OnPropertyChanged();
                IsDirty = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


    }
}