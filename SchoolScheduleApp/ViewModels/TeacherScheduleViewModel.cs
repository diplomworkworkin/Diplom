using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;

namespace SchoolScheduleApp.ViewModels
{
    public class FilterOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class TeacherScheduleRow
    {
        public string Day { get; set; } = "";
        public string TimeRange { get; set; } = "";
        public int LessonIndex { get; set; }
        public string AcademicClass { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Classroom { get; set; } = "";
        public string Type { get; set; } = "—";
    }

    public class TeacherScheduleViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        public ObservableCollection<FilterOption> DayOptions { get; } = new();
        public ObservableCollection<FilterOption> WeekOptions { get; } = new();
        public ObservableCollection<FilterOption> ClassOptions { get; } = new();
        public ObservableCollection<FilterOption> SubjectOptions { get; } = new();
        public ObservableCollection<TeacherScheduleRow> ScheduleRows { get; } = new();

        private FilterOption? _selectedDay;
        public FilterOption? SelectedDay
        {
            get => _selectedDay;
            set { _selectedDay = value; OnPropertyChanged(); _ = LoadSchedule(); }
        }

        private FilterOption? _selectedWeek;
        public FilterOption? SelectedWeek
        {
            get => _selectedWeek;
            set { _selectedWeek = value; OnPropertyChanged(); }
        }

        private FilterOption? _selectedClass;
        public FilterOption? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); _ = LoadSchedule(); }
        }

        private FilterOption? _selectedSubject;
        public FilterOption? SelectedSubject
        {
            get => _selectedSubject;
            set { _selectedSubject = value; OnPropertyChanged(); _ = LoadSchedule(); }
        }

        private string _weekRangeText = "";
        public string WeekRangeText
        {
            get => _weekRangeText;
            set { _weekRangeText = value; OnPropertyChanged(); }
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public RelayCommand ResetFiltersCommand { get; }

        public TeacherScheduleViewModel()
        {
            _apiClient = new ApiClient();
            ResetFiltersCommand = new RelayCommand(async (param) => await ResetFilters());
            BuildDefaultFilters();
            _ = LoadOptions();
            _ = LoadSchedule();
        }

        private void BuildDefaultFilters()
        {
            DayOptions.Add(new FilterOption { Id = 0, Name = "Все дни" });
            DayOptions.Add(new FilterOption { Id = 1, Name = "Понедельник" });
            DayOptions.Add(new FilterOption { Id = 2, Name = "Вторник" });
            DayOptions.Add(new FilterOption { Id = 3, Name = "Среда" });
            DayOptions.Add(new FilterOption { Id = 4, Name = "Четверг" });
            DayOptions.Add(new FilterOption { Id = 5, Name = "Пятница" });

            WeekOptions.Add(new FilterOption { Id = 0, Name = "Вся неделя" });
            WeekOptions.Add(new FilterOption { Id = 1, Name = "Текущая неделя" });

            SelectedDay = DayOptions.FirstOrDefault();
            SelectedWeek = WeekOptions.FirstOrDefault();
        }

        private async Task LoadOptions()
        {
            var user = UserSession.CurrentUser;
            if (user == null || user.Role != UserRole.Teacher || user.TeacherId == null)
            {
                ErrorMessage = "Нет привязки учителя к учетной записи.";
                return;
            }

            try
            {
                var lessons = await _apiClient.GetLessonsAsync(teacherId: user.TeacherId.Value);

                var classItems = lessons
                    .Where(x => x.AcademicClass != null)
                    .Select(x => x.AcademicClass)
                    .DistinctBy(x => x.Id)
                    .OrderBy(x => x.Name)
                    .ToList();

                ClassOptions.Clear();
                ClassOptions.Add(new FilterOption { Id = 0, Name = "Все классы" });
                foreach (var cls in classItems)
                    ClassOptions.Add(new FilterOption { Id = cls.Id, Name = cls.Name });

                SubjectOptions.Clear();
                SubjectOptions.Add(new FilterOption { Id = 0, Name = "Все предметы" });
                var subjects = lessons
                    .Where(x => x.Subject != null)
                    .Select(x => x.Subject)
                    .DistinctBy(x => x.Id)
                    .OrderBy(x => x.Name)
                    .ToList();
                foreach (var subj in subjects)
                    SubjectOptions.Add(new FilterOption { Id = subj.Id, Name = subj.Name });

                SelectedClass = ClassOptions.FirstOrDefault();
                SelectedSubject = SubjectOptions.FirstOrDefault();

                WeekRangeText = GetCurrentWeekRange();
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки опций для расписания учителя из API.", ex);
                ErrorMessage = "Ошибка загрузки опций. Проверьте подключение к API.";
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки опций для расписания учителя.", ex);
                ErrorMessage = "Произошла ошибка при загрузке опций.";
            }
        }

        private async Task LoadSchedule()
        {
            WeekRangeText = GetCurrentWeekRange();
            ScheduleRows.Clear();

            var user = UserSession.CurrentUser;
            if (user == null || user.Role != UserRole.Teacher || user.TeacherId == null)
            {
                ErrorMessage = "Роль учителя не подтверждена.";
                return;
            }

            ErrorMessage = "";

            try
            {
                var lessons = await _apiClient.GetLessonsAsync(
                    teacherId: user.TeacherId.Value,
                    dayOfWeek: SelectedDay?.Id == 0 ? null : SelectedDay?.Id,
                    classId: SelectedClass?.Id == 0 ? null : SelectedClass?.Id,
                    subjectId: SelectedSubject?.Id == 0 ? null : SelectedSubject?.Id);

                foreach (var l in lessons.OrderBy(x => x.DayOfWeek).ThenBy(x => x.LessonIndex))
                {
                    ScheduleRows.Add(new TeacherScheduleRow
                    {
                        Day = SchedulePresentationHelper.DayToText(l.DayOfWeek),
                        TimeRange = SchedulePresentationHelper.LessonIndexToTimeRange(l.LessonIndex),
                        LessonIndex = l.LessonIndex,
                        AcademicClass = l.AcademicClass?.Name ?? "",
                        Subject = l.Subject?.Name ?? "",
                        Classroom = l.Classroom?.Number ?? "—",
                        Type = string.IsNullOrWhiteSpace(l.Classroom?.Type) ? "—" : l.Classroom!.Type!
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки расписания учителя из API.", ex);
                ErrorMessage = "Ошибка загрузки расписания. Проверьте подключение к API.";
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки расписания учителя.", ex);
                ErrorMessage = "Произошла ошибка при загрузке расписания.";
            }
        }

        private async Task ResetFilters()
        {
            SelectedDay = DayOptions.FirstOrDefault();
            SelectedWeek = WeekOptions.FirstOrDefault();
            SelectedClass = ClassOptions.FirstOrDefault();
            SelectedSubject = SubjectOptions.FirstOrDefault();
            await LoadSchedule();
        }

        private static string GetCurrentWeekRange()
        {
            var today = DateTime.Today;
            var diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var monday = today.AddDays(-diff);
            var friday = monday.AddDays(4);
            return $"{monday:dd.MM.yyyy} – {friday:dd.MM.yyyy}";
        }
    }
}
