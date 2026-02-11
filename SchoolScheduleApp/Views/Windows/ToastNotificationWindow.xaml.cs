using System;
using System.Windows;
using System.Windows.Media;

namespace SchoolScheduleApp.Views.Windows
{
    public partial class ToastNotificationWindow : Window
    {
        public ToastNotificationWindow(string title, string message, bool isError)
        {
            InitializeComponent();
            TitleText.Text = title;
            MessageText.Text = message;

            if (isError)
            {
                RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 107, 107));
            }
        }
    }
}
