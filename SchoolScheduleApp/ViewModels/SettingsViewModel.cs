using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolScheduleApp.Core;
using System;
using System.IO;
using System.Windows;

namespace SchoolScheduleApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;

        public SettingsViewModel()
        {
            _settings = AppSettingsService.Load();

            // применяем тему сразу после загрузки
            ThemeManager.SetTheme(_settings.IsDarkTheme);

            _isDarkTheme = _settings.IsDarkTheme;
            _schoolName = _settings.SchoolName;
            _lessonDuration = _settings.LessonDuration;
            _startTime = _settings.StartTime;

            SaveCommand = new RelayCommand(_ => ExecuteSave());
            BackupCommand = new RelayCommand(_ => ExecuteBackup());
        }

        // === Свойства настроек ===

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged();

                    ThemeManager.SetTheme(_isDarkTheme);
                }
            }
        }

        private string _schoolName;
        public string SchoolName
        {
            get => _schoolName;
            set { _schoolName = value; OnPropertyChanged(); }
        }

        private int _lessonDuration;
        public int LessonDuration
        {
            get => _lessonDuration;
            set { _lessonDuration = value; OnPropertyChanged(); }
        }

        private string _startTime;
        public string StartTime
        {
            get => _startTime;
            set { _startTime = value; OnPropertyChanged(); }
        }

        // === Команды ===
        public RelayCommand SaveCommand { get; }
        public RelayCommand BackupCommand { get; }

        private void ExecuteSave()
        {
            // небольшая валидация, чтобы в дипломе выглядело логично
            if (string.IsNullOrWhiteSpace(SchoolName))
            {
                MessageBox.Show("Название учреждения не может быть пустым.", "Ошибка");
                return;
            }

            if (LessonDuration <= 0 || LessonDuration > _settings.MaxLessonDuration)
            {
                MessageBox.Show("Некорректная длительность урока.", "Ошибка");
                return;
            }

            if (string.IsNullOrWhiteSpace(StartTime) || !TimeSpan.TryParse(StartTime, out _))
            {
                MessageBox.Show("Начало первого урока должно быть в формате ЧЧ:ММ (например 08:00).", "Ошибка");
                return;
            }

            _settings.IsDarkTheme = IsDarkTheme;
            _settings.SchoolName = SchoolName.Trim();
            _settings.LessonDuration = LessonDuration;
            _settings.StartTime = StartTime.Trim();

            AppSettingsService.Save(_settings);

            MessageBox.Show("Настройки сохранены (settings.json).", "Система", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteBackup()
        {
            try
            {
                // Делаем BACKUP DATABASE через SQL
                var backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                Directory.CreateDirectory(backupFolder);

                var fileName = $"School11_Schedule_DB_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var fullPath = Path.Combine(backupFolder, fileName);

                using var db = new SchoolDbContext();

                // важно: путь должен быть в одинарных кавычках
                var sql = $"BACKUP DATABASE [School11_Schedule_DB] TO DISK = N'{fullPath}' WITH INIT";
                db.Database.ExecuteSqlRaw(sql);

                MessageBox.Show($"Бэкап создан:\n{fullPath}", "Бэкап", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось создать бэкап.\n\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
