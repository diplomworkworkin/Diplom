using Newtonsoft.Json;
using SchoolScheduleApp.Data.Entites;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SchoolScheduleApp.Core
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8000"; // TODO: Make configurable

        public ApiClient()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        private async Task<T> GetAsync<T>(string requestUri)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseBody);
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest content)
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, httpContent);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseBody);
        }

        // User Endpoints
        public async Task<List<User>> GetUsersAsync()
        {
            return await GetAsync<List<User>>("/users/");
        }

        public async Task<User> CreateUserAsync(UserCreate userCreate)
        {
            return await PostAsync<UserCreate, User>("/users/", userCreate);
        }

        // Subject Endpoints
        public async Task<List<Subject>> GetSubjectsAsync()
        {
            return await GetAsync<List<Subject>>("/subjects/");
        }

        public async Task<Subject> CreateSubjectAsync(SubjectCreate subjectCreate)
        {
            return await PostAsync<SubjectCreate, Subject>("/subjects/", subjectCreate);
        }

        // Classroom Endpoints
        public async Task<List<Classroom>> GetClassroomsAsync()
        {
            return await GetAsync<List<Classroom>>("/classrooms/");
        }

        public async Task<Classroom> CreateClassroomAsync(ClassroomCreate classroomCreate)
        {
            return await PostAsync<ClassroomCreate, Classroom>("/classrooms/", classroomCreate);
        }

        // Teacher Endpoints
        public async Task<List<Teacher>> GetTeachersAsync()
        {
            return await GetAsync<List<Teacher>>("/teachers/");
        }

        public async Task<Teacher> CreateTeacherAsync(TeacherCreate teacherCreate)
        {
            return await PostAsync<TeacherCreate, Teacher>("/teachers/", teacherCreate);
        }

        // AcademicClass Endpoints
        public async Task<List<AcademicClass>> GetAcademicClassesAsync()
        {
            return await GetAsync<List<AcademicClass>>("/classes/");
        }

        public async Task<AcademicClass> CreateAcademicClassAsync(AcademicClassCreate academicClassCreate)
        {
            return await PostAsync<AcademicClassCreate, AcademicClass>("/classes/", academicClassCreate);
        }

        // Workload Endpoints
        public async Task<List<Workload>> GetWorkloadsAsync()
        {
            return await GetAsync<List<Workload>>("/workloads/");
        }

        public async Task<Workload> CreateWorkloadAsync(WorkloadCreate workloadCreate)
        {
            return await PostAsync<WorkloadCreate, Workload>("/workloads/", workloadCreate);
        }

        // Lesson Endpoints
        public async Task<List<Lesson>> GetLessonsAsync(int? classId = null, int? teacherId = null)
        {
            string requestUri = "/lessons/";
            List<string> queryParams = new List<string>();
            if (classId.HasValue) queryParams.Add($"class_id={classId.Value}");
            if (teacherId.HasValue) queryParams.Add($"teacher_id={teacherId.Value}");

            if (queryParams.Count > 0)
            {
                requestUri += "?" + string.Join("&", queryParams);
            }
            return await GetAsync<List<Lesson>>(requestUri);
        }

        public async Task<Lesson> CreateLessonAsync(LessonCreate lessonCreate)
        {
            return await PostAsync<LessonCreate, Lesson>("/lessons/", lessonCreate);
        }
    }
}
