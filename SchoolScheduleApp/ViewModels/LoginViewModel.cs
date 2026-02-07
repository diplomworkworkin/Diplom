using SchoolSchedule.Context;
using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SchoolScheduleApp.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); } // Сообщение об ошибке (красным)
        }

        // Команда для кнопки
        public RelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(ExecuteLogin);
        }

        private void ExecuteLogin(object parameter)
        {
            // Передача пароля из PasswordBox (MVVM хак для безопасности)
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Введите логин и пароль!";
                return;
            }

            // Проверка в Базе Данных
            using (var db = new SchoolDbContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == Username && u.Password == password);

                if (user != null)
                {
                    ErrorMessage = "";

                    // Успешный вход!
                    // Открываем главное окно (пока заглушка, сделаем на след. шаге)
                    MessageBox.Show($"Добро пожаловать, {user.FullName}!", "Успех");

                    // TODO: Здесь будет открытие MainWindow
                    var adminWindow = new AdminWindow();
                    adminWindow.Show();

                    // Закрываем текущее окно (через хак, так как VM не должна знать об окнах)
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.DataContext == this)
                        {
                             window.Close(); // Раскомментируем, когда будет главное окно
                        }
                    }
                }
                else
                {
                    ErrorMessage = "Неверный логин или пароль";
                }
            }
        }
    }
}
