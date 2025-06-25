using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace TimeController.Services
{
    public class JsonSettingsService
    {
        private readonly string _configPath =
            Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private readonly ReaderWriterLockSlim _lock = new();

        // 内部模型——只在这里定义默认值
        private class Config
        {
            public int WeeklyTarget { get; set; } = 4;
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public bool EnableDailyReviewPrompt { get; set; } = false;
            public int DailyReviewPromptHour { get; set; } = 18;
        }


        public JsonSettingsService()
        {
            // 如果文件不存在或为空，写一份默认的
            if (!File.Exists(_configPath) || new FileInfo(_configPath).Length == 0)
                SaveConfig(new Config());
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

    }
}