using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeController.ViewModels;

namespace TimeController.Services
{
    /// <summary>
    /// 主题模式选项
    /// </summary>
    public enum ThemeOption
    {
        Light,
        Dark,
        System
    }

    /// <summary>
    /// 应用设置服务接口
    /// </summary>
    public interface ISettingsService
    {
        int LoadWeeklyTarget();
        void SaveWeeklyTarget(int value);

        ThemeOption LoadThemeOption();
        void SaveThemeOption(ThemeOption option);
    }
}
