using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Net.Http;

namespace SchoolScheduleApp.ViewModels
{
    public class LessonRow
    {
        public string Day { get; set; } = string.Empty;
        public int DayOfWeek { get; set; }
        public int LessonIndex { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Teacher { get; set; } = string.Empty;
        public string Classroom { get; set; } = string.Empty;
    }

    public class LessonSlot
    {
        public int DisplayIndex { get; set; }
        public int RealLessonIndex { get; set; }
        public bool HasLesson { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Teacher { get; set; } = string.Empty;
        public string Classroom { get; set; } = string.Empty;
    }

    public class ScheduleViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        public RelayCommand AutoGenerateScheduleCommand { get; }

        public ObservableCollection<AcademicClass> Classes { get; set; } = new();
        public ObservableCollection<LessonSlot> DayGrid { get; set; } = new();
        public ObservableCollection<LessonRow> ScheduleTable { get; set; } = new();

        private string _weekRangeText = string.Empty;
        public string WeekRangeText
        {
            get => _weekRangeText;
            set { _weekRangeText = value; OnPropertyChanged(); }
        }

        private int _selectedDay = 1;
        public int SelectedDay
        {
            get => _selectedDay;
            set
            {
                if (_selectedDay == value) return;

                _selectedDay = value;
                OnPropertyChanged();

                var idx = _selectedDay - 1;
                if (idx < 0) idx = 0;
                if (idx > 4) idx = 4;

                if (_selectedDayTabIndex != idx)
                {
                    _selectedDayTabIndex = idx;
                    OnPropertyChanged(nameof(SelectedDayTabIndex));
                }

                _ = RefreshData();
            }
        }

        private int _selectedDayTabIndex;
        public int SelectedDayTabIndex
        {
            get => _selectedDayTabIndex;
            set
            {
                if (_selectedDayTabIndex == value) return;

                _selectedDayTabIndex = value;
                OnPropertyChanged();

                var day = _selectedDayTabIndex + 1;
                if (day < 1) day = 1;
                if (day > 5) day = 5;

                if (_selectedDay != day)
                {
                    _selectedDay = day;
                    OnPropertyChanged(nameof(SelectedDay));
                    _ = RefreshData();
                }
            }
        }

        private int _selectedClassId;
        public int SelectedClassId
        {
            get => _selectedClassId;
            set
            {
                _selectedClassId = value;
                OnPropertyChanged();
                _ = RefreshData();
            }
        }

        public ScheduleViewModel()
        {
            _apiClient = new ApiClient();
            AutoGenerateScheduleCommand = new RelayCommand(async (param) => await ExecuteAutoGenerate());
            UpdateWeekRange();
            _ = LoadClasses();
        }

        public async Task RefreshData()
        {
            UpdateWeekRange();
            await LoadSchedule();
            await LoadDayGrid();
        }

        private void UpdateWeekRange()
        {
            var today = System.DateTime.Today;
            var diff = (7 + (today.DayOfWeek - System.DayOfWeek.Monday)) % 7;
            var monday = today.AddDays(-diff);
            var friday = monday.AddDays(4);
            WeekRangeText = $"Неделя: {monday:dd.MM.yyyy} – {friday:dd.MM.yyyy}";
        }

        private async Task LoadClasses()
        {
            try
            {
                var classes = await _apiClient.GetAcademicClassesAsync();
                Classes = new ObservableCollection<AcademicClass>(
                    classes.OrderBy(c => c.Name).ToList()
                );
                OnPropertyChanged(nameof(Classes));

                if (Classes.Count > 0 && SelectedClassId <= 0)
                {
                    SelectedClassId = Classes[0].Id;
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки классов из API.", ex);
                ToastService.Show("Ошибка загрузки классов. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки классов.", ex);
                ToastService.Show("Произошла ошибка при загрузке классов.", "Ошибка");
            }
        }

        private async Task ExecuteAutoGenerate()
        {
            var confirm = MessageBox.Show(
                "Автоматически составить расписание?\nСтарое расписание будет удалено.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // TODO: Implement API endpoint for schedule generation
                // For now, mock data or skip if no API yet
                // var result = await _apiClient.GenerateScheduleAsync(clearOldSchedule: true);
                // RefreshData();

                // int lessonsForSelectedClass = (await _apiClient.GetLessonsAsync(classId: SelectedClassId)).Count();

                // var message = $"Создано уроков: {result.CreatedLessons}. Для выбранного класса: {lessonsForSelectedClass}.";

                // if (lessonsForSelectedClass == 0)
                // {
                //     message += " Нагрузка для выбранного класса не задана — расписание может быть пустым.";
                // }

                // if (result.Problems.Count > 0)
                // {
                //     message += $" Обнаружено проблем: {result.Problems.Count}.";
                // }
                // else
                // {
                //     message += " Генерация завершена.";
                // }

                // ToastService.Show(message, "Результат");
                ToastService.Show("Функционал автоматической генерации расписания пока не реализован через API.", "Информация");
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка генерации расписания через API.", ex);
                ToastService.Show("Ошибка генерации расписания. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка генерации расписания.", ex);
                ToastService.Show("Произошла ошибка при генерации расписания.", "Ошибка");
            }
        }

        private async Task LoadSchedule()
        {
            ScheduleTable.Clear();

            if (SelectedClassId <= 0) return;

            try
            {
                var lessons = await _apiClient.GetLessonsAsync(classId: SelectedClassId);

                foreach (var lesson in lessons.Where(x => x.DayOfWeek == SelectedDay).OrderBy(x => x.LessonIndex))
                {
                    ScheduleTable.Add(new LessonRow
                    {
                        Day = SchedulePresentationHelper.DayToText(lesson.DayOfWeek),
                        DayOfWeek = lesson.DayOfWeek,
                        LessonIndex = lesson.LessonIndex,
                        TimeRange = SchedulePresentationHelper.LessonIndexToTimeRange(lesson.LessonIndex),
                        Subject = lesson.Subject?.Name ?? string.Empty,
                        Teacher = lesson.Teacher?.FullName ?? string.Empty,
                        Classroom = lesson.Classroom?.Number ?? "-"
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки расписания из API.", ex);
                ToastService.Show("Ошибка загрузки расписания. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки расписания.", ex);
                ToastService.Show("Произошла ошибка при загрузке расписания.", "Ошибка");
            }
        }

        private async Task LoadDayGrid()
        {
            DayGrid.Clear();

            if (SelectedClassId <= 0) return;

            try
            {
                var classes = await _apiClient.GetAcademicClassesAsync();
                var selectedClass = classes.FirstOrDefault(c => c.Id == SelectedClassId);
                int shift = selectedClass?.Shift ?? 1;

                int start = shift == 1 ? 1 : 7;
                int end = shift == 1 ? 6 : 12;

                var lessons = await _apiClient.GetLessonsAsync(classId: SelectedClassId);

                var filteredLessons = lessons.Where(x => x.DayOfWeek == SelectedDay).ToList();

                int displayIndex = 1;
                for (int idx = start; idx <= end; idx++)
                {
                    var lesson = filteredLessons.FirstOrDefault(x => x.LessonIndex == idx);

                    DayGrid.Add(new LessonSlot
                    {
                        DisplayIndex = displayIndex,
                        RealLessonIndex = idx,
                        HasLesson = lesson != null,
                        Subject = lesson?.Subject?.Name ?? "Нет урока",
                        Teacher = lesson?.Teacher?.FullName ?? string.Empty,
                        Classroom = lesson?.Classroom?.Number ?? "-"
                    });

                    displayIndex++;
                }

                OnPropertyChanged(nameof(DayGrid));
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки сетки дня из API.", ex);
                ToastService.Show("Ошибка загрузки сетки дня. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки сетки дня.", ex);
                ToastService.Show("Произошла ошибка при загрузке сетки дня.", "Ошибка");
            }
        }
    }
}
