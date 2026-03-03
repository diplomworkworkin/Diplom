using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SchoolScheduleApp.Data.Entites
{
    public enum UserRole
    {
        Admin = 0,
        Teacher = 1,
        Student = 2
    }

    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SubjectCreate
    {
        public string Name { get; set; } = string.Empty;
    }

    public class Classroom
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public string? Type { get; set; }
    }

    public class ClassroomCreate
    {
        public string Number { get; set; } = string.Empty;
        public int? Capacity { get; set; }
        public string? Type { get; set; }
    }

    public class Teacher
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public int? ClassroomId { get; set; }
        public Subject? Subject { get; set; }
        public Classroom? Classroom { get; set; }
    }

    public class TeacherCreate
    {
        public string FullName { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public int? ClassroomId { get; set; }
    }

    public class AcademicClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int Shift { get; set; }
        public int? CuratorTeacherId { get; set; }
        public Teacher? CuratorTeacher { get; set; }
    }

    public class AcademicClassCreate
    {
        public string Name { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public int Shift { get; set; }
        public int? CuratorTeacherId { get; set; }
    }

    public class Workload
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int AcademicClassId { get; set; }
        public int HoursPerWeek { get; set; }
        public Teacher? Teacher { get; set; }
        public Subject? Subject { get; set; }
        public AcademicClass? AcademicClass { get; set; }
    }

    public class WorkloadCreate
    {
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int AcademicClassId { get; set; }
        public int HoursPerWeek { get; set; }
    }

    public class Lesson
    {
        public int Id { get; set; }
        public int DayOfWeek { get; set; }
        public int LessonIndex { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int AcademicClassId { get; set; }
        public int? ClassroomId { get; set; }
        public Teacher? Teacher { get; set; }
        public Subject? Subject { get; set; }
        public AcademicClass? AcademicClass { get; set; }
        public Classroom? Classroom { get; set; }
    }

    public class LessonCreate
    {
        public int DayOfWeek { get; set; }
        public int LessonIndex { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int AcademicClassId { get; set; }
        public int? ClassroomId { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        [JsonIgnore]
        public string? Password { get; set; }
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int? TeacherId { get; set; }
        public int? AcademicClassId { get; set; }
        public Teacher? Teacher { get; set; }
        public AcademicClass? AcademicClass { get; set; }
    }

    public class UserCreate
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int? TeacherId { get; set; }
        public int? AcademicClassId { get; set; }
    }

    public class UserLogin
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
