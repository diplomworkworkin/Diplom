using Microsoft.EntityFrameworkCore;
using SchoolScheduleApp.Data.Context;
using SchoolScheduleApp.Core;
using System.Windows;

namespace SchoolScheduleApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1) Применяем тему из settings.json (чтобы переключатель в настройках реально работал)
            var settings = AppSettingsService.Load();
            ThemeManager.SetTheme(settings.IsDarkTheme);

            // При старте применяем миграции, чтобы HasData (Subjects/Teachers/Classes и т.д.) попал в БД.
            using (var db = new SchoolDbContext())
            {
                db.Database.Migrate();
            }
        }
    }

}
