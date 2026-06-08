using DeliveryService.Commands;
using DeliveryService.Services;
using System.Windows.Input;
using System.Windows.Threading;


namespace DeliveryService.ViewModels
{
    public class OrderAcceptViewModel : BaseViewModel
    {
        private readonly WindowsService _windowService;
        private readonly SimulationService _simulationService;
        private readonly SessionService _sessionService;
        private readonly CourierService _courierService;

        public event Func<Task>? CourierAssigned;

        public event Action? ClosedRequested;

        /// <summary>
        /// Таймер, который перезагружает данные
        /// </summary>
        private DispatcherTimer _refreshTimer;
        /// <summary>
        /// Интервал таймера
        /// </summary>
        public double TIMER_INTERVAL = 3;
        /// <summary>
        /// Статус заказа
        /// </summary>
        private string _status;

        private string _statusMessage;
        private string _orderNumber;
        private string _addressFrom;
        private string _addressTo;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }


        public string OrderNumber
        {
            get => _orderNumber;
            set => SetProperty(ref _orderNumber, value);
        }


        public string AddressFrom
        {
            get => _addressFrom;
            set => SetProperty(ref _addressFrom, value);
        }

        public string AddressTo
        {
            get => _addressTo;
            set => SetProperty(ref _addressTo, value);
        }

        /// <summary>
        /// Статус заказа
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ICommand ReturnCommand { get; set; }

        public OrderAcceptViewModel(WindowsService windowService, SessionService sessionService, CourierService courierService, SimulationService simulationService)
        {
            _windowService = windowService;
            _sessionService = sessionService;
            _courierService = courierService;
            _simulationService = simulationService;
            ReturnCommand = new RelayCommand(
                execute: () => TryRunTaskAsync(ReturnToMenu, "Ошибка возврата в меню"),
                canExecute: () => !IsBusy);

            Status = "Ищем ближайщего курера..";
            ClosedRequested += () => _windowService.CloseWindow(this);
            _simulationService.CourierFinal += ChangeStatus;
            InitializeTimer();


        }

        private void ChangeStatus()
        {
            StatusMessage = "Ваш заказ доставлен!";
        }

        private async Task ReturnToMenu()
        {
            _windowService.OpenMenu();
            ClosedRequested.Invoke();
        }

        /// <summary>
        /// Инициализация таймера
        /// </summary>
        private async void InitializeTimer()
        {
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(TIMER_INTERVAL);
            _refreshTimer.Tick += OnTimerTick;
            _refreshTimer.Start();
        }

        private async void OnTimerTick(object? sender, EventArgs e)
        {

            if (_sessionService?.CurrentOrder?.Courier == null)
            {

                bool success = await _courierService.AssignFreeCourierToOrderAsync(_sessionService?.CurrentOrder);
                if (success == false) return;
                Status = $"Ваш заказ принял: {_sessionService?.CurrentOrder?.Courier?.Name}";
                StatusMessage = "Курьер уже в пути";
                OrderNumber = $"№ ORD-{_sessionService?.CurrentOrder?.Id}";
                AddressFrom = _sessionService?.CurrentOrder?.Address_From;
                AddressTo = _sessionService?.CurrentOrder?.Address_To;
                CourierAssigned?.Invoke();
                _refreshTimer.Stop();

            }
        }
    }
}
