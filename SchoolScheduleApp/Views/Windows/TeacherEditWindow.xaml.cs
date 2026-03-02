using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Net.Http;

namespace SchoolScheduleApp.Views.Windows
{
    public partial class TeacherEditWindow : Window
    {
        private readonly ApiClient _apiClient;
        public ObservableCollection<Subject> Subjects { get; private set; } = new();
        public ObservableCollection<ClassroomOption> Classrooms { get; private set; } = new();
        public Teacher Teacher { get; }

        public TeacherEditWindow(Teacher teacher)
        {
            InitializeComponent();
            _apiClient = new ApiClient();

            Teacher = teacher ?? new Teacher();
            _ = LoadSubjects();
            _ = LoadClassrooms();

            DataContext = this;
        }

        private async Task LoadSubjects()
        {
            try
            {
                Subjects = new ObservableCollection<Subject>(
                    (await _apiClient.GetSubjectsAsync()).OrderBy(s => s.Name).ToList()
                );
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки предметов из API.", ex);
                ToastService.Show("Ошибка загрузки предметов. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки предметов.", ex);
                ToastService.Show("Произошла ошибка при загрузке предметов.", "Ошибка");
            }
        }

        private void RefreshBinding()
        {
            // DataContext = null;
            // DataContext = this;
            // No longer needed with async loading and OnPropertyChanged in ViewModels
        }

        private async Task LoadClassrooms()
        {
            try
            {
                var rooms = (await _apiClient.GetClassroomsAsync())
                    .OrderBy(c => c.Number)
                    .Select(c => new ClassroomOption
                    {
                        Id = c.Id,
                        DisplayName = string.IsNullOrWhiteSpace(c.Type)
                            ? c.Number
                            : $"{c.Number} ({c.Type})"
                    })
                    .ToList();

                Classrooms = new ObservableCollection<ClassroomOption>(rooms);
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки кабинетов из API.", ex);
                ToastService.Show("Ошибка загрузки кабинетов. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки кабинетов.", ex);
                ToastService.Show("Произошла ошибка при загрузке кабинетов.", "Ошибка");
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Teacher.FullName))
            {
                ToastService.Show("Введите ФИО преподавателя.", "Проверка", true);
                return;
            }

            if (Teacher.SubjectId == null)
            {
                ToastService.Show("Выберите предмет для учителя.", "Проверка", true);
                return;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private async void BtnAddSubject_Click(object sender, RoutedEventArgs e)
        {
            var name = (TbNewSubject.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ToastService.Show("Введите название предмета.", "Проверка", true);
                return;
            }

            try
            {
                var existingSubjects = await _apiClient.GetSubjectsAsync();
                if (existingSubjects.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    ToastService.Show("Такой предмет уже существует.", "Информация");
                    return;
                }

                var subject = new SubjectCreate { Name = name };
                var addedSubject = await _apiClient.AddSubjectAsync(subject);

                await LoadSubjects();
                Teacher.SubjectId = addedSubject.Id;
                TbNewSubject.Clear();
                // RefreshBinding(); // No longer needed

                ToastService.Show("Предмет успешно добавлен.");
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка добавления предмета через API.", ex);
                ToastService.Show($"Ошибка добавления предмета: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка добавления предмета.", ex);
                ToastService.Show("Произошла ошибка при добавлении предмета.", "Ошибка");
            }
        }

        private async void BtnAddClassroom_Click(object sender, RoutedEventArgs e)
        {
            var number = (TbNewRoomNumber.Text ?? string.Empty).Trim();
            var type = (TbNewRoomType.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(number))
            {
                ToastService.Show("Введите номер/название кабинета.", "Проверка", true);
                return;
            }

            if (!int.TryParse(TbNewRoomCapacity.Text, out var capacity) || capacity <= 0)
            {
                ToastService.Show("Вместимость должна быть положительным числом.", "Проверка", true);
                return;
            }

            try
            {
                var existingClassrooms = await _apiClient.GetClassroomsAsync();
                if (existingClassrooms.Any(c => c.Number.Equals(number, StringComparison.OrdinalIgnoreCase)))
                {
                    ToastService.Show("Такой кабинет уже существует.", "Информация");
                    return;
                }

                var classroom = new ClassroomCreate
                {
                    Number = number,
                    Type = string.IsNullOrWhiteSpace(type) ? null : type,
                    Capacity = capacity
                };

                var addedClassroom = await _apiClient.AddClassroomAsync(classroom);

                Teacher.ClassroomId = addedClassroom.Id;
                await LoadClassrooms();
                // RefreshBinding(); // No longer needed

                TbNewRoomNumber.Clear();
                TbNewRoomType.Clear();
                TbNewRoomCapacity.Clear();

                ToastService.Show("Кабинет добавлен.");
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка добавления кабинета через API.", ex);
                ToastService.Show($"Ошибка добавления кабинета: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка добавления кабинета.", ex);
                ToastService.Show("Произошла ошибка при добавлении кабинета.", "Ошибка");
            }
        }

        public sealed class ClassroomOption
        {
            public int Id { get; set; }
            public string DisplayName { get; set; } = string.Empty;
        }
    }
}
