using SchoolScheduleApp.Data.Entites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoolScheduleApp.Core
{
    public class ScheduleGenerateResult
    {
        public int CreatedLessons { get; set; }
        public List<string> Problems { get; set; } = new();
    }

    public static class ScheduleGenerator
    {
        private const int DaysPerWeek = 5;
        private const int LessonsPerShift = 6;

        public static event Action? ScheduleChanged;

        public static async Task<ScheduleGenerateResult> GenerateAsync(bool clearOldSchedule = true)
        {
            var apiClient = new ApiClient();
            var res = new ScheduleGenerateResult();

            try
            {
                var workloads = await apiClient.GetWorkloadsAsync();
                var classes = await apiClient.GetAcademicClassesAsync();
                var teachers = await apiClient.GetTeachersAsync();
                var subjects = await apiClient.GetSubjectsAsync();
                var rooms = await apiClient.GetClassroomsAsync();

                if (workloads.Count == 0)
                {
                    res.Problems.Add("Нет нагрузки (Workloads). Сначала заполните учебную нагрузку.");
                    return res;
                }

                // TODO: API должен поддерживать удаление всех уроков. 
                // Пока что мы просто будем добавлять новые.
                // if (clearOldSchedule) { ... }

                var busyClass = new HashSet<string>();
                var busyTeacher = new HashSet<string>();
                var busyRoom = new HashSet<string>();
                var lessonsToCreate = new List<LessonCreate>();

                var ordered = workloads.OrderByDescending(w => w.HoursPerWeek).ToList();

                foreach (var w in ordered)
                {
                    var cls = classes.FirstOrDefault(c => c.Id == w.AcademicClassId);
                    var teacher = teachers.FirstOrDefault(t => t.Id == w.TeacherId);
                    var subject = subjects.FirstOrDefault(s => s.Id == w.SubjectId);

                    if (cls == null || subject == null || teacher == null)
                    {
                        res.Problems.Add($"Workload #{w.Id}: не хватает связей.");
                        continue;
                    }

                    int shift = cls.Shift;
                    int startIndex = shift == 1 ? 1 : 7;
                    int endIndex = shift == 1 ? 6 : 12;
                    var classSubjectDayUsed = new HashSet<string>();

                    for (int i = 0; i < w.HoursPerWeek; i++)
                    {
                        bool ok = TryPlace(w, cls, teacher, startIndex, endIndex, rooms,
                            busyClass, busyTeacher, busyRoom, classSubjectDayUsed,
                            out LessonCreate lesson);

                        if (!ok)
                        {
                            res.Problems.Add($"Не удалось поставить: класс {cls.Name}, предмет {subject.Name} (час {i + 1}/{w.HoursPerWeek}).");
                            continue;
                        }

                        lessonsToCreate.Add(lesson);
                    }
                }

                foreach (var l in lessonsToCreate)
                {
                    await apiClient.AddLessonAsync(l);
                }

                res.CreatedLessons = lessonsToCreate.Count;
                ScheduleChanged?.Invoke();
            }
            catch (Exception ex)
            {
                res.Problems.Add($"Ошибка при генерации: {ex.Message}");
            }

            return res;
        }

        private static bool TryPlace(
            Workload w, AcademicClass cls, Teacher teacher,
            int startIndex, int endIndex, List<Classroom> rooms,
            HashSet<string> busyClass, HashSet<string> busyTeacher, HashSet<string> busyRoom,
            HashSet<string> classSubjectDayUsed,
            out LessonCreate lesson)
        {
            lesson = null!;

            for (int day = 1; day <= DaysPerWeek; day++)
            {
                for (int idx = startIndex; idx <= endIndex; idx++)
                {
                    string classKey = $"{w.AcademicClassId}-{day}-{idx}";
                    string teacherKey = $"{w.TeacherId}-{day}-{idx}";

                    if (busyClass.Contains(classKey)) continue;
                    if (busyTeacher.Contains(teacherKey)) continue;

                    string subDayKey = $"{w.AcademicClassId}-{w.SubjectId}-{day}";
                    if (classSubjectDayUsed.Contains(subDayKey)) continue;

                    int? roomId = PickRoom(cls, teacher, rooms, busyRoom, day, idx);

                    busyClass.Add(classKey);
                    busyTeacher.Add(teacherKey);
                    classSubjectDayUsed.Add(subDayKey);

                    if (roomId != null)
                        busyRoom.Add($"{roomId}-{day}-{idx}");

                    lesson = new LessonCreate
                    {
                        DayOfWeek = day,
                        LessonIndex = idx,
                        AcademicClassId = w.AcademicClassId,
                        TeacherId = w.TeacherId,
                        SubjectId = w.SubjectId,
                        ClassroomId = roomId
                    };

                    return true;
                }
            }

            return false;
        }

        private static int? PickRoom(AcademicClass cls, Teacher teacher, List<Classroom> rooms, HashSet<string> busyRoom, int day, int idx)
        {
            int students = cls.StudentCount;

            if (teacher.ClassroomId != null)
            {
                var ownRoom = rooms.FirstOrDefault(r => r.Id == teacher.ClassroomId.Value);
                if (ownRoom != null)
                {
                    var ownRoomKey = $"{ownRoom.Id}-{day}-{idx}";
                    var roomFits = ownRoom.Capacity <= 0 || students <= 0 || ownRoom.Capacity >= students;
                    if (roomFits && !busyRoom.Contains(ownRoomKey))
                    {
                        return ownRoom.Id;
                    }
                }
            }

            foreach (var r in rooms)
            {
                if (r.Capacity > 0 && students > 0 && r.Capacity < students)
                    continue;

                string roomKey = $"{r.Id}-{day}-{idx}";
                if (busyRoom.Contains(roomKey))
                    continue;

                return r.Id;
            }

            return null;
        }
    }
}
