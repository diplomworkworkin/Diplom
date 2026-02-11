using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SchoolScheduleApp.ViewModels
{
    public class WorkloadRow
    {
        public int Id { get; set; }

        public int AcademicClassId { get; set; }
        public string ClassName { get; set; }

        public int TeacherId { get; set; }
        public string TeacherName { get; set; }

        public int SubjectId { get; set; }
        public string SubjectName { get; set; }

        public int HoursPerWeek { get; set; }
    }

    public class WorkloadsViewModel : ViewModelBase
    {
        public ObservableCollection<WorkloadRow> Workloads { get; set; } = new();

        public ObservableCollection<AcademicClass> Classes { get; set; } = new();
        public ObservableCollection<Teacher> Teachers { get; set; } = new();
        public ObservableCollection<Subject> Subjects { get; set; } = new();

        private WorkloadRow? _selectedWorkload;
        public WorkloadRow? SelectedWorkload
        {
            get => _selectedWorkload;
            set
            {
                _selectedWorkload = value;
                OnPropertyChanged();
                FillFormFromSelected();
            }
        }

        // ===== Поля формы (справа/сверху) =====
        private int _formClassId;
        public int FormClassId
        {
            get => _formClassId;
            set { _formClassId = value; OnPropertyChanged(); AutoSetSubjectFromTeacher(); }
        }

        private int _formTeacherId;
        public int FormTeacherId
        {
            get => _formTeacherId;
            set { _formTeacherId = value; OnPropertyChanged(); AutoSetSubjectFromTeacher(); }
        }

        private int _formSubjectId;
        public int FormSubjectId
        {
            get => _formSubjectId;
            set { _formSubjectId = value; OnPropertyChanged(); }
        }

        private int _formHoursPerWeek = 1;
        public int FormHoursPerWeek
        {
            get => _formHoursPerWeek;
            set { _formHoursPerWeek = value; OnPropertyChanged(); }
        }

        private int _editingId = 0; // 0 = добавление, >0 = редактирование

        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand RefreshCommand { get; }

        public WorkloadsViewModel()
        {
            NewCommand = new RelayCommand(_ => ClearForm());
            SaveCommand = new RelayCommand(_ => SaveWorkload());
            DeleteCommand = new RelayCommand(_ => DeleteWorkload(), _ => SelectedWorkload != null);
            RefreshCommand = new RelayCommand(_ => LoadAll());

            LoadAll();
            ClearForm();
        }

        private void LoadAll()
        {
            using var db = new SchoolDbContext();

            // Проверка: предмет должен соответствовать учителю
            // (у учителя в справочнике выбран 1 предмет)
            var teacher = db.Teachers.FirstOrDefault(t => t.Id == FormTeacherId);
            if (teacher != null && teacher.SubjectId != null && teacher.SubjectId.Value != FormSubjectId)
            {
                ToastService.Show("Выбранный предмет не соответствует предмету учителя. Проверьте предмет у учителя в разделе \"Учителя\" или выберите другого учителя.", "Ошибка", true);
                return;
            }
            Classes = new ObservableCollection<AcademicClass>(db.AcademicClasses.OrderBy(x => x.Name).ToList());
            Teachers = new ObservableCollection<Teacher>(db.Teachers.OrderBy(x => x.FullName).ToList());
            Subjects = new ObservableCollection<Subject>(db.Subjects.OrderBy(x => x.Name).ToList());

            OnPropertyChanged(nameof(Classes));
            OnPropertyChanged(nameof(Teachers));
            OnPropertyChanged(nameof(Subjects));

            var list = db.Workloads
                .Include(w => w.AcademicClass)
                .Include(w => w.Teacher)
                .Include(w => w.Subject)
                .OrderBy(w => w.AcademicClass.Name)
                .ThenBy(w => w.Subject.Name)
                .ToList();

            Workloads = new ObservableCollection<WorkloadRow>(
                list.Select(w => new WorkloadRow
                {
                    Id = w.Id,
                    AcademicClassId = w.AcademicClassId,
                    ClassName = w.AcademicClass?.Name ?? "",
                    TeacherId = w.TeacherId,
                    TeacherName = w.Teacher?.FullName ?? "",
                    SubjectId = w.SubjectId,
                    SubjectName = w.Subject?.Name ?? "",
                    HoursPerWeek = w.HoursPerWeek
                })
            );

            OnPropertyChanged(nameof(Workloads));
        }

        private void FillFormFromSelected()
        {
            if (SelectedWorkload == null) return;

            _editingId = SelectedWorkload.Id;

            FormClassId = SelectedWorkload.AcademicClassId;
            FormTeacherId = SelectedWorkload.TeacherId;
            FormSubjectId = SelectedWorkload.SubjectId;
            FormHoursPerWeek = SelectedWorkload.HoursPerWeek;
        }

        private void ClearForm()
        {
            _editingId = 0;
            SelectedWorkload = null;

            // ставим по умолчанию первые элементы (если есть)
            FormClassId = Classes.FirstOrDefault()?.Id ?? 0;
            FormTeacherId = Teachers.FirstOrDefault()?.Id ?? 0;

            AutoSetSubjectFromTeacher();

            FormHoursPerWeek = 1;
        }

        private void AutoSetSubjectFromTeacher()
        {
            // если у учителя есть SubjectId - автоматически проставим предмет
            var t = Teachers.FirstOrDefault(x => x.Id == FormTeacherId);
            if (t != null && t.SubjectId != null)
            {
                FormSubjectId = t.SubjectId.Value;
            }
            else
            {
                // иначе просто оставим текущий/первый
                if (FormSubjectId == 0)
                    FormSubjectId = Subjects.FirstOrDefault()?.Id ?? 0;
            }
        }

        private void SaveWorkload()
        {
            if (FormClassId <= 0)
            {
                ToastService.Show("Выберите класс.", "Ошибка", true);
                return;
            }
            if (FormTeacherId <= 0)
            {
                ToastService.Show("Выберите учителя.", "Ошибка", true);
                return;
            }
            if (FormSubjectId <= 0)
            {
                ToastService.Show("Выберите предмет.", "Ошибка", true);
                return;
            }

            // разумное ограничение для недели (можешь поменять под себя)
            if (FormHoursPerWeek < 1 || FormHoursPerWeek > 10)
            {
                ToastService.Show("Часов в неделю должно быть от 1 до 10.", "Ошибка", true);
                return;
            }

            using var db = new SchoolDbContext();

            // Проверка: предмет должен соответствовать учителю
            // (у учителя в справочнике выбран 1 предмет)
            var teacher = db.Teachers.FirstOrDefault(t => t.Id == FormTeacherId);
            if (teacher != null && teacher.SubjectId != null && teacher.SubjectId.Value != FormSubjectId)
            {
                ToastService.Show("Выбранный предмет не соответствует предмету учителя. Проверьте предмет учителя в разделе \"Учителя\" или выберите другого учителя.", "Ошибка", true);
                return;
            }

            // запрет дублей: одинаковые (класс + предмет) в нагрузке
            bool duplicate = db.Workloads.Any(w =>
                w.AcademicClassId == FormClassId &&
                w.SubjectId == FormSubjectId &&
                w.Id != _editingId);

            if (duplicate)
            {
                ToastService.Show("Для этого класса нагрузка по этому предмету уже существует.", "Ошибка", true);
                return;
            }

            if (_editingId == 0)
            {
                var w = new Workload
                {
                    AcademicClassId = FormClassId,
                    TeacherId = FormTeacherId,
                    SubjectId = FormSubjectId,
                    HoursPerWeek = FormHoursPerWeek
                };

                db.Workloads.Add(w);
                db.SaveChanges();
            }
            else
            {
                var w = db.Workloads.FirstOrDefault(x => x.Id == _editingId);
                if (w == null) return;

                w.AcademicClassId = FormClassId;
                w.TeacherId = FormTeacherId;
                w.SubjectId = FormSubjectId;
                w.HoursPerWeek = FormHoursPerWeek;

                db.SaveChanges();
            }

            LoadAll();
            ClearForm();
            ToastService.Show("Нагрузка сохранена.", "Успешно");
        }

        private void DeleteWorkload()
        {
            if (SelectedWorkload == null) return;

            var ok = MessageBox.Show($"Удалить нагрузку: {SelectedWorkload.ClassName} / {SelectedWorkload.SubjectName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (ok != MessageBoxResult.Yes) return;

            using var db = new SchoolDbContext();

            var w = db.Workloads.FirstOrDefault(x => x.Id == SelectedWorkload.Id);
            if (w == null) return;

            db.Workloads.Remove(w);
            db.SaveChanges();

            LoadAll();
            ClearForm();
        }
    }
}
