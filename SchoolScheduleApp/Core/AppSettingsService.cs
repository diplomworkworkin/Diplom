using System;
using System.IO;
using System.Text.Json;

namespace SchoolScheduleApp.Core
{
    // Сервис для чтения/сохранения настроек в JSON
    public static class AppSettingsService
    {
        private static string SettingsPath
            => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return new AppSettings();

                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                return s ?? new AppSettings();
            }
            catch
            {
                // если файл битый/пустой — возвращаем значения по умолчанию
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SettingsPath, json);
        }
    }
}
