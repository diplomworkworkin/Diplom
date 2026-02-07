using SchoolScheduleApp.ViewModels;
using System.Windows.Controls;

namespace SchoolScheduleApp.Views.Pages
{
    public partial class SchedulePage : Page
    {
        public SchedulePage()
        {
            InitializeComponent();
            DataContext = new ScheduleViewModel();
        }
    }
}
