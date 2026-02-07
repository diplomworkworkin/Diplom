using SchoolSchedule.Context;
using SchoolSchedule.Entites;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SchoolScheduleApp.Views.Windows
{
    /// <summary>
    /// Логика взаимодействия для ClassEditWindow.xaml
    /// </summary>
    public partial class ClassEditWindow : Window
    {
        public AcademicClass AcademicClass { get; private set; }
        public ObservableCollection<Teacher> Teachers { get; private set; } = new();

        public ClassEditWindow(AcademicClass? cls)
        {
            InitializeComponent();

            AcademicClass = cls ?? new AcademicClass { Shift = 1, StudentCount = 1 };

            LoadTeachersForCurator();
            DataContext = this;
        }

        private void LoadTeachersForCurator()
        {
            using var db = new SchoolDbContext();

            // чтобы учитель не мог быть куратором 2 классов:
            // берем тех, кто уже куратор, кроме текущего класса (если редактируем)
            int currentClassId = AcademicClass.Id;

            var busyTeacherIds = db.AcademicClasses
                .Where(c => c.CuratorTeacherId != null && c.Id != currentClassId)
                .Select(c => c.CuratorTeacherId!.Value)
                .ToList();

            var teachers = db.Teachers
                .Where(t => !busyTeacherIds.Contains(t.Id))
                .OrderBy(t => t.FullName)
                .ToList();

            Teachers = new ObservableCollection<Teacher>(teachers);
        }

        private void BtnClearCurator_Click(object sender, RoutedEventArgs e)
        {
            AcademicClass.CuratorTeacherId = null;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 1) Приводим название к нормальному виду
            AcademicClass.Name = (AcademicClass.Name ?? "")
                .Trim()
                .ToUpper()
                .Replace("–", "-"); // если кто-то вставил длинное тире

            // 2) Проверка названия: 1..11 + "-" + русская буква
            // Примеры: 1-А, 5-Б, 11-В
            string pattern = @"^(?:[1-9]|1[0-1])-[А-ЯЁ]$";
            if (!Regex.IsMatch(AcademicClass.Name, pattern))
            {
                MessageBox.Show("Название должно быть в формате: 1-А, 2-Б, ... 11-В (цифра + дефис + русская буква).", "Ошибка");
                return;
            }

            // 3) Кол-во учеников: 1..30
            if (AcademicClass.StudentCount < 1 || AcademicClass.StudentCount > 30)
            {
                MessageBox.Show("Количество учеников должно быть от 1 до 30.", "Ошибка");
                return;
            }

            // 4) Смена: 1 или 2
            if (AcademicClass.Shift != 1 && AcademicClass.Shift != 2)
            {
                MessageBox.Show("Смена должна быть 1 или 2.", "Ошибка");
                return;
            }

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }


        private void TbStudentCount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Только цифры
            if (!Regex.IsMatch(e.Text, @"^\d+$"))
            {
                e.Handled = true;
                return;
            }

            // Не даем ввести > 30
            var tb = (TextBox)sender;
            string newText = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                    .Insert(tb.SelectionStart, e.Text);

            if (int.TryParse(newText, out int value))
                e.Handled = value > 30;
        }

        private void TbStudentCount_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string text = e.DataObject.GetData(DataFormats.Text)?.ToString() ?? "";
            if (!Regex.IsMatch(text, @"^\d+$"))
            {
                e.CancelCommand();
                return;
            }

            if (int.TryParse(text, out int value) && value > 30)
                e.CancelCommand();
        }
    }
}

