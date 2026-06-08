using DeliveryService.Commands;
using DeliveryService.Models;
using DeliveryService.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace DeliveryService.ViewModels
{
    public class RegistrationViewModel : BaseViewModel
    {

        public event Action? RegistrationSuccess;


        /// <summary>
        /// Имя
        /// </summary>
        private string _name;
        /// <summary>
        /// Пароль 
        /// </summary>
        private string _password;
        /// <summary>
        /// Почта
        /// </summary>
        private string _email;
        /// <summary>
        /// Номер курьера, "очищенный" от всего, кроме цифр
        /// </summary>
        private string _cleanedPhoneNumber;
        /// <summary>
        /// Номер телефона
        /// </summary>
        private string _phoneNumber;
        /// <summary>
        /// Номер телефона
        /// </summary>
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }
        /// <summary>
        /// Почта
        /// </summary>
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }
        /// <summary>
        /// Имя
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        /// <summary>
        /// Пароль 
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        /// <summary>
        /// Сервис клиента
        /// </summary>
        private ClientService _clientService;
        /// <summary>
        /// Команда для кнопки регистрации
        /// </summary>
        public ICommand RegistrationCommand { get; }
        /// <summary>
        /// Сервис для открытия окон
        /// </summary>

        private readonly WindowsService _windowService;

        public RegistrationViewModel(ClientService clientService, WindowsService windowService)
        {
            System.Diagnostics.Debug.WriteLine("RegistrationViewModel создан");
            _clientService = clientService;
            _windowService = windowService;

            RegistrationCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(SaveUserAsync, "Ошибка создания пользователя"),
                canExecute: () => !IsBusy
            );
        }
        /// <summary>
        /// Создание юзера и сохранение в базу данных
        /// </summary>
        private async Task SaveUserAsync()
        {

            ErrorMessage = null;

            if (!ValidateProperty())
                return;

            if (!ValidatePhoneNumber())
                return;


            if (!long.TryParse(PhoneNumber, out long phoneNumber))
            {
                ErrorMessage = "Номер телефона должен содержать только цифры";

                return;
            }

            Client Client = new Client
            {
                Name = this.Name,
                Phone = phoneNumber.ToString(),
                Created_At = DateTime.UtcNow,
                Email = this.Email,
                Password = this.Password,
                Role = "user"
            };

            try
            {
                bool success = await _clientService.AddClientAsync(Client);

                if (success) RegistrationSuccess?.Invoke();
                else
                    ErrorMessage = "Не удалось выполнить команду";
            }
            catch (DbUpdateException ex)
            {

                ErrorMessage = $"Ошибка БД: {ex.InnerException?.Message ?? ex.Message}";
                System.Diagnostics.Debug.WriteLine($"DbUpdateException: {ex.InnerException?.Message}");

            }
        }
        /// <summary>
        /// Проверка валидации
        /// </summary>
        /// <returns>true, если все поля валидны, иначе false</returns>
        private bool ValidateProperty()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Введите имя";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Введите пароль";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Email) || !IsValidEmail(Email))
            {
                ErrorMessage = "Введите почту";
                return false;
            }
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ErrorMessage = "Введите номер телефона";
                return false;
            }

            return true;
        }

        private bool ValidatePhoneNumber()
        {
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                ErrorMessage = "Введите номер телефона";
                _cleanedPhoneNumber = null;
                return false;
            }

            string cleaned = new string(PhoneNumber.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(cleaned))
            {
                ErrorMessage = "Номер телефона должен содержать хотя бы одну цифру";
                _cleanedPhoneNumber = null;
                return false;
            }
            if (cleaned.Length < 10 || cleaned.Length > 11)
            {
                ErrorMessage = "Номер телефона должен содержать 11 цифр";
                _cleanedPhoneNumber = null;
                return false;
            }

            _cleanedPhoneNumber = cleaned;
            return true;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

    }
}
