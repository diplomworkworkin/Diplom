using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using System.Net.Http;

namespace SchoolScheduleApp.ViewModels
{
    public class WorkloadRow
    {
        public int Id { get; set; }

        public int AcademicClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;

        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;

        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        public int HoursPerWeek { get; set; }
    }

    public class WorkloadsViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

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
            set { _formClassId = value; OnPropertyChanged(); _ = AutoSetSubjectFromTeacher(); }
        }

        private int _formTeacherId;
        public int FormTeacherId
        {
            get => _formTeacherId;
            set { _formTeacherId = value; OnPropertyChanged(); _ = AutoSetSubjectFromTeacher(); }
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
            _apiClient = new ApiClient();
            NewCommand = new RelayCommand((param) => ClearForm());
            SaveCommand = new RelayCommand(async (param) => await SaveWorkload());
            DeleteCommand = new RelayCommand(async (param) => await DeleteWorkload(), (param) => SelectedWorkload != null);
            RefreshCommand = new RelayCommand(async (param) => await LoadAll());

            _ = LoadAll();
            ClearForm();
        }

        private async Task LoadAll()
        {
            try
            {
                Classes = new ObservableCollection<AcademicClass>((await _apiClient.GetAcademicClassesAsync()).OrderBy(x => x.Name).ToList());
                Teachers = new ObservableCollection<Teacher>((await _apiClient.GetTeachersAsync()).OrderBy(x => x.FullName).ToList());
                Subjects = new ObservableCollection<Subject>((await _apiClient.GetSubjectsAsync()).OrderBy(x => x.Name).ToList());

                OnPropertyChanged(nameof(Classes));
                OnPropertyChanged(nameof(Teachers));
                OnPropertyChanged(nameof(Subjects));

                var workloads = await _apiClient.GetWorkloadsAsync();

                Workloads = new ObservableCollection<WorkloadRow>(
                    workloads.Select(w => new WorkloadRow
                    {
                        Id = w.Id,
                        AcademicClassId = w.AcademicClassId,
                        ClassName = Classes.FirstOrDefault(c => c.Id == w.AcademicClassId)?.Name ?? "",
                        TeacherId = w.TeacherId,
                        TeacherName = Teachers.FirstOrDefault(t => t.Id == w.TeacherId)?.FullName ?? "",
                        SubjectId = w.SubjectId,
                        SubjectName = Subjects.FirstOrDefault(s => s.Id == w.SubjectId)?.Name ?? "",
                        HoursPerWeek = w.HoursPerWeek
                    })
                );

                OnPropertyChanged(nameof(Workloads));
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка загрузки данных для нагрузок из API.", ex);
                ToastService.Show("Ошибка загрузки данных. Проверьте подключение к API.", "Ошибка");
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки данных для нагрузок.", ex);
                ToastService.Show("Произошла ошибка при загрузке данных.", "Ошибка");
            }
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

            _ = AutoSetSubjectFromTeacher();

            FormHoursPerWeek = 1;
        }

        private async Task AutoSetSubjectFromTeacher()
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

        private async Task SaveWorkload()
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

            try
            {
                var newWorkload = new WorkloadCreate
                {
                    AcademicClassId = FormClassId,
                    TeacherId = FormTeacherId,
                    SubjectId = FormSubjectId,
                    HoursPerWeek = FormHoursPerWeek
                };

                if (_editingId == 0)
                {
                    await _apiClient.AddWorkloadAsync(newWorkload);
                }
                else
                {
                    await _apiClient.UpdateWorkloadAsync(_editingId, newWorkload);
                }

                await LoadAll();
                ClearForm();
                ToastService.Show("Нагрузка сохранена.", "Успешно");
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка сохранения нагрузки через API.", ex);
                ToastService.Show($"Ошибка сохранения нагрузки: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка сохранения нагрузки.", ex);
                ToastService.Show("Произошла ошибка при сохранении нагрузки.", "Ошибка");
            }
        }

        private async Task DeleteWorkload()
        {
            if (SelectedWorkload == null) return;

            var ok = MessageBox.Show($"Удалить нагрузку: {SelectedWorkload.ClassName} / {SelectedWorkload.SubjectName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (ok != MessageBoxResult.Yes) return;

            try
            {
                await _apiClient.DeleteWorkloadAsync(SelectedWorkload.Id);
                await LoadAll();
                ClearForm();
                ToastService.Show("Нагрузка удалена.", "Успешно");
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка удаления нагрузки через API.", ex);
                ToastService.Show($"Ошибка удаления нагрузки: {ex.Message}", "Ошибка", true);
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка удаления нагрузки.", ex);
                ToastService.Show("Произошла ошибка при удалении нагрузки.", "Ошибка");
            }
        }
    }
}
