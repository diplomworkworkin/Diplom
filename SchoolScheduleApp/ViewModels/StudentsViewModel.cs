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
    public class StudentsViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        private ObservableCollection<AcademicClass> _classesList = new();
        public ObservableCollection<AcademicClass> ClassesList
        {
            get => _classesList;
            set { _classesList = value; OnPropertyChanged(); }
        }

        private AcademicClass? _selectedClass;
        public AcademicClass? SelectedClass
        {
            get => _selectedClass;
            set { _selectedClass = value; OnPropertyChanged(); }
        }

        public RelayCommand AddClassCommand { get; }
        public RelayCommand EditClassCommand { get; }
        public RelayCommand DeleteClassCommand { get; }
        public RelayCommand RefreshClassesCommand { get; }

        public StudentsViewModel()
        {
            _apiClient = new ApiClient();
            AddClassCommand = new RelayCommand(async (param) => await ExecuteAddClass());
            EditClassCommand = new RelayCommand(async (param) => await ExecuteEditClass(param), CanEditOrDelete);
            DeleteClassCommand = new RelayCommand(async (param) => await ExecuteDeleteClass(param), CanEditOrDelete);
            RefreshClassesCommand = new RelayCommand(async (param) => await LoadData());

            _ = LoadData();
        }

        private bool CanEditOrDelete(object? obj)
        {
            var ac = obj as AcademicClass ?? SelectedClass;
            return ac != null;
        }

        private async Task LoadData()
        {
            try
            {
                var classes = await _apiClient.GetAcademicClassesAsync();
                ClassesList = new ObservableCollection<AcademicClass>(classes.OrderBy(c => c.Id));
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки классов из API.", ex);
                ToastService.Show("Ошибка загрузки классов. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки данных для студентов.", ex);
                ToastService.Show("Произошла ошибка при загрузке данных.", "Ошибка");
            }
        }

        private async Task ExecuteAddClass()
        {
            var wnd = new ClassEditWindow(null)
            {
                Owner = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w is not ClassEditWindow)
                    ?? Application.Current?.MainWindow
            };

            if (wnd.ShowDialog() != true)
                return;

            var newClass = wnd.AcademicClass;

            try
            {
                // 1) Уникальность имени класса (проверяется на сервере)
                // 2) Проверка куратора (проверяется на сервере)
                var addedClass = await _apiClient.AddAcademicClassAsync(newClass);
                ToastService.Show($"Класс '{addedClass.Name}' успешно добавлен.", "Успех");
                await LoadData();
                SelectedClass = ClassesList.FirstOrDefault(x => x.Id == addedClass.Id);
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка добавления класса через API.", ex);
                ToastService.Show($"Ошибка добавления класса: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка добавления класса.", ex);
                ToastService.Show("Произошла ошибка при добавлении класса.", "Ошибка");
            }
        }

        private async Task ExecuteEditClass(object? obj)
        {
            var ac = obj as AcademicClass ?? SelectedClass;
            if (ac == null) return;

            // делаем копию, чтобы "Отмена" не меняла таблицу
            var editable = new AcademicClass
            {
                Id = ac.Id,
                Name = ac.Name,
                StudentCount = ac.StudentCount,
                Shift = ac.Shift,
                CuratorTeacherId = ac.CuratorTeacherId
            };

            var wnd = new ClassEditWindow(editable)
            {
                Owner = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w is not ClassEditWindow)
                    ?? Application.Current?.MainWindow
            };

            if (wnd.ShowDialog() != true)
                return;

            var updated = wnd.AcademicClass;

            try
            {
                // 1) Уникальность имени (кроме самого себя) (проверяется на сервере)
                // 2) Проверка куратора (кроме самого себя) (проверяется на сервере)
                var updatedClass = await _apiClient.UpdateAcademicClassAsync(updated.Id, updated);
                ToastService.Show($"Класс '{updatedClass.Name}' успешно обновлен.", "Успех");
                await LoadData();
                SelectedClass = ClassesList.FirstOrDefault(x => x.Id == updatedClass.Id);
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка обновления класса через API.", ex);
                ToastService.Show($"Ошибка обновления класса: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка обновления класса.", ex);
                ToastService.Show("Произошла ошибка при обновлении класса.", "Ошибка");
            }
        }

        private async Task ExecuteDeleteClass(object? obj)
        {
            var ac = obj as AcademicClass ?? SelectedClass;
            if (ac == null) return;

            var result = MessageBox.Show(
                $"Удалить класс \"{ac.Name}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                // Важное ограничение: нельзя удалить класс, если на него есть нагрузки (проверяется на сервере)
                await _apiClient.DeleteAcademicClassAsync(ac.Id);
                ToastService.Show($"Класс '{ac.Name}' успешно удален.", "Успех");
                await LoadData();
                SelectedClass = null;
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка удаления класса через API.", ex);
                ToastService.Show($"Ошибка удаления класса: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка удаления класса.", ex);
                ToastService.Show("Произошла ошибка при удалении класса.", "Ошибка");
            }
        }
    }
}
