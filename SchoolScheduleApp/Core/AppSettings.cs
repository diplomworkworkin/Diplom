namespace SchoolScheduleApp.Core
{
    // Простая модель настроек (для диплома без усложнений)
    public class AppSettings
    {
        public bool IsDarkTheme { get; set; } = true;
        public string SchoolName { get; set; } = "МБОУ СШ №11";
        public int LessonDuration { get; set; } = 45;
        public string StartTime { get; set; } = "08:00";

        // Граница для проверки (не обязательно, но удобно)
        public int MaxLessonDuration => 120;   
    }
}
