using SchoolScheduleApp.Data.Entites;

namespace SchoolScheduleApp.Core
{
    public static class UserSession
    {
        public static User CurrentUser { get; private set; }

        public static void SetUser(User user)
        {
            CurrentUser = user;
        }

        public static void ClearSession()
        {
            CurrentUser = null;
        }

        public static bool IsLoggedIn => CurrentUser != null;
        public static bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
        public static bool IsTeacher => CurrentUser?.Role == UserRole.Teacher;
        public static bool IsStudent => CurrentUser?.Role == UserRole.Student;
    }
}
