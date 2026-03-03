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
        private const string BaseUrl = "http://localhost:8000";

        public ApiClient()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        }

        private async Task<T> GetAsync<T>(string requestUri)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(responseBody) ?? throw new Exception("Failed to deserialize response");
        }

        private async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest content)
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, httpContent);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseBody) ?? throw new Exception("Failed to deserialize response");
        }

        private async Task<TResponse> PutAsync<TRequest, TResponse>(string requestUri, TRequest content)
        {
            string jsonContent = JsonConvert.SerializeObject(content);
            HttpContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PutAsync(requestUri, httpContent);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseBody) ?? throw new Exception("Failed to deserialize response");
        }

        private async Task DeleteAsync(string requestUri)
        {
            HttpResponseMessage response = await _httpClient.DeleteAsync(requestUri);
            response.EnsureSuccessStatusCode();
        }

        // Auth Endpoints
        public async Task<User> LoginAsync(UserLogin loginData)
        {
            return await PostAsync<UserLogin, User>("/login", loginData);
        }

        public async Task<User> RegisterUserAsync(UserCreate userCreate)
        {
            return await PostAsync<UserCreate, User>("/register", userCreate);
        }

        // User Endpoints
        public async Task<List<User>> GetUsersAsync()
        {
            return await GetAsync<List<User>>("/users/");
        }

        // Subject Endpoints
        public async Task<List<Subject>> GetSubjectsAsync()
        {
            return await GetAsync<List<Subject>>("/subjects/");
        }

        public async Task<Subject> AddSubjectAsync(SubjectCreate subjectCreate)
        {
            return await PostAsync<SubjectCreate, Subject>("/subjects/", subjectCreate);
        }

        // Classroom Endpoints
        public async Task<List<Classroom>> GetClassroomsAsync()
        {
            return await GetAsync<List<Classroom>>("/classrooms/");
        }

        public async Task<Classroom> AddClassroomAsync(ClassroomCreate classroomCreate)
        {
            return await PostAsync<ClassroomCreate, Classroom>("/classrooms/", classroomCreate);
        }

        // Teacher Endpoints
        public async Task<List<Teacher>> GetTeachersAsync()
        {
            return await GetAsync<List<Teacher>>("/teachers/");
        }

        public async Task<Teacher> GetTeacherByIdAsync(int id)
        {
            return await GetAsync<Teacher>($"/teachers/{id}");
        }

        public async Task<Teacher> AddTeacherAsync(TeacherCreate teacherCreate)
        {
            return await PostAsync<TeacherCreate, Teacher>("/teachers/", teacherCreate);
        }

        public async Task<Teacher> UpdateTeacherAsync(int id, Teacher teacher)
        {
            var updateData = new TeacherCreate
            {
                FullName = teacher.FullName,
                SubjectId = teacher.SubjectId,
                ClassroomId = teacher.ClassroomId
            };
            return await PutAsync<TeacherCreate, Teacher>($"/teachers/{id}", updateData);
        }

        public async Task DeleteTeacherAsync(int id)
        {
            await DeleteAsync($"/teachers/{id}");
        }

        // AcademicClass Endpoints
        public async Task<List<AcademicClass>> GetAcademicClassesAsync()
        {
            return await GetAsync<List<AcademicClass>>("/classes/");
        }

        public async Task<AcademicClass> AddAcademicClassAsync(AcademicClass academicClass)
        {
            var createData = new AcademicClassCreate
            {
                Name = academicClass.Name,
                StudentCount = academicClass.StudentCount,
                Shift = academicClass.Shift,
                CuratorTeacherId = academicClass.CuratorTeacherId
            };
            return await PostAsync<AcademicClassCreate, AcademicClass>("/classes/", createData);
        }

        public async Task<AcademicClass> UpdateAcademicClassAsync(int id, AcademicClass academicClass)
        {
            var updateData = new AcademicClassCreate
            {
                Name = academicClass.Name,
                StudentCount = academicClass.StudentCount,
                Shift = academicClass.Shift,
                CuratorTeacherId = academicClass.CuratorTeacherId
            };
            return await PutAsync<AcademicClassCreate, AcademicClass>($"/classes/{id}", updateData);
        }

        public async Task DeleteAcademicClassAsync(int id)
        {
            await DeleteAsync($"/classes/{id}");
        }

        // Workload Endpoints
        public async Task<List<Workload>> GetWorkloadsAsync()
        {
            return await GetAsync<List<Workload>>("/workloads/");
        }

        public async Task<Workload> AddWorkloadAsync(WorkloadCreate workloadCreate)
        {
            return await PostAsync<WorkloadCreate, Workload>("/workloads/", workloadCreate);
        }

        public async Task<Workload> UpdateWorkloadAsync(int id, WorkloadCreate workloadUpdate)
        {
            return await PutAsync<WorkloadCreate, Workload>($"/workloads/{id}", workloadUpdate);
        }

        public async Task DeleteWorkloadAsync(int id)
        {
            await DeleteAsync($"/workloads/{id}");
        }

        // Lesson Endpoints
        public async Task<List<Lesson>> GetLessonsAsync(int? classId = null, int? teacherId = null, int? dayOfWeek = null, int? subjectId = null)
        {
            string requestUri = "/lessons/";
            List<string> queryParams = new List<string>();
            if (classId.HasValue) queryParams.Add($"class_id={classId.Value}");
            if (teacherId.HasValue) queryParams.Add($"teacher_id={teacherId.Value}");
            if (dayOfWeek.HasValue) queryParams.Add($"day_of_week={dayOfWeek.Value}");
            if (subjectId.HasValue) queryParams.Add($"subject_id={subjectId.Value}");

            if (queryParams.Count > 0)
            {
                requestUri += "?" + string.Join("&", queryParams);
            }
            return await GetAsync<List<Lesson>>(requestUri);
        }

        public async Task<Lesson> AddLessonAsync(LessonCreate lessonCreate)
        {
            return await PostAsync<LessonCreate, Lesson>("/lessons/", lessonCreate);
        }
    }
}
