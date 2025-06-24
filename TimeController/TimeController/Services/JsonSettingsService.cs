using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace TimeController.Services
{
    public class JsonSettingsService : ISettingsService
    {
        private readonly string _configPath =
            Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private readonly ReaderWriterLockSlim _lock = new();

        // 内部模型——只在这里定义默认值
        private class Config
        {
            public int WeeklyTarget { get; set; } = 4;
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public ThemeOption ThemeOption { get; set; } = ThemeOption.Light;
            public bool EnableDailyReviewPrompt { get; set; } = false;
            public int DailyReviewPromptHour { get; set; } = 18;
        }


        public JsonSettingsService()
        {
            // 如果文件不存在或为空，写一份默认的
            if (!File.Exists(_configPath) || new FileInfo(_configPath).Length == 0)
                SaveConfig(new Config());
        }

        private Config LoadConfig()
        {
            _lock.EnterReadLock();
            try
            {
                var json = File.ReadAllText(_configPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new Config();
                return JsonSerializer.Deserialize<Config>(json)
                       ?? new Config();
            }
            catch
            {
                return new Config();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void SaveConfig(Config cfg)
        {
            _lock.EnterWriteLock();
            try
            {
                var opts = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var json = JsonSerializer.Serialize(cfg, opts);
                File.WriteAllText(_configPath, json);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        // —— ISettingsService 接口实现 —— 

        public int LoadWeeklyTarget() =>
            LoadConfig().WeeklyTarget;

        public void SaveWeeklyTarget(int value)
        {
            var cfg = LoadConfig();
            cfg.WeeklyTarget = value;
            SaveConfig(cfg);
        }

        public ThemeOption LoadThemeOption() =>
            LoadConfig().ThemeOption;

        public void SaveThemeOption(ThemeOption option)
        {
            var cfg = LoadConfig();
            cfg.ThemeOption = option;
            SaveConfig(cfg);
        }

        public bool LoadEnableDailyReviewPrompt() =>
            LoadConfig().EnableDailyReviewPrompt;

        public void SaveEnableDailyReviewPrompt(bool value)
        {
            var cfg = LoadConfig();
            cfg.EnableDailyReviewPrompt = value;
            SaveConfig(cfg);
        }

        public int LoadDailyReviewPromptHour() =>
            LoadConfig().DailyReviewPromptHour;

        public void SaveDailyReviewPromptHour(int hour)
        {
            var cfg = LoadConfig();
            cfg.DailyReviewPromptHour = hour;
            SaveConfig(cfg);
        }

    }
}