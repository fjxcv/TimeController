using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace TimeController.Services
{
    public class AppSettingsService : ISettingsService
    {
        public int LoadWeeklyTarget() =>
            int.TryParse(ConfigurationManager.AppSettings["WeeklyTarget"], out var v) ? v : 4;

        public void SaveWeeklyTarget(int value)
        {
            // 打开配置
            var config = ConfigurationManager
                           .OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;

            if (settings["WeeklyTarget"] == null)
            {
                // 配置里没有，就添加
                settings.Add("WeeklyTarget", value.ToString());
            }
            else
            {
                // 已经有，就修改
                settings["WeeklyTarget"].Value = value.ToString();
            }

            // 保存并刷新
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }


        public bool LoadFollowSystemTheme() =>
            bool.TryParse(ConfigurationManager.AppSettings["FollowSystemTheme"], out var v) && v;

        public void SaveFollowSystemTheme(bool on)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["FollowSystemTheme"].Value = on.ToString();
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}