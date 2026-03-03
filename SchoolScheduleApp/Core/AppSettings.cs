namespace SchoolScheduleApp.Core
{
    public class AppSettings
    {
        public bool IsDarkTheme { get; set; } = true;
        public string SchoolName { get; set; } = "МБОУ СШ №11";
        public int LessonDuration { get; set; } = 45;
        public string StartTime { get; set; } = "08:00";
        public bool RememberMe { get; set; }
        public string SavedUsername { get; set; } = string.Empty;
        public string SavedPassword { get; set; } = string.Empty;

        public int MaxLessonDuration => 120;   
    }
}
