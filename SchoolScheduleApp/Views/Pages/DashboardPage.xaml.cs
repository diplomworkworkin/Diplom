using SchoolScheduleApp.Views;
using System.Windows;
using System.Windows.Controls;

namespace SchoolScheduleApp.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        private void BtnOpenWorkloads_Click(object sender, RoutedEventArgs e)
        {
            // Переходим на страницу нагрузки (Workloads) через родительское окно
            var wnd = Window.GetWindow(this) as AdminWindow;
            wnd?.NavigateTo(new WorkloadsPage(), "Учебная нагрузка");
        }
    }
}
