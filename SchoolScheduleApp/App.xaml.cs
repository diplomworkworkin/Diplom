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

            // 1) Применяем тему из settings.json
            var settings = AppSettingsService.Load();
            ThemeManager.SetTheme(settings.IsDarkTheme);
            
            AppLogger.LogInfo("Приложение запущено.");
        }
    }
}
