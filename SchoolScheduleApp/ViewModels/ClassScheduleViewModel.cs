using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace SchoolScheduleApp.ViewModels
{
    public class ClassScheduleRow
    {
        public string Day { get; set; } = "";
        public string TimeRange { get; set; } = "";
        public int LessonIndex { get; set; }
        public string Subject { get; set; } = "";
        public string Teacher { get; set; } = "";
        public string Classroom { get; set; } = "";
    }

    public class ClassScheduleViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        public ObservableCollection<FilterOption> DayOptions { get; } = new();
        public ObservableCollection<FilterOption> ClassOptions { get; } = new();
        public ObservableCollection<ClassScheduleRow> ScheduleRows { get; } = new();

        private FilterOption? _selectedDay;
        public FilterOption? SelectedDay
        {
            get => _selectedDay;
            set { _selectedDay = value; OnPropertyChanged(); _ = LoadSchedule(); }
        }

        private FilterOption? _selectedClass;
        public FilterOption? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); _ = LoadSchedule(); }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        private string _weekRangeText = string.Empty;
        public string WeekRangeText
        {
            get => _weekRangeText;
            set { _weekRangeText = value; OnPropertyChanged(); }
        }

        public ClassScheduleViewModel()
        {
            _apiClient = new ApiClient();
            UpdateWeekRange();
            BuildDefaultFilters();
            _ = LoadOptions();
            _ = LoadSchedule();
        }

        private void UpdateWeekRange()
        {
            var today = System.DateTime.Today;
            var diff = (7 + (today.DayOfWeek - System.DayOfWeek.Monday)) % 7;
            var monday = today.AddDays(-diff);
            var friday = monday.AddDays(4);
            WeekRangeText = $"Неделя: {monday:dd.MM.yyyy} – {friday:dd.MM.yyyy}";
        }

        private void BuildDefaultFilters()
        {
            DayOptions.Add(new FilterOption { Id = 0, Name = "Все дни" });
            DayOptions.Add(new FilterOption { Id = 1, Name = "Понедельник" });
            DayOptions.Add(new FilterOption { Id = 2, Name = "Вторник" });
            DayOptions.Add(new FilterOption { Id = 3, Name = "Среда" });
            DayOptions.Add(new FilterOption { Id = 4, Name = "Четверг" });
            DayOptions.Add(new FilterOption { Id = 5, Name = "Пятница" });

            SelectedDay = DayOptions.FirstOrDefault();
        }

        private async Task LoadOptions()
        {
            try
            {
                var classes = await _apiClient.GetAcademicClassesAsync();

                ClassOptions.Clear();
                ClassOptions.Add(new FilterOption { Id = 0, Name = "Все классы" });
                foreach (var cls in classes.OrderBy(x => x.Name))
                    ClassOptions.Add(new FilterOption { Id = cls.Id, Name = cls.Name });

                var user = UserSession.CurrentUser;
                if (user?.Role == UserRole.Student && user.AcademicClassId.HasValue)
                {
                    SelectedClass = ClassOptions.FirstOrDefault(x => x.Id == user.AcademicClassId.Value)
                        ?? ClassOptions.FirstOrDefault();
                }
                else
                {
                    SelectedClass = ClassOptions.FirstOrDefault();
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки классов из API.", ex);
                ErrorMessage = "Ошибка загрузки классов. Проверьте подключение к API.";
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки опций для расписания классов.", ex);
                ErrorMessage = "Произошла ошибка при загрузке опций.";
            }
        }

        private async Task LoadSchedule()
        {
            UpdateWeekRange();
            ScheduleRows.Clear();

            if (SelectedClass == null || SelectedClass.Id == 0)
            {
                ErrorMessage = "Выберите класс.";
                return;
            }

            ErrorMessage = "";

            try
            {
                var lessons = await _apiClient.GetLessonsAsync(classId: SelectedClass.Id, teacherId: null);

                foreach (var l in lessons.OrderBy(x => x.DayOfWeek).ThenBy(x => x.LessonIndex))
                {
                    if (SelectedDay?.Id == 0 || l.DayOfWeek == SelectedDay?.Id)
                    {
                        ScheduleRows.Add(new ClassScheduleRow
                        {
                            Day = SchedulePresentationHelper.DayToText(l.DayOfWeek),
                            TimeRange = SchedulePresentationHelper.LessonIndexToTimeRange(l.LessonIndex),
                            LessonIndex = l.LessonIndex,
                            Subject = l.Subject?.Name ?? "",
                            Teacher = l.Teacher?.FullName ?? "",
                            Classroom = l.Classroom?.Number ?? "—"
                        });
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки расписания классов из API.", ex);
                ErrorMessage = "Ошибка загрузки расписания. Проверьте подключение к API.";
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки расписания классов.", ex);
                ErrorMessage = "Произошла ошибка при загрузке расписания.";
            }
        }
    }
}
