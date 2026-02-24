using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Entites;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SchoolSchedule.Context
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext()
        {
        }
                  
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }
        public DbSet<AcademicClass> AcademicClasses { get; set; }
        public DbSet<Workload> Workloads { get; set; }
        public DbSet<Lesson> Lessons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=School11_Schedule_DB;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<AcademicClass>()
                .HasOne(c => c.CuratorTeacher)
                .WithMany()
                .HasForeignKey(c => c.CuratorTeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AcademicClass>()
                .HasIndex(c => c.CuratorTeacherId)
                .IsUnique();

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Subject)
                .WithMany()
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Classroom)
                .WithMany()
                .HasForeignKey(t => t.ClassroomId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lesson>()
                .HasIndex(x => new { x.AcademicClassId, x.DayOfWeek, x.LessonIndex })
                .IsUnique();

            modelBuilder.Entity<Lesson>()
                .HasIndex(x => new { x.TeacherId, x.DayOfWeek, x.LessonIndex })
                .IsUnique();

            modelBuilder.Entity<Lesson>()
                .HasIndex(x => new { x.ClassroomId, x.DayOfWeek, x.LessonIndex })
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Teacher)
                .WithMany()
                .HasForeignKey(u => u.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.AcademicClass)
                .WithMany()
                .HasForeignKey(u => u.AcademicClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // 1. Предметы
            modelBuilder.Entity<Subject>().HasData(
                new Subject { Id = 1, Name = "Математика" },
                new Subject { Id = 2, Name = "Русский язык" },
                new Subject { Id = 3, Name = "Информатика" },
                new Subject { Id = 4, Name = "Физика" },
                new Subject { Id = 5, Name = "История" },
                new Subject { Id = 6, Name = "Английский язык" },
                new Subject { Id = 7, Name = "Физкультура" },
                new Subject { Id = 8, Name = "Литература" }
            );

            // 2. Кабинеты
            modelBuilder.Entity<Classroom>().HasData(
                new Classroom { Id = 1, Number = "101", Capacity = 30, Type = "Математика" },
                new Classroom { Id = 2, Number = "102", Capacity = 30, Type = "Русский язык" },
                new Classroom { Id = 3, Number = "201", Capacity = 15, Type = "Компьютерный" },
                new Classroom { Id = 4, Number = "202", Capacity = 30, Type = "Физика" },
                new Classroom { Id = 5, Number = "301", Capacity = 30, Type = "История" },
                new Classroom { Id = 6, Number = "302", Capacity = 30, Type = "Лингафонный" },
                new Classroom { Id = 7, Number = "Спортзал", Capacity = 60, Type = "Спортзал" },
                new Classroom { Id = 8, Number = "Актовый зал", Capacity = 100, Type = "Лекционный" }
            );

            // 3. Учителя
            modelBuilder.Entity<Teacher>().HasData(
                new Teacher { Id = 1, FullName = "Иванов Иван Иванович", SubjectId = 1, ClassroomId = 1 },
                new Teacher { Id = 2, FullName = "Петрова Анна Сергеевна", SubjectId = 2, ClassroomId = 2 },
                new Teacher { Id = 3, FullName = "Сидоров Петр Алексеевич", SubjectId = 3, ClassroomId = 3 },
                new Teacher { Id = 4, FullName = "Кузнецова Ольга Владимировна", SubjectId = 4, ClassroomId = 4 },
                new Teacher { Id = 5, FullName = "Морозов Дмитрий Николаевич", SubjectId = 5, ClassroomId = 5 },
                new Teacher { Id = 6, FullName = "Васильева Елена Игоревна", SubjectId = 6, ClassroomId = 6 },
                new Teacher { Id = 7, FullName = "Смирнов Андрей Викторович", SubjectId = 7, ClassroomId = 7 },
                new Teacher { Id = 8, FullName = "Павлова Марина Анатольевна", SubjectId = 8, ClassroomId = 8 }
            );

            // 4. Классы
            modelBuilder.Entity<AcademicClass>().HasData(
                new AcademicClass { Id = 1, Name = "9-А", StudentCount = 25, Shift = 1, CuratorTeacherId = 1 },
                new AcademicClass { Id = 2, Name = "9-Б", StudentCount = 24, Shift = 1, CuratorTeacherId = 2 },
                new AcademicClass { Id = 3, Name = "10-А", StudentCount = 22, Shift = 1, CuratorTeacherId = 3 },
                new AcademicClass { Id = 4, Name = "11-А", StudentCount = 28, Shift = 1, CuratorTeacherId = 4 }
            );

            // 5. Нагрузка (Workload) - распределяем часы
            modelBuilder.Entity<Workload>().HasData(
                // 9-А
                new Workload { Id = 1, AcademicClassId = 1, TeacherId = 1, SubjectId = 1, HoursPerWeek = 5 },
                new Workload { Id = 2, AcademicClassId = 1, TeacherId = 2, SubjectId = 2, HoursPerWeek = 4 },
                new Workload { Id = 3, AcademicClassId = 1, TeacherId = 3, SubjectId = 3, HoursPerWeek = 2 },
                new Workload { Id = 4, AcademicClassId = 1, TeacherId = 7, SubjectId = 7, HoursPerWeek = 3 },
                // 9-Б
                new Workload { Id = 5, AcademicClassId = 2, TeacherId = 1, SubjectId = 1, HoursPerWeek = 5 },
                new Workload { Id = 6, AcademicClassId = 2, TeacherId = 5, SubjectId = 5, HoursPerWeek = 3 },
                new Workload { Id = 7, AcademicClassId = 2, TeacherId = 6, SubjectId = 6, HoursPerWeek = 4 },
                new Workload { Id = 8, AcademicClassId = 2, TeacherId = 8, SubjectId = 8, HoursPerWeek = 3 },
                // 10-А
                new Workload { Id = 9, AcademicClassId = 3, TeacherId = 1, SubjectId = 1, HoursPerWeek = 6 },
                new Workload { Id = 10, AcademicClassId = 3, TeacherId = 4, SubjectId = 4, HoursPerWeek = 4 },
                new Workload { Id = 11, AcademicClassId = 3, TeacherId = 3, SubjectId = 3, HoursPerWeek = 3 },
                new Workload { Id = 12, AcademicClassId = 3, TeacherId = 2, SubjectId = 2, HoursPerWeek = 4 },
                // 11-А
                new Workload { Id = 13, AcademicClassId = 4, TeacherId = 1, SubjectId = 1, HoursPerWeek = 6 },
                new Workload { Id = 14, AcademicClassId = 4, TeacherId = 4, SubjectId = 4, HoursPerWeek = 5 },
                new Workload { Id = 15, AcademicClassId = 4, TeacherId = 6, SubjectId = 6, HoursPerWeek = 5 },
                new Workload { Id = 16, AcademicClassId = 4, TeacherId = 8, SubjectId = 8, HoursPerWeek = 4 }
            );

            // 6. Пользователи (только админ и автоматически созданные учителя)
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Password = "admin", FullName = "Системный Администратор", Role = UserRole.Admin }
            );

            // Добавляем пользователей для каждого учителя по вашему правилу teacher{id}
            for (int i = 1; i <= 8; i++)
            {
                modelBuilder.Entity<User>().HasData(new User
                {
                    Id = 10 + i,
                    Username = $"teacher{i}",
                    Password = $"teacher{i}",
                    FullName = GetTeacherName(i),
                    Role = UserRole.Teacher,
                    TeacherId = i
                });
            }
        }

        private string GetTeacherName(int id) => id switch
        {
            1 => "Иванов Иван Иванович",
            2 => "Петрова Анна Сергеевна",
            3 => "Сидоров Петр Алексеевич",
            4 => "Кузнецова Ольга Владимировна",
            5 => "Морозов Дмитрий Николаевич",
            6 => "Васильева Елена Игоревна",
            7 => "Смирнов Андрей Викторович",
            8 => "Павлова Марина Анатольевна",
            _ => "Учитель"
        };
    }
}
