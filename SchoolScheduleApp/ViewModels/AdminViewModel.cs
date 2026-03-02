using SchoolScheduleApp.Data.Context;
using SchoolScheduleApp.Core;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SchoolScheduleApp.ViewModels
{
    public class AdminViewModel : ViewModelBase
    {
        private int _teachersCount;
        public int TeachersCount
        {
            get => _teachersCount;
            set { _teachersCount = value; OnPropertyChanged(); }
        }

        private int _studentsCount;
        public int StudentsCount
        {
            get => _studentsCount;
            set { _studentsCount = value; OnPropertyChanged(); }
        }

        private string _scheduleStatus = string.Empty;
        public string ScheduleStatus
        {
            get => _scheduleStatus;
            set { _scheduleStatus = value; OnPropertyChanged(); }
        }

        private Brush _scheduleStatusBrush = Brushes.Red;
        public Brush ScheduleStatusBrush
        {
            get => _scheduleStatusBrush;
            set { _scheduleStatusBrush = value; OnPropertyChanged(); }
        }

        private PointCollection _roomLoadLine = new();
        public PointCollection RoomLoadLine
        {
            get => _roomLoadLine;
            set { _roomLoadLine = value; OnPropertyChanged(); }
        }

        private PointCollection _roomLoadArea = new();
        public PointCollection RoomLoadArea
        {
            get => _roomLoadArea;
            set { _roomLoadArea = value; OnPropertyChanged(); }
        }

        private string _roomLoadPercentText = "0%";
        public string RoomLoadPercentText
        {
            get => _roomLoadPercentText;
            set { _roomLoadPercentText = value; OnPropertyChanged(); }
        }

        private string _roomLoadRangeText = "Диапазон: 0–0%";
        public string RoomLoadRangeText
        {
            get => _roomLoadRangeText;
            set { _roomLoadRangeText = value; OnPropertyChanged(); }
        }

        public RelayCommand RefreshRoomLoadCommand { get; }

        public AdminViewModel()
        {
            RefreshRoomLoadCommand = new RelayCommand(_ =>
            {
                LoadDashboardData();
                LoadRoomLoadChart();
            });

            ScheduleGenerator.ScheduleChanged += OnScheduleChanged;

            LoadDashboardData();
            LoadRoomLoadChart();
        }

        private void OnScheduleChanged()
        {
            LoadDashboardData();
            LoadRoomLoadChart();
        }

        private void LoadDashboardData()
        {
            using var db = new SchoolDbContext();

            TeachersCount = db.Teachers.Count();
            StudentsCount = db.AcademicClasses.Count();

            var hasLessons = db.Lessons.Any();
            if (hasLessons)
            {
                ScheduleStatus = "Готово";
                ScheduleStatusBrush = Brushes.LimeGreen;
            }
            else
            {
                ScheduleStatus = "Не готово";
                ScheduleStatusBrush = Brushes.IndianRed;
            }
        }

        private void LoadRoomLoadChart()
        {
            const double xMax = 800;
            const double yTop = 20;
            const double yBottom = 250;
            const int maxSlotsPerDay = 12;

            using var db = new SchoolDbContext();
            var roomsCount = db.Classrooms.Count();
            if (roomsCount == 0)
            {
                RoomLoadPercentText = "0%";
                RoomLoadRangeText = "Диапазон: 0–0%";
                RoomLoadLine = new PointCollection();
                RoomLoadArea = new PointCollection();
                return;
            }

            var lessonsByDay = db.Lessons
                .Where(l => l.ClassroomId != null && l.DayOfWeek >= 1 && l.DayOfWeek <= 5)
                .AsEnumerable()
                .GroupBy(l => l.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => $"{x.ClassroomId}-{x.LessonIndex}").Distinct().Count());

            var percents = new double[5];
            for (var day = 1; day <= 5; day++)
            {
                var busySlots = lessonsByDay.TryGetValue(day, out var value) ? value : 0;
                var totalSlots = roomsCount * maxSlotsPerDay;
                percents[day - 1] = totalSlots > 0 ? (busySlots / (double)totalSlots) * 100.0 : 0;
            }

            var avg = percents.Average();
            var min = percents.Min();
            var max = percents.Max();

            RoomLoadPercentText = $"{Math.Round(avg, 1)}%";
            RoomLoadRangeText = $"Диапазон: {Math.Round(min, 1)}–{Math.Round(max, 1)}%";

            var scale = BuildAdaptiveScale(min, max);
            var line = new PointCollection();
            var area = new PointCollection { new Point(0, yBottom) };

            var step = xMax / 4.0;
            for (var i = 0; i < 5; i++)
            {
                var x = step * i;
                var normalized = (percents[i] - scale.Min) / (scale.Max - scale.Min);
                normalized = Math.Clamp(normalized, 0, 1);

                var y = yBottom - normalized * (yBottom - yTop);
                line.Add(new Point(x, y));
                area.Add(new Point(x, y));
            }

            area.Add(new Point(xMax, yBottom));

            RoomLoadLine = line;
            RoomLoadArea = area;
        }

        private static (double Min, double Max) BuildAdaptiveScale(double min, double max)
        {
            if (Math.Abs(max - min) < 0.001)
            {
                var center = min;
                return (Math.Max(0, center - 5), Math.Min(100, center + 5));
            }

            var spread = max - min;
            var pad = Math.Max(1.5, spread * 0.25);
            var scaledMin = Math.Max(0, min - pad);
            var scaledMax = Math.Min(100, max + pad);

            if (Math.Abs(scaledMax - scaledMin) < 0.001)
            {
                scaledMax = Math.Min(100, scaledMin + 1);
            }

            return (scaledMin, scaledMax);
        }
    }
}
