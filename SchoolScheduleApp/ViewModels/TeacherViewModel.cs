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
    public class TeachersViewModel : ViewModelBase
    {
        private ObservableCollection<Teacher> _teachersList = new();
        public ObservableCollection<Teacher> TeachersList
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
            EditCommand = new RelayCommand(o => ExecuteEdit(o as Teacher));
            DeleteCommand = new RelayCommand(o => ExecuteDelete(o as Teacher));

            LoadData();
        }

        private void LoadData()
        {
            using var db = new SchoolDbContext();

            // Важно: Include нужен, чтобы в таблице был Subject.Name
            var list = db.Teachers
                .Include(t => t.Subject)
                .OrderBy(t => t.FullName)
                .ToList();

            TeachersList = new ObservableCollection<Teacher>(list);
        }

        private void ExecuteAdd()
        {
            var newTeacher = new Teacher();
            var wnd = new TeacherEditWindow(newTeacher);

            if (wnd.ShowDialog() == true)
            {
                using var db = new SchoolDbContext();
                db.Teachers.Add(wnd.Teacher);
                db.SaveChanges();

                LoadData();
            }
        }

        private void ExecuteEdit(Teacher? teacher)
        {
            if (teacher == null) return;

            // Работаем с копией, чтобы "Отмена" не портила строку в таблице
            var editable = new Teacher
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                SubjectId = teacher.SubjectId
            };

            var wnd = new TeacherEditWindow(editable);
            if (wnd.ShowDialog() == true)
            {
                using var db = new SchoolDbContext();
                var fromDb = db.Teachers.FirstOrDefault(t => t.Id == editable.Id);
                if (fromDb == null) return;

                fromDb.FullName = editable.FullName;
                fromDb.SubjectId = editable.SubjectId;

                db.SaveChanges();
                LoadData();
            }
        }

        private void ExecuteDelete(Teacher? teacher)
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

            db.Teachers.Remove(fromDb);
            db.SaveChanges();

            LoadData();
        }
    }
}
