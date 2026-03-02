using SchoolScheduleApp.Data.Entites;
using SchoolScheduleApp.Core;
using SchoolScheduleApp.Views;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SchoolScheduleApp.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AppSettings _settings;
        private readonly ApiClient _apiClient;
        private string _username;
        private bool _rememberMe;
        private string _initialPassword = string.Empty;
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

        public bool RememberMe
        {
            get => _rememberMe;
            set { _rememberMe = value; OnPropertyChanged(); }
        }

        public string InitialPassword
        {
            get => _initialPassword;
            set { _initialPassword = value; OnPropertyChanged(); }
        }

        // Команда для кнопки
        public RelayCommand LoginCommand { get; }

        public LoginViewModel()
        {
            _settings = AppSettingsService.Load();
            _apiClient = new ApiClient();

            if (_settings.RememberMe)
            {
                Username = _settings.SavedUsername;
                InitialPassword = _settings.SavedPassword;
                RememberMe = true;
            }

            LoginCommand = new RelayCommand(async (param) => await ExecuteLogin(param));
        }

        private async Task ExecuteLogin(object parameter)
        {
            // Передача пароля из PasswordBox (MVVM хак для безопасности)
            var passwordBox = parameter as PasswordBox;
            var password = passwordBox?.Password;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Введите логин и пароль!";
                return;
            }

            try
            {
                // Fetch all users and filter locally for now. Ideally, a dedicated login endpoint would be better.
                List<User> users = await _apiClient.GetUsersAsync();
                var user = users.FirstOrDefault(u => u.Username == Username && u.Password == password);

                if (user == null)
                {
                    ErrorMessage = "Неверный логин или пароль";
                    return;
                }

                ErrorMessage = "";

                _settings.RememberMe = RememberMe;
                _settings.SavedUsername = RememberMe ? Username : string.Empty;
                _settings.SavedPassword = RememberMe ? password : string.Empty;
                AppSettingsService.Save(_settings);

                UserSession.SetUser(user);
                AppLogger.LogInfo($"Вход в систему: {user.Username} ({user.Role})");

                Window? nextWindow = user.Role switch
                {
                    UserRole.Admin => new AdminWindow(),
                    UserRole.Teacher => new TeacherWindow(),
                    UserRole.Student => new StudentWindow(),
                    _ => null
                };

                if (nextWindow == null)
                {
                    ErrorMessage = "Роль пользователя не поддерживается.";
                    return;
                }

                Application.Current.MainWindow = nextWindow;
                ToastService.Show($"Добро пожаловать, {user.FullName}!", "Успех");
                nextWindow.Show();

                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext == this)
                        window.Close();
                }
            }
            catch (HttpRequestException ex)
            {
                AppLogger.LogError("Ошибка подключения к API.", ex);
                ErrorMessage = "Произошла ошибка подключения к серверу API. Проверьте его доступность.";
            }
            catch (Exception ex)
            {
                AppLogger.LogError("Ошибка авторизации.", ex);
                ErrorMessage = "Произошла ошибка входа. Попробуйте еще раз.";
            }
        }
    }
}
