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

        private ObservableCollection<AcademicClass> _classes = new();
        public ObservableCollection<AcademicClass> Classes
        {
            get => _classes;
            set { _classes = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LessonSlot> _dayGrid = new();
        public ObservableCollection<LessonSlot> DayGrid
        {
            get => _dayGrid;
            set { _dayGrid = value; OnPropertyChanged(); }
        }

        private ObservableCollection<LessonRow> _scheduleTable = new();
        public ObservableCollection<LessonRow> ScheduleTable
        {
            get => _scheduleTable;
            set { _scheduleTable = value; OnPropertyChanged(); }
        }

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
                _selectedDayTabIndex = _selectedDay - 1;
                OnPropertyChanged(nameof(SelectedDayTabIndex));
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
                _selectedDay = _selectedDayTabIndex + 1;
                OnPropertyChanged(nameof(SelectedDay));
                _ = RefreshData();
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
                Classes = new ObservableCollection<AcademicClass>(classes.OrderBy(c => c.Name));
                if (Classes.Count > 0 && SelectedClassId <= 0)
                {
                    SelectedClassId = Classes[0].Id;
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки классов.", ex);
            }
        }

        private async Task ExecuteAutoGenerate()
        {
            if (MessageBox.Show("Автоматически составить расписание?", "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                await ScheduleGenerator.GenerateAsync();
                await RefreshData();
                ToastService.Show("Генерация завершена.", "Результат");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка генерации расписания.", ex);
                ToastService.Show("Ошибка при генерации.", "Ошибка");
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
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки расписания.", ex);
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

                for (int idx = start; idx <= end; idx++)
                {
                    var lesson = filteredLessons.FirstOrDefault(x => x.LessonIndex == idx);
                    DayGrid.Add(new LessonSlot
                    {
                        DisplayIndex = idx - start + 1,
                        RealLessonIndex = idx,
                        HasLesson = lesson != null,
                        Subject = lesson?.Subject?.Name ?? "Нет урока",
                        Teacher = lesson?.Teacher?.FullName ?? string.Empty,
                        Classroom = lesson?.Classroom?.Number ?? "-"
                    });
                }
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки сетки дня.", ex);
            }
        }
    }
}
