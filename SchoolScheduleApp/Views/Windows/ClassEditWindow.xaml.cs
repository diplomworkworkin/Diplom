using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;

namespace SchoolScheduleApp.Views.Windows
{
    /// <summary>
    /// Логика взаимодействия для ClassEditWindow.xaml
    /// </summary>
    public partial class ClassEditWindow : Window
    {
        private readonly ApiClient _apiClient;
        public AcademicClass AcademicClass { get; private set; }
        public ObservableCollection<Teacher> Teachers { get; private set; } = new();

        public ClassEditWindow(AcademicClass? cls)
        {
            InitializeComponent();
            _apiClient = new ApiClient();

            AcademicClass = cls ?? new AcademicClass { Shift = 1, StudentCount = 1 };

            _ = LoadTeachersForCurator();
            DataContext = this;
        }

        private async Task LoadTeachersForCurator()
        {
            try
            {
                var allTeachers = await _apiClient.GetTeachersAsync();
                // TODO: API должен предоставлять информацию о том, кто является куратором
                // Пока что, просто загружаем всех учителей.
                Teachers = new ObservableCollection<Teacher>(allTeachers.OrderBy(t => t.FullName));
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки учителей для куратора из API.", ex);
                ToastService.Show("Ошибка загрузки учителей. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки учителей для куратора.", ex);
                ToastService.Show("Произошла ошибка при загрузке учителей.", "Ошибка");
            }
        }

        private void BtnClearCurator_Click(object sender, RoutedEventArgs e)
        {
            AcademicClass.CuratorTeacherId = null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1) Приводим название к нормальному виду
            AcademicClass.Name = (AcademicClass.Name ?? "")
                .Trim()
                .ToUpper()
                .Replace("–", "-"); // если кто-то вставил длинное тире

            // 2) Проверка названия: 1..11 + "-" + русская буква
            // Примеры: 1-А, 5-Б, 11-В
            string pattern = @"^(?:[1-9]|1[0-1])-[А-ЯЁ]$";
            if (!Regex.IsMatch(AcademicClass.Name, pattern))
            {
                ToastService.Show("Название должно быть в формате: 1-А, 2-Б, ... 11-В (цифра + дефис + русская буква).", "Ошибка", true);
                return;
            }

            // 3) Кол-во учеников: 1..30
            if (AcademicClass.StudentCount < 1 || AcademicClass.StudentCount > 30)
            {
                ToastService.Show("Количество учеников должно быть от 1 до 30.", "Ошибка", true);
                return;
            }

            // 4) Смена: 1 или 2
            if (AcademicClass.Shift != 1 && AcademicClass.Shift != 2)
            {
                ToastService.Show("Смена должна быть 1 или 2.", "Ошибка", true);
                return;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        private void TbStudentCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Только цифры
            if (!Regex.IsMatch(e.Text, @"^\d+$"))
            {
                e.Handled = true;
                return;
            }

            // Не даем ввести > 30
            var tb = (TextBox)sender;
            string newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                    .Insert(tb.SelectionStart, e.Text);

            if (int.TryParse(newText, out int value))
                e.Handled = value > 30;
        }

        private void TbStudentCount_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string text = e.DataObject.GetData(DataFormats.Text)?.ToString() ?? "";
            if (!Regex.IsMatch(text, @"^\d+$"))
            {
                e.CancelCommand();
                return;
            }

            if (int.TryParse(text, out int value) && value > 30)
                e.CancelCommand();
        }
    }
}
