using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SchoolScheduleApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Логика перетаскивания (оставь как было)
            this.MouseDown += (sender, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // НОВЫЙ МЕТОД: Скрытие надписи "Пароль"
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (UserPasswordBox.Password.Length > 0)
            {
                PasswordPlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                PasswordPlaceholder.Visibility = Visibility.Visible;
            }
        }
    }
}
