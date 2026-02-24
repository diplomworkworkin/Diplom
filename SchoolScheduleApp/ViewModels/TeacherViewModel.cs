using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

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
            AddCommand = new RelayCommand(_ => ExecuteAdd());
            EditCommand = new RelayCommand(o => ExecuteEdit(o as TeacherRow));
            DeleteCommand = new RelayCommand(o => ExecuteDelete(o as TeacherRow));

            LoadData();
        }

        private void LoadData()
        {
            using var db = new SchoolDbContext();

            // Статус "Активен" только если у учителя есть нагрузка (Workloads) или уроки в расписании (Lessons)
            var activeTeacherIds = db.Workloads
                .Select(w => w.TeacherId)
                .Distinct()
                .ToHashSet();

            var lessonTeacherIds = db.Lessons
                .Select(l => l.TeacherId)
                .Distinct()
                .ToList();

            foreach (var id in lessonTeacherIds)
            {
                activeTeacherIds.Add(id);
            }

            var teacherRows = db.Teachers
                .Include(t => t.Subject)
                .OrderBy(t => t.Id)
                .ToList()
                .Select(t => new TeacherRow
                {
                    Id = t.Id,
                    FullName = t.FullName,
                    SubjectName = t.Subject?.Name ?? "-",
                    IsActive = activeTeacherIds.Contains(t.Id)
                })
                .ToList();

            TeachersList = new ObservableCollection<TeacherRow>(teacherRows);
        }

        private void ExecuteAdd()
        {
            var wnd = new TeacherEditWindow(new Teacher())
            {
                Owner = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w is not TeacherEditWindow)
                    ?? Application.Current?.MainWindow
            };

            if (wnd.ShowDialog() == true)
            {
                using var db = new SchoolDbContext();
                db.Teachers.Add(wnd.Teacher);
                db.SaveChanges(); // Сохраняем учителя, чтобы получить его Id

                // Создаем учетную запись для учителя
                var user = new User
                {
                    Username = $"teacher{wnd.Teacher.Id}",
                    Password = $"teacher{wnd.Teacher.Id}",
                    FullName = wnd.Teacher.FullName,
                    Role = UserRole.Teacher,
                    TeacherId = wnd.Teacher.Id
                };
                db.Users.Add(user);
                db.SaveChanges();

                LoadData();
            }
        }

        private void ExecuteEdit(TeacherRow? teacher)
        {
            if (teacher == null) return;

            using var db = new SchoolDbContext();
            var fromDb = db.Teachers.FirstOrDefault(t => t.Id == teacher.Id);
            if (fromDb == null) return;

            var editable = new Teacher
            {
                Id = fromDb.Id,
                FullName = fromDb.FullName,
                SubjectId = fromDb.SubjectId,
                ClassroomId = fromDb.ClassroomId
            };

            var wnd = new TeacherEditWindow(editable)
            {
                Owner = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w is not TeacherEditWindow)
                    ?? Application.Current?.MainWindow
            };
            if (wnd.ShowDialog() != true) return;

            fromDb.FullName = editable.FullName;
            fromDb.SubjectId = editable.SubjectId;
            fromDb.ClassroomId = editable.ClassroomId;

            // Обновляем имя пользователя в учетной записи
            var user = db.Users.FirstOrDefault(u => u.TeacherId == fromDb.Id);
            if (user != null)
            {
                user.FullName = fromDb.FullName;
            }

            db.SaveChanges();
            LoadData();
        }

        private void ExecuteDelete(TeacherRow? teacher)
        {
            if (teacher == null) return;

            var result = MessageBox.Show(
                $"Удалить учителя \"{teacher.FullName}\"?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            using var db = new SchoolDbContext();
            var fromDb = db.Teachers.FirstOrDefault(t => t.Id == teacher.Id);
            if (fromDb == null) return;

            // Удаляем связанные учетные записи пользователей
            var relatedUsers = db.Users.Where(u => u.TeacherId == fromDb.Id).ToList();
            if (relatedUsers.Any())
            {
                db.Users.RemoveRange(relatedUsers);
            }

            db.Teachers.Remove(fromDb);
            db.SaveChanges();

            LoadData();
        }
    }
}
