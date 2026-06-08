using DeliveryService.Commands;
using DeliveryService.Services;
using DeliveryService.Utils;
using System;
using System.Windows.Input;

namespace DeliveryService.ViewModels
{
    public class AuthorizationViewModel : BaseViewModel
    {
        /// <summary>
        /// Текущее view
        /// </summary>
        private object _currentView;

        private readonly EntranceViewModel _entranceViewModel;
        private readonly RegistrationViewModel _registrationViewModel;
        private readonly WindowsService _windowsService;

        /// <summary>
        /// Текущее view
        /// </summary>
        public object CurrentView
        {
            get => _currentView;
            set{ SetProperty(ref _currentView, value);}
        }

        /// <summary>
        /// Команда для входа в аккаунт
        /// </summary>
        public ICommand LoginCommand { get; }
        /// <summary>
        /// Команда для регистрации аккаунта
        /// </summary>
        public ICommand RegCommand { get; }
        /// <summary>
        /// Команда для закрытия окна
        /// </summary>
        public ICommand CloseCommand { get; }

        public AuthorizationViewModel(
            EntranceViewModel EntViewModel,
            RegistrationViewModel RegistrationViewModel,
            WindowsService windowService
            )
        {
            _entranceViewModel = EntViewModel;
            _registrationViewModel = RegistrationViewModel;
            _windowsService = windowService;
            
            Logger.LogDebug("AuthorizationViewModel инициализирован");
            
            _entranceViewModel.CloseRequested += () => 
            {
                Logger.LogInfo("Запрос на закрытие окна от EntranceViewModel");
                _windowsService.CloseWindow(this);
            };
            
            _registrationViewModel.RegistrationSuccess += () => 
            {
                Logger.LogInfo("Успешная регистрация, переключение на EntranceViewModel");
                CurrentView = _entranceViewModel;
            };

            CurrentView = _entranceViewModel;
            Logger.LogDebug("Начальное представление: EntranceViewModel");

            LoginCommand = new RelayCommand(() => 
            {
                Logger.LogDebug("Переключение на представление входа (LoginCommand)");
                CurrentView = _entranceViewModel;
            });
            
            RegCommand = new RelayCommand(() => 
            {
                Logger.LogDebug("Переключение на представление регистрации (RegCommand)");
                CurrentView = _registrationViewModel;
            });
            
            CloseCommand = new RelayCommand(() => 
            {
                Logger.LogInfo("Закрытие окна авторизации по команде CloseCommand");
                _windowsService.CloseWindow(this);
            });
        }
    }
}