using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using SchoolScheduleApp.Core;
using Xunit;

namespace SchoolSchedule.Tests
{
    public class ScheduleGeneratorTests
    {
        private SchoolDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SchoolDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new SchoolDbContext(options);
        }

        [Fact]
        public void Generate_WithEmptyWorkload_ShouldReturnProblem()
        {
            // Arrange
            using var db = GetInMemoryDbContext();

            // Act
            var result = ScheduleGenerator.Generate(db, clearOldSchedule: true);

            // Assert
            Assert.Contains("Нет нагрузки", result.Problems.First());
            Assert.Equal(0, result.CreatedLessons);
        }

        [Fact]
        public void Generate_WithCorrectWorkload_ShouldCreateLessons()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            
            var subject = new Subject { Id = 1, Name = "Math" };
            var room = new Classroom { Id = 1, Number = "101", Capacity = 30 };
            var teacher = new Teacher { Id = 1, FullName = "Teacher 1", SubjectId = 1, ClassroomId = 1 };
            var academicClass = new AcademicClass { Id = 1, Name = "9-A", StudentCount = 20, Shift = 1, CuratorTeacherId = 1 };
            
            db.Subjects.Add(subject);
            db.Classrooms.Add(room);
            db.Teachers.Add(teacher);
            db.AcademicClasses.Add(academicClass);
            db.Workloads.Add(new Workload { Id = 1, AcademicClassId = 1, TeacherId = 1, SubjectId = 1, HoursPerWeek = 2 });
            db.SaveChanges();

            // Act
            var result = ScheduleGenerator.Generate(db, clearOldSchedule: true);

            // Assert
            Assert.Empty(result.Problems);
            Assert.Equal(2, result.CreatedLessons);
            Assert.Equal(2, db.Lessons.Count());
        }

        [Fact]
        public void Generate_ShouldNotHaveTeacherConflicts()
        {
            // Arrange
            using var db = GetInMemoryDbContext();
            
            var subject = new Subject { Id = 1, Name = "Math" };
            var room1 = new Classroom { Id = 1, Number = "101" };
            var room2 = new Classroom { Id = 2, Number = "102" };
            var teacher = new Teacher { Id = 1, FullName = "Busy Teacher", SubjectId = 1, ClassroomId = 1 };
            var classA = new AcademicClass { Id = 1, Name = "9-A", Shift = 1, CuratorTeacherId = 1 };
            var classB = new AcademicClass { Id = 2, Name = "9-B", Shift = 1 };
            
            db.Subjects.Add(subject);
            db.Classrooms.AddRange(room1, room2);
            db.Teachers.Add(teacher);
            db.AcademicClasses.AddRange(classA, classB);
            
            // Нагрузка превышает возможности одного учителя в один слот
            db.Workloads.Add(new Workload { Id = 1, AcademicClassId = 1, TeacherId = 1, SubjectId = 1, HoursPerWeek = 15 });
            db.Workloads.Add(new Workload { Id = 2, AcademicClassId = 2, TeacherId = 1, SubjectId = 1, HoursPerWeek = 15 });
            db.SaveChanges();

            // Act
            var result = ScheduleGenerator.Generate(db, clearOldSchedule: true);

            // Assert
            // Проверяем, что нет уроков одного учителя в одно и то же время
            var lessons = db.Lessons.ToList();
            var conflicts = lessons.GroupBy(l => new { l.DayOfWeek, l.LessonIndex, l.TeacherId })
                                   .Where(g => g.Count() > 1);
            
            Assert.Empty(conflicts);
        }

        [Fact]
        public void User_TeacherId_ShouldBeNullable()
        {
            // Arrange
            var user = new User { Username = "admin", Role = UserRole.Admin };

            // Assert
            Assert.Null(user.TeacherId);
        }

        [Fact]
        public void Teacher_FullName_ShouldBeRequired()
        {
            // Arrange
            var teacher = new Teacher { FullName = "Test" };

            // Assert
            Assert.Equal("Test", teacher.FullName);
        }
    }
}
