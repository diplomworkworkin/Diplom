using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SchoolScheduleApp.Views.Windows
{
    public partial class TeacherEditWindow : Window
    {
        public ObservableCollection<Subject> Subjects { get; set; } = new();
        public Teacher Teacher { get; private set; }

        public TeacherEditWindow(Teacher teacher)
        {
            InitializeComponent();

            Teacher = teacher ?? new Teacher();

            using (var db = new SchoolDbContext())
            {
                Subjects = new ObservableCollection<Subject>(
                    db.Subjects.OrderBy(s => s.Name).ToList()
                );
            }

            // Важно: DataContext = окно, чтобы XAML видел и Teacher, и Subjects
            DataContext = this;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Teacher.FullName))
            {
                MessageBox.Show("Введите ФИО!", "Ошибка");
                return;
            }

            if (Teacher.SubjectId == null)
            {
                MessageBox.Show("Выберите предмет для учителя.", "Ошибка");
                return;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void BtnAddSubject_Click(object sender, RoutedEventArgs e)
        {
            var name = TbNewSubject.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название предмета.", "Ошибка");
                return;
            }

            using var db = new SchoolDbContext();

            // Проверяем, чтобы не дублировать
            bool exists = db.Subjects.Any(s => s.Name.ToLower() == name.ToLower());
            if (exists)
            {
                MessageBox.Show("Такой предмет уже есть.", "Информация");
                return;
            }

            var newSubject = new Subject { Name = name };
            db.Subjects.Add(newSubject);
            db.SaveChanges();

            // Обновляем список в ComboBox и выбираем добавленный предмет
            Subjects = new ObservableCollection<Subject>(db.Subjects.OrderBy(s => s.Name).ToList());
            Teacher.SubjectId = newSubject.Id;
            TbNewSubject.Clear();

            // Обновляем биндинг
            DataContext = null;
            DataContext = this;
        }
    }
}
