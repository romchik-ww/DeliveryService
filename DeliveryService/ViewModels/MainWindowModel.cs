using DeliveryService.Commands;
using DeliveryService.Services;
using System.Windows.Input;


namespace DeliveryService.ViewModels
{
    /// <summary>
    /// Логика взаимодействия пользователя с MainWindow
    /// </summary>
    public class MainWindowModel : BaseViewModel
    {
        private readonly WindowsService _windowsService;
        private readonly SessionService _sessionService;

        /// <summary>
        /// Команда открытия DispatcherView
        /// </summary>
        public ICommand OpenDispatcherCommand { get; }
        /// <summary>
        /// Команда открытия OrderListView
        /// </summary>
        public ICommand OpenOrderListCommand { get; }
        /// <summary>
        /// Команда открытия ListCouriersView
        /// </summary>
        public ICommand OpenListCouriersCommand { get; }
        /// <summary>
        /// Команда открытия NewOrderView
        /// </summary>
        public ICommand OpenNewOrderCommand { get; }
        /// <summary>
        /// Команда открытия RegistrationCouriers
        /// </summary>
        public ICommand OpenRegistrationCourierCommand { get; }
        /// <summary>
        /// Команда закрытия всех открытых немодальных окон
        /// </summary>
        public ICommand CloseWindowsCommand { get; }
        public ICommand LogoutCommand { get; }

        private object _currentView;
        private readonly DispatcherViewModel _dispatcherVm;
        private readonly OrderListViewModel _ordersVm;
        private readonly ListCouriersViewModel _couriersVm;

        public event Action CloseRequested;
        public object CurrentView
        {
            get => _currentView;
            set
            {
                switch (value)
                {
                    case OrderListViewModel o: _dispatcherVm.TimerStop(); o.LoadOrdersCommand.Execute(null); break;
                    case DispatcherViewModel d: _dispatcherVm.TimerStart(); d.LoadDataCommand.Execute(null); break;
                    case ListCouriersViewModel d: _dispatcherVm.TimerStop(); d.LoadCouriersCommand.Execute(null); break;
                }
                SetProperty(ref _currentView, value);
            }
        }


        public MainWindowModel(
            OrderListViewModel ordersVm,
            DispatcherViewModel dispatcherVm,
            ListCouriersViewModel couriersVm,
            WindowsService windowsService,
            SessionService sessionService)
        {

            _dispatcherVm = dispatcherVm;
            _ordersVm = ordersVm;
            _couriersVm = couriersVm;

            CurrentView = ordersVm;
            _windowsService = windowsService;
            _sessionService = sessionService;


            OpenDispatcherCommand = new RelayCommand(() =>
                CurrentView = _dispatcherVm


                );
            OpenOrderListCommand = new RelayCommand(() =>
                CurrentView = _ordersVm

                );
            OpenListCouriersCommand = new RelayCommand(() =>
                CurrentView = _couriersVm


                );

            CloseRequested += () => _windowsService.CloseWindow(this);

            OpenNewOrderCommand = new RelayCommand(() =>
                _windowsService.OpenMenu()
            );

            OpenRegistrationCourierCommand = new RelayCommand(() =>
            {
                _windowsService.OpenRegistrationCourier();
            });


            //CloseWindowsCommand = new RelayCommand(_windowsService.CloseWindows);

            LogoutCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(Logout, "Ошибка выхода из аккаунта"),
                canExecute: () => !IsBusy
                );
        }

        /// <summary>
        /// Выход из аккаунта
        /// </summary>
        /// <returns></returns>
        private async Task Logout()
        {
            _sessionService.CurrentClient = null;
            _sessionService.CurrentOrder = null;
            _windowsService.OpenEntrance();
            this.CloseRequested?.Invoke();
        }
    }
}