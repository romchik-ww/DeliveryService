using DeliveryService.Commands;
using DeliveryService.Models;
using DeliveryService.Services;
using DeliveryService.Utils;


namespace DeliveryService.ViewModels
{
    /// <summary>
    /// Логика взаимодействия пользователя и базы данных с DispatcherView
    /// </summary>
    public class DispatcherViewModel : BaseViewModel
    {
        /// <summary>
        /// Интервал таймера
        /// </summary>
        private const int TIMER_INTERVAL = 30; 

        /// <summary>
        /// Таймер, который перезагружает данные
        /// </summary>
        private DispatcherTimer _refreshTimer;
        /// <summary>
        /// Активен ли таймер
        /// </summary>
        private bool _isTimerActive = false;

        private readonly OrderService _orderService;
        private readonly CourierService _courierService;
        private SimulationService _simulationService;

        public event Action? DisposeRequested;
        
        /// <summary>
        /// Счётчик заказов со статусом "New"
        /// </summary>
        private int _newOrderCount;
        /// <summary>
        /// Счётчик заказов со статусом "InDelivery"
        /// </summary>
        private int _inTransitOrderCount;
        /// <summary>
        /// Счётчик заказов со статусом "Done"
        /// </summary>
        private int _completedOrderCount;
        /// <summary>
        /// Выбранный заказ
        /// </summary>
        private Order _selectedOrder;
        /// <summary>
        /// Выбранный курьер
        /// </summary>
        private Courier _selectedCourier;
       
        public Courier SelectedCourier
        {
            get => _selectedCourier;
            set => SetProperty(ref _selectedCourier, value);
        }

        public SimulationService SimulationService
        {
            get => _simulationService;
            set => SetProperty(ref _simulationService, value);
        }

        /// <summary>
        /// Выбранный Заказ
        /// </summary>
        public Order SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
        }

        /// <summary>
        /// Список активных заказов
        /// </summary>
        public ObservableCollection<Order> ActiveOrders { get; }
        /// <summary>
        /// Список курьеров, которые онлайн
        /// </summary>
        public ObservableCollection<Courier> OnlineCouriers { get; }

        /// <summary>
        /// Список свободных курьеров, которые онлайн
        /// </summary>
        public ObservableCollection<Courier> FreeCouriers { get; }

        /// <summary>
        /// Счётчик заказов со статусом "New"
        /// </summary>
        public int NewOrderCount
        {
            get => _newOrderCount;
            set => SetProperty(ref _newOrderCount, value);
        }
        /// <summary>
        /// Счётчик заказов со статусом "InDelivery"
        /// </summary>
        public int InTransitOrderCount
        {
            get => _inTransitOrderCount;
            set => SetProperty(ref _inTransitOrderCount, value);
        }
        /// <summary>
        /// Счётчик заказов со статусом "Done"
        /// </summary>
        public int CompletedOrderCount
        {
            get => _completedOrderCount;
            set => SetProperty(ref _completedOrderCount, value);
        }

        /// <summary>
        /// Команда загрузки данных
        /// </summary>
        public ICommand LoadDataCommand { get; }

        /// <summary>
        /// Команда назначения курьера на заказ
        /// </summary>
        public ICommand AssignCourierCommand { get; }
        /// <summary>
        /// Команда для выбора заказа
        /// </summary>
        public ICommand SelectOrderCommand { get; }

        /// <summary>
        /// Команда для выбора курьера
        /// </summary>
        public ICommand SelectCourierCommand { get; }

        /// <summary>
        /// Событие, которое вызывается при выборе заказа 
        /// </summary>
        public event Action<Order>? OrderSelected;
        /// <summary>
        /// Событие, которое вызывается при выборе курьера 
        /// </summary>
        public event Action<double,double,double,double,double,double>? CourierSelected;

        public DispatcherViewModel(OrderService orderService, CourierService courierService, SimulationService simulationService)
        {
            _orderService = orderService;
            _courierService = courierService;
            _simulationService = simulationService;
 
            ActiveOrders = new ObservableCollection<Order>();
            OnlineCouriers = new ObservableCollection<Courier>();
            FreeCouriers = new ObservableCollection<Courier>();
            _simulationService.CourierFinal += _simulationService_CourierFinal;

            Logger.LogDebug("DispatcherViewModel инициализирован");

            LoadDataCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(LoadDataAsync, "Ошибка загрузки"),
                canExecute: () => !IsBusy
            );

            AssignCourierCommand = new RelayCommandAsync(async order =>
            {
                if (SelectedCourier == null || order == null)
                {
                    Logger.LogWarning("Попытка назначения курьера: курьер или заказ не выбран");
                    return;
                }
                
                Order ord = (Order)order;
                Logger.LogInfo($"Назначение курьера {SelectedCourier.Id} на заказ {ord.Id}");
                
                bool success = await _courierService.AssignCourierToOrderAsync(SelectedCourier.Id, ord.Id);
                if (success)
                {
                    Logger.LogInfo($"Курьер {SelectedCourier.Id} успешно назначен на заказ {ord.Id}");
                    await LoadDataAsync();
                }
                else
                {
                    Logger.LogWarning($"Не удалось назначить курьера {SelectedCourier.Id} на заказ {ord.Id}");
                }
            });

            SelectOrderCommand = new RelayCommandAsync(async order =>
            {
                if (order == null) return;
                SelectedOrder = (Order)order;
                Logger.LogDebug($"Выбран заказ {SelectedOrder.Id} со статусом {SelectedOrder.Status}");
                OrderSelected?.Invoke((Order)order);
            });

            SelectCourierCommand = new RelayCommandAsync(async parameter =>
            {
                if (parameter is not Courier courier)
                {
                    Logger.LogWarning("SelectCourierCommand: параметр не является курьером");
                    return;
                }
                
                SelectedCourier = courier;
                Logger.LogDebug($"Выбран курьер {SelectedCourier.Id}: {SelectedCourier.Name}, статус: {(SelectedCourier.IsActive ? "онлайн" : "офлайн")}");

                SelectedOrder = await _orderService.FindOrderByCourierIdAsync(courier.Id);

                if (SelectedOrder == null)
                {
                    Logger.LogDebug($"У курьера {courier.Id} нет активных заказов");
                    return;
                }
                
                if (SelectedCourier == null) return;

                Logger.LogDebug($"Курьер {courier.Id} назначен на заказ {SelectedOrder.Id}");
                CourierSelected?.Invoke(SelectedOrder.Lat_From, SelectedOrder.Lon_From, SelectedOrder.Lat_To, SelectedOrder.Lon_To, SelectedCourier.Current_Lat, SelectedCourier.Current_Lon);
            });
        }

        /// <summary>
        /// Функция, срабатывающая при достижении курьером финальной точки
        /// </summary>
        private void _simulationService_CourierFinal()
        {
            Logger.LogInfo("Курьер достиг финальной точки, обновление данных");
            LoadDataCommand.Execute(null);
        }

        /// <summary>
        /// Загрузка данных о заказах
        /// </summary>
        private async Task LoadOrdersAsync()
        {
            Logger.LogDebug("Начало загрузки заказов");
            
            try
            {
                var allOrders = await _orderService.GetAllAsync();

                if (allOrders != null && allOrders.Any())
                {
                    var activeOrders = allOrders.Where(o => o.Status != "Доставлен").OrderByDescending(o => o.Created_At).ToList();

                    ActiveOrders.Clear();
                    foreach (var order in activeOrders) 
                        ActiveOrders.Add(order);

                    NewOrderCount = activeOrders.Count(o => o.Status == "Новый");
                    InTransitOrderCount = activeOrders.Count(o => o.Status == "В пути");
                    CompletedOrderCount = allOrders.Count(o => o.Status == "Доставлен");

                    Logger.LogDebug($"Заказы загружены: Всего={allOrders.Count}, Активных={activeOrders.Count}, Новых={NewOrderCount}, В пути={InTransitOrderCount}, Доставлено={CompletedOrderCount}");
                }
                else
                {
                    ActiveOrders.Clear();
                    NewOrderCount = 0;
                    InTransitOrderCount = 0;
                    CompletedOrderCount = 0;
                    Logger.LogDebug("Заказы не найдены");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при загрузке заказов", ex);
                throw;
            }
        }

        /// <summary>
        /// Загрузка свободных курьеров
        /// </summary>
        /// <returns></returns>
        private async Task LoadFreeCouriersAsync()
        {
            Logger.LogDebug("Начало загрузки свободных курьеров");
            
            try
            {
                var freeCouriers = await _courierService.GetFreeCouriersAsync();
                if (freeCouriers == null)
                {
                    Logger.LogDebug("Свободные курьеры не найдены");
                    return;
                }

                var onlineCouriers = freeCouriers.Where(c => c.IsActive).ToList();

                FreeCouriers.Clear();
                foreach (var courier in onlineCouriers)
                    FreeCouriers.Add(courier);

                Logger.LogDebug($"Загружено {FreeCouriers.Count} свободных курьеров");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при загрузке свободных курьеров", ex);
                throw;
            }
        }

        /// <summary>
        /// Назначение курьера на заказ
        /// </summary>
        /// <param name="courierId">Айди курьера</param>
        /// <param name="orderId">Айди заказа</param>
        /// <returns></returns>
        public async Task AssignCourier(int courierId, int orderId)
        {
            Logger.LogInfo($"Назначение курьера {courierId} на заказ {orderId}");
            
            try
            {
                await _courierService.AssignCourierToOrderAsync(courierId, orderId);
                Logger.LogInfo($"Курьер {courierId} успешно назначен на заказ {orderId}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при назначении курьера {courierId} на заказ {orderId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Загрузка данных об курьерах
        /// </summary>
        private async Task LoadCouriersAsync()
        {
            Logger.LogDebug("Начало загрузки курьеров");
            
            try
            {
                var allCouriers = await _courierService.GetAllAsync();
                if (allCouriers == null)
                {
                    Logger.LogDebug("Курьеры не найдены");
                    return;
                }

                var onlineCouriers = allCouriers.Where(c => c.IsActive).ToList();

                OnlineCouriers.Clear();
                foreach (var courier in onlineCouriers)
                    OnlineCouriers.Add(courier);

                Logger.LogDebug($"Загружено {OnlineCouriers.Count} онлайн-курьеров из {allCouriers.Count} всего");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при загрузке курьеров", ex);
                throw;
            }
        }

        /// <summary>
        /// Загрузка данных
        /// </summary>
        private async Task LoadDataAsync()
        {
            Logger.LogDebug("Полная загрузка данных диспетчера");
            
            try
            {
                await LoadOrdersAsync();
                await LoadCouriersAsync();
                await LoadFreeCouriersAsync();
                Logger.LogDebug("Данные диспетчера успешно обновлены");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при полной загрузке данных диспетчера", ex);
                throw;
            }
        }

        /// <summary>
        /// Старт таймера
        /// </summary>
        public void TimerStart()
        {
            Logger.LogInfo($"Запуск таймера обновления с интервалом {TIMER_INTERVAL} секунд");
            
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(TIMER_INTERVAL);
            _refreshTimer.Tick += OnTimerTick;
            _refreshTimer.Start();
            _isTimerActive = true;
            
            Debug.WriteLine("Timer start");
        }

        /// <summary>
        /// Остановка таймера
        /// </summary>
        public void TimerStop()
        {
            if (_isTimerActive)
            {
                Logger.LogInfo("Остановка таймера обновления");
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnTimerTick;
                _isTimerActive = false;
                Debug.WriteLine("Timer stop");
            }
            else
            {
                Logger.LogDebug("Попытка остановить неактивный таймер");
            }
        }

        /// <summary>
        /// Логика таймера
        /// </summary>
        private void OnTimerTick(object? sender, EventArgs e)
        {
            Logger.LogDebug("Срабатывание таймера обновления данных");
            LoadDataCommand.Execute(null);
        }

        public async Task SaveCoords(double v1, double v2)
        {
            if (SelectedCourier == null)
            {
                Logger.LogWarning("Попытка сохранить координаты без выбранного курьера");
                return;
            }
            
            Logger.LogDebug($"Сохранение координат курьера {SelectedCourier.Id}: ({v1}, {v2})");
            
            try
            {
                SelectedCourier.Current_Lat = v1;
                SelectedCourier.Current_Lon = v2;
                await _courierService.Update(SelectedCourier);
                Logger.LogDebug($"Координаты курьера {SelectedCourier.Id} успешно сохранены");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при сохранении координат курьера {SelectedCourier?.Id}", ex);
                throw;
            }
        }
    }
}