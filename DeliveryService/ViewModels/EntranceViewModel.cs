using DeliveryService.Commands;
using DeliveryService.Models;
using DeliveryService.Services;
using System.Windows.Input;

namespace DeliveryService.ViewModels
{
    public class EntranceViewModel : BaseViewModel
    {
        private readonly SessionService _sessionService;
        /// <summary>
        /// Событие, нужное для закрывания окна
        /// </summary>
        public event Action? CloseRequested;

        /// <summary>
        /// Имя
        /// </summary>
        private string _name;
        /// <summary>
        /// Пароль 
        /// </summary>
        private string _password;
        /// <summary>
        /// Юзер, который входит в систему
        /// </summary>
        private Client? _client;
        /// <summary>
        /// Роль юзера
        /// </summary>
        private string _role;

        /// <summary>
        /// Роль юзера
        /// </summary>
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        /// <summary>
        /// Юзер, который входит в систему
        /// </summary>
        public Client? Client
        {
            get => _client;
            set => SetProperty(ref _client, value);
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
        /// Клиент сервис
        /// </summary>
        private readonly ClientService _clientService;
        /// <summary>
        /// Сервис для открытия окон
        /// </summary>
        private readonly WindowsService _windowService;

        /// <summary>
        /// Команда для авторизации
        /// </summary>
        public ICommand LoginCommand { get; }


        public EntranceViewModel(ClientService clientService, WindowsService windowService, SessionService sessionService)
        {

            _clientService = clientService;
            _windowService = windowService;
            _sessionService = sessionService;

            LoginCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(CheckAndAuthClient, "Ошибка аунтефикации"),
                canExecute: () => !IsBusy
            );


        }

        /// <summary>
        /// Проверка и аунтефикация пользователя
        /// </summary>
        /// <returns></returns>
        private async Task CheckAndAuthClient()
        {

            Client = await _clientService.GetClientByName(Name);
            _sessionService.CurrentClient = Client;

            if (Client == null)
            {
                ErrorMessage = "Такого пользователя не существует";
                return;
            }
            Role = Client.Role;
            switch (Role)
            {
                case "admin":
                    _windowService.OpenMainWindow();
                    CloseRequested?.Invoke();
                    break;
                case "user":
                    _windowService.OpenMenu();
                    CloseRequested?.Invoke();
                    break;
            }



        }
    }
}
