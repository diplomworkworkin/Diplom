using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SchoolScheduleApp.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;
        private readonly UserRole _currentRole;

        public SettingsViewModel()
        {
            _settings = AppSettingsService.Load();
            _currentRole = UserSession.CurrentUser?.Role ?? UserRole.Student;

            ThemeManager.SetTheme(_settings.IsDarkTheme);

            _isDarkTheme = _settings.IsDarkTheme;
            _schoolName = _settings.SchoolName;
            _lessonDuration = _settings.LessonDuration;
            _startTime = _settings.StartTime;

            SaveCommand = new RelayCommand(_ => ExecuteSave());
            BackupCommand = new RelayCommand(_ => ExecuteBackup(), _ => CanManageAcademicSettings);
            ExportCsvCommand = new RelayCommand(_ => ExecuteExportCsv(), _ => CanExportSchedule);
            ExportWordCommand = new RelayCommand(_ => ExecuteExportWord(), _ => CanExportSchedule);
        }

        public bool CanManageAcademicSettings => _currentRole == UserRole.Admin;
        public bool CanExportSchedule => _currentRole == UserRole.Admin || _currentRole == UserRole.Teacher;

        private bool _isDarkTheme;
        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme == value)
                {
                    return;
                }

                _isDarkTheme = value;
                OnPropertyChanged();
                ThemeManager.SetTheme(_isDarkTheme);
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

        public RelayCommand SaveCommand { get; }
        public RelayCommand BackupCommand { get; }
        public RelayCommand ExportCsvCommand { get; }
        public RelayCommand ExportWordCommand { get; }

        private void ExecuteSave()
        {
            if (CanManageAcademicSettings && string.IsNullOrWhiteSpace(SchoolName))
            {
                ToastService.Show("Название учреждения не может быть пустым.", "Ошибка", true);
                return;
            }

            if (CanManageAcademicSettings && (LessonDuration <= 0 || LessonDuration > _settings.MaxLessonDuration))
            {
                ToastService.Show("Некорректная длительность урока.", "Ошибка", true);
                return;
            }

            if (CanManageAcademicSettings && (string.IsNullOrWhiteSpace(StartTime) || !TimeSpan.TryParse(StartTime, out _)))
            {
                ToastService.Show("Начало первого урока должно быть в формате ЧЧ:ММ (например 08:00).", "Ошибка", true);
                return;
            }

            _settings.IsDarkTheme = IsDarkTheme;

            if (CanManageAcademicSettings)
            {
                _settings.SchoolName = SchoolName.Trim();
                _settings.LessonDuration = LessonDuration;
                _settings.StartTime = StartTime.Trim();
            }

            AppSettingsService.Save(_settings);
            ToastService.Show("Настройки сохранены (settings.json).", "Система");
        }

        private void ExecuteBackup()
        {
            if (!CanManageAcademicSettings)
            {
                ToastService.Show("Резервное копирование доступно только администратору.", "Доступ", true);
                return;
            }

            try
            {
                var backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                Directory.CreateDirectory(backupFolder);

                var fileName = $"School11_Schedule_DB_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var fullPath = Path.Combine(backupFolder, fileName);

                using var db = new SchoolDbContext();
                var sql = $"BACKUP DATABASE [School11_Schedule_DB] TO DISK = N'{fullPath}' WITH INIT";
                db.Database.ExecuteSqlRaw(sql);

                ToastService.Show($"Бэкап создан: {fullPath}", "Бэкап");
            }
            catch (Exception ex)
            {
                ToastService.Show("Не удалось создать бэкап. " + ex.Message, "Ошибка", true);
            }
        }

        private void ExecuteExportCsv()
        {
            ExecuteExport("CSV (*.csv)|*.csv", "schedule_export.csv", ExportToCsv);
        }

        private void ExecuteExportWord()
        {
            ExecuteExport("Word (*.doc)|*.doc", "schedule_export.doc", ExportToWordDoc);
        }

        private void ExecuteExport(string filter, string defaultFileName, Action<string, IReadOnlyCollection<LessonExportRow>> exportAction)
        {
            if (!CanExportSchedule)
            {
                ToastService.Show("Экспорт доступен только учителю и администратору.", "Доступ", true);
                return;
            }

            using var db = new SchoolDbContext();
            var rows = BuildExportRows(db);
            if (rows.Count == 0)
            {
                ToastService.Show("Нет данных для экспорта.", "Экспорт", true);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                EnsureCanOverwrite(dialog.FileName);
                exportAction(dialog.FileName, rows);
                ToastService.Show($"Файл сохранён: {dialog.FileName}", "Экспорт");
            }
            catch (Exception ex)
            {
                ToastService.Show("Ошибка экспорта: " + ex.Message, "Ошибка", true);
            }
        }

        private List<LessonExportRow> BuildExportRows(SchoolDbContext db)
        {
            var query = db.Lessons
                .Include(x => x.AcademicClass)
                .Include(x => x.Subject)
                .Include(x => x.Teacher)
                .Include(x => x.Classroom)
                .AsQueryable();

            if (_currentRole == UserRole.Teacher)
            {
                var teacherId = UserSession.CurrentUser?.TeacherId;
                if (!teacherId.HasValue)
                {
                    return [];
                }

                query = query.Where(x => x.TeacherId == teacherId.Value);
            }

            return query
                .OrderBy(x => x.AcademicClass.Name)
                .ThenBy(x => x.DayOfWeek)
                .ThenBy(x => x.LessonIndex)
                .Select(x => new LessonExportRow
                {
                    Day = x.DayOfWeek,
                    LessonNumber = x.LessonIndex,
                    ClassName = x.AcademicClass.Name,
                    Subject = x.Subject.Name,
                    Teacher = x.Teacher.FullName,
                    Room = x.Classroom != null ? x.Classroom.Number : "-"
                })
                .ToList();
        }

        private static void ExportToCsv(string filePath, IReadOnlyCollection<LessonExportRow> rows)
        {
            var byClass = BuildClassTables(rows);
            var sb = new StringBuilder();

            foreach (var classTable in byClass)
            {
                sb.AppendLine($"Класс:;{EscapeCsv(classTable.ClassName)}");
                sb.AppendLine($"Период:;{EscapeCsv(classTable.PeriodText)}");
                sb.AppendLine();

                var dayHeaders = new List<string> { "№" };
                foreach (var day in classTable.Days)
                {
                    dayHeaders.Add(day);
                    dayHeaders.Add("Ауд.");
                }

                sb.AppendLine(string.Join(';', dayHeaders.Select(EscapeCsv)));

                foreach (var row in classTable.Rows)
                {
                    var line = new List<string> { row.LessonNumber.ToString() };
                    foreach (var cell in row.DayCells)
                    {
                        line.Add(cell.SubjectTeacher);
                        line.Add(cell.Room);
                    }

                    sb.AppendLine(string.Join(';', line.Select(EscapeCsv)));
                }

                sb.AppendLine();
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
        }

        private static void ExportToWordDoc(string filePath, IReadOnlyCollection<LessonExportRow> rows)
        {
            var byClass = BuildClassTables(rows);
            var html = new StringBuilder();
            html.Append("<html><head><meta charset='utf-8'/><style>");
            html.Append("body{font-family:'Times New Roman';font-size:12pt;} ");
            html.Append("h2{margin:0 0 6px 0;} p{margin:0 0 8px 0;} ");
            html.Append("table{border-collapse:collapse;width:100%;margin:10px 0 24px 0;} ");
            html.Append("th,td{border:1px solid #000;padding:4px;vertical-align:top;} ");
            html.Append("th{text-align:center;background:#f0f0f0;} ");
            html.Append(".n{width:35px;text-align:center;} .room{width:55px;text-align:center;}");
            html.Append("</style></head><body>");

            foreach (var classTable in byClass)
            {
                html.Append($"<h2>{EscapeHtml(classTable.ClassName)}</h2>");
                html.Append($"<p><b>{EscapeHtml(classTable.PeriodText)}</b></p>");
                html.Append("<table><thead><tr>");
                html.Append("<th class='n'>№</th>");
                foreach (var day in classTable.Days)
                {
                    html.Append($"<th>{EscapeHtml(day)}</th><th class='room'>Ауд.</th>");
                }

                html.Append("</tr></thead><tbody>");

                foreach (var row in classTable.Rows)
                {
                    html.Append($"<tr><td class='n'>{row.LessonNumber}</td>");
                    foreach (var cell in row.DayCells)
                    {
                        html.Append($"<td>{EscapeHtml(cell.SubjectTeacher)}</td>");
                        html.Append($"<td class='room'>{EscapeHtml(cell.Room)}</td>");
                    }

                    html.Append("</tr>");
                }

                html.Append("</tbody></table>");
            }

            html.Append("</body></html>");
            File.WriteAllText(filePath, html.ToString(), new UTF8Encoding(true));
        }

        private static List<ClassExportTable> BuildClassTables(IReadOnlyCollection<LessonExportRow> rows)
        {
            var minDay = rows.Min(x => x.Day);
            var maxDay = rows.Max(x => x.Day);
            var dayRange = Enumerable.Range(Math.Max(1, minDay), Math.Max(1, maxDay - Math.Max(1, minDay) + 1)).ToList();
            var periodStart = DateTime.Today;
            var periodEnd = periodStart.AddDays(4);

            return rows
                .GroupBy(x => x.ClassName)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var maxLesson = Math.Max(1, g.Max(x => x.LessonNumber));
                    var rowsByKey = g.ToDictionary(x => $"{x.Day}-{x.LessonNumber}", x => x);
                    var tableRows = new List<ClassExportRow>();

                    for (var lesson = 1; lesson <= maxLesson; lesson++)
                    {
                        var dayCells = new List<ClassExportCell>();
                        foreach (var day in dayRange)
                        {
                            var key = $"{day}-{lesson}";
                            if (rowsByKey.TryGetValue(key, out var lessonRow))
                            {
                                dayCells.Add(new ClassExportCell
                                {
                                    SubjectTeacher = $"{lessonRow.Subject}\n{lessonRow.Teacher}",
                                    Room = lessonRow.Room
                                });
                            }
                            else
                            {
                                dayCells.Add(new ClassExportCell { SubjectTeacher = string.Empty, Room = string.Empty });
                            }
                        }

                        tableRows.Add(new ClassExportRow
                        {
                            LessonNumber = lesson,
                            DayCells = dayCells
                        });
                    }

                    return new ClassExportTable
                    {
                        ClassName = g.Key,
                        Days = dayRange.Select(DayToText).ToList(),
                        PeriodText = $"{periodStart:dd.MM.yyyy} - {periodEnd:dd.MM.yyyy}",
                        Rows = tableRows
                    };
                })
                .ToList();
        }

        private static string EscapeCsv(string value)
        {
            var escaped = value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
            return $"\"{escaped}\"";
        }

        private static string EscapeHtml(string value)
        {
            var escaped = System.Security.SecurityElement.Escape(value) ?? string.Empty;
            return escaped.Replace("\n", "<br/>");
        }

        private static string DayToText(int day)
        {
            return day switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                6 => "Суббота",
                _ => "День"
            };
        }

        private static void EnsureCanOverwrite(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }

        private sealed class LessonExportRow
        {
            public int Day { get; set; }
            public int LessonNumber { get; set; }
            public string ClassName { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Teacher { get; set; } = string.Empty;
            public string Room { get; set; } = string.Empty;
        }

        private sealed class ClassExportTable
        {
            public string ClassName { get; set; } = string.Empty;
            public List<string> Days { get; set; } = [];
            public string PeriodText { get; set; } = string.Empty;
            public List<ClassExportRow> Rows { get; set; } = [];
        }

        private sealed class ClassExportRow
        {
            public int LessonNumber { get; set; }
            public List<ClassExportCell> DayCells { get; set; } = [];
        }

        private sealed class ClassExportCell
        {
            public string SubjectTeacher { get; set; } = string.Empty;
            public string Room { get; set; } = string.Empty;
        }
    }
}
