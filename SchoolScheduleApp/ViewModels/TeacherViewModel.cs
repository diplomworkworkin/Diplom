using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Net.Http;

namespace SchoolScheduleApp.ViewModels
{
    public class TeacherRow
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string StatusText => IsActive ? "Активен" : "Неактивен";
    }

    public class TeachersViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        private ObservableCollection<TeacherRow> _teachersList = new();
        public ObservableCollection<TeacherRow> TeachersList
        {
            get => _teachersList;
            set { _teachersList = value; OnPropertyChanged(); }
        }

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }

        public TeachersViewModel()
        {
            _apiClient = new ApiClient();
            AddCommand = new RelayCommand(async (param) => await ExecuteAdd());
            EditCommand = new RelayCommand(async (param) => await ExecuteEdit(param as TeacherRow));
            DeleteCommand = new RelayCommand(async (param) => await ExecuteDelete(param as TeacherRow));

            _ = LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                var teachers = await _apiClient.GetTeachersAsync();
                var subjects = await _apiClient.GetSubjectsAsync(); // Получаем предметы для отображения

                // TODO: API должен предоставлять информацию об активности учителя (наличие нагрузки/уроков)
                // Пока что, все учителя считаются активными.
                var teacherRows = teachers
                    .OrderBy(t => t.Id)
                    .Select(t => new TeacherRow
                    {
                        Id = t.Id,
                        FullName = t.FullName,
                        SubjectName = subjects.FirstOrDefault(s => s.Id == t.SubjectId)?.Name ?? "-",
                        IsActive = true // Временно считаем всех активными, пока нет API для проверки нагрузки
                    })
                    .ToList();

                TeachersList = new ObservableCollection<TeacherRow>(teacherRows);
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки учителей из API.", ex);
                ToastService.Show("Ошибка загрузки учителей. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки данных учителей.", ex);
                ToastService.Show("Произошла ошибка при загрузке данных учителей.", "Ошибка");
            }
        }

        private async Task ExecuteAdd()
        {
            var wnd = new TeacherEditWindow(new Teacher())
            {
                Owner = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w is not TeacherEditWindow)
                    ?? Application.Current?.MainWindow
            };

            if (wnd.ShowDialog() == true)
            {
                try
                {
                    var newTeacher = wnd.Teacher;
                    var addedTeacher = await _apiClient.AddTeacherAsync(newTeacher);

                    // Создаем учетную запись для учителя через API
                    var user = new UserCreate
                    {
                        Username = $"teacher{addedTeacher.Id}",
                        Password = $"teacher{addedTeacher.Id}",
                        FullName = addedTeacher.FullName,
                        Role = UserRole.Teacher,
                        TeacherId = addedTeacher.Id
                    };
                    await _apiClient.RegisterUserAsync(user);

                    ToastService.Show($"Учитель \'{addedTeacher.FullName}\' успешно добавлен.", "Успех");
                    await LoadData();
                }
                catch (HttpRequestException ex)
                {
                    AppLogger.LogError("Ошибка добавления учителя или создания пользователя через API.", ex);
                    ToastService.Show($"Ошибка добавления учителя: {ex.Message}", "Ошибка", true);
                }
                catch (System.Exception ex)
                {
                    AppLogger.LogError("Ошибка добавления учителя.", ex);
                    ToastService.Show("Произошла ошибка при добавлении учителя.", "Ошибка");
                }
            }
        }

        private async Task ExecuteEdit(TeacherRow? teacherRow)
        {
            if (teacherRow == null) return;

            try
            {
                var fromApi = await _apiClient.GetTeacherByIdAsync(teacherRow.Id);
                if (fromApi == null) return;

                var editable = new Teacher
                {
                    Id = fromApi.Id,
                    FullName = fromApi.FullName,
                    SubjectId = fromApi.SubjectId,
                    ClassroomId = fromApi.ClassroomId
                };

                var wnd = new TeacherEditWindow(editable)
                {
                    Owner = Application.Current?.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.IsActive && w is not TeacherEditWindow)
                        ?? Application.Current?.MainWindow
                };
                if (wnd.ShowDialog() != true) return;

                var updatedTeacher = wnd.Teacher;
                await _apiClient.UpdateTeacherAsync(updatedTeacher.Id, updatedTeacher);

                // Обновляем имя пользователя в учетной записи через API
                // TODO: API должен предоставлять эндпоинт для обновления пользователя по TeacherId
                // Пока что, это не реализовано в API, поэтому пропускаем.
                // var user = await _apiClient.GetUserByTeacherIdAsync(updatedTeacher.Id);
                // if (user != null)
                // {
                //     user.FullName = updatedTeacher.FullName;
                //     await _apiClient.UpdateUserAsync(user.Id, user);
                // }

                ToastService.Show($"Учитель \'{updatedTeacher.FullName}\' успешно обновлен.", "Успех");
                await LoadData();
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка обновления учителя через API.", ex);
                ToastService.Show($"Ошибка обновления учителя: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка обновления учителя.", ex);
                ToastService.Show("Произошла ошибка при обновлении учителя.", "Ошибка");
            }
        }

        private async Task ExecuteDelete(TeacherRow? teacherRow)
        {
            if (teacherRow == null) return;

            var result = MessageBox.Show(
                $"Удалить учителя \"{teacherRow.FullName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // Удаляем связанные учетные записи пользователей через API
                // TODO: API должен предоставлять эндпоинт для удаления пользователя по TeacherId
                // Пока что, это не реализовано в API, поэтому пропускаем.
                // await _apiClient.DeleteUserByTeacherIdAsync(teacherRow.Id);

                await _apiClient.DeleteTeacherAsync(teacherRow.Id);
                ToastService.Show($"Учитель \'{teacherRow.FullName}\' успешно удален.", "Успех");
                await LoadData();
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка удаления учителя через API.", ex);
                ToastService.Show($"Ошибка удаления учителя: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка удаления учителя.", ex);
                ToastService.Show("Произошла ошибка при удалении учителя.", "Ошибка");
            }
        }
    }
}
