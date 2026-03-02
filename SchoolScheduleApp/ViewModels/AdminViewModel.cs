using SchoolScheduleApp.Core;
using SchoolScheduleApp.Data.Entites;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SchoolScheduleApp.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        private ObservableCollection<Teacher> _teachers;
        public ObservableCollection<Teacher> Teachers
        {
            get => _teachers;
            set { _teachers = value; OnPropertyChanged(); }
        }

        private ObservableCollection<AcademicClass> _academicClasses;
        public ObservableCollection<AcademicClass> AcademicClasses
        {
            get => _academicClasses;
            set { _academicClasses = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Subject> _subjects;
        public ObservableCollection<Subject> Subjects
        {
            get => _subjects;
            set { _subjects = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Classroom> _classrooms;
        public ObservableCollection<Classroom> Classrooms
        {
            get => _classrooms;
            set { _classrooms = value; OnPropertyChanged(); }
        }

        public ICommand LoadDataCommand { get; }

        public AdminViewModel()
        {
            _apiClient = new ApiClient();
            LoadDataCommand = new RelayCommand(async (param) => await LoadDataAsync());
            _ = LoadDataAsync(); // Initial load
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Teachers = new ObservableCollection<Teacher>(await _apiClient.GetTeachersAsync());
                AcademicClasses = new ObservableCollection<AcademicClass>(await _apiClient.GetAcademicClassesAsync());
                Subjects = new ObservableCollection<Subject>(await _apiClient.GetSubjectsAsync());
                Classrooms = new ObservableCollection<Classroom>(await _apiClient.GetClassroomsAsync());
            }
            catch (System.Exception ex)
            {
                AppLogger.LogError("Ошибка загрузки данных для панели администратора.", ex);
                ToastService.Show("Ошибка загрузки данных.", "Ошибка");
            }
        }
    }
}
