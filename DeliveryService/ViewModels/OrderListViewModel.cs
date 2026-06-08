using DeliveryService.Commands;
using DeliveryService.DTO;
using DeliveryService.Services;
using DeliveryService.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DeliveryService.ViewModels
{
    /// <summary>
    /// Логика взаимодействия пользователя и базы данных с OrderListView
    /// </summary>
    public class OrderListViewModel : BaseViewModel
    {
        private readonly WindowsService _windowsService;
        private readonly OrderService _orderService;
        private readonly CourierService _courierService;

        /// <summary>
        /// Список DTO всех заказов
        /// </summary>
        private List<OrderDTO> _allOrders;
        /// <summary>
        /// Список DTO всех заказов для отображения
        /// </summary>
        private ObservableCollection<OrderDTO> _orders;
        /// <summary>
        /// Список всех курьеров
        /// </summary>
        private List<CourierDTO> _couriers;
        /// <summary>
        /// Фильтр списка заказов
        /// </summary>
        private string _filter;
        /// <summary>
        /// Id выбранного курьера для фильтра 
        /// </summary>
        private int? _selectedCourierId;

        /// <summary>
        /// Количество заказов
        /// </summary>
        private int _totalCount;
        /// <summary>
        /// Количество заказов со статусом "В пути" 
        /// </summary>
        private int _inProcessCount;
        /// <summary>
        /// Количество заказов со статусом "Новый" 
        /// </summary>
        private int _pendingCount;
        /// <summary>
        /// Количество заказов со статусом "Доставлено" 
        /// </summary>
        private int _completedCount;

        /// <summary>
        /// Список DTO всех заказов для отображения
        /// </summary>
        public ObservableCollection<OrderDTO> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }
        /// <summary>
        /// Список всех курьеров
        /// </summary>
        public List<CourierDTO> Couriers
        {
            get => _couriers;
            set => SetProperty(ref _couriers, value);
        }
        /// <summary>
        /// Фильтр списка заказов
        /// </summary>
        public string Filter
        {
            get => _filter;
            set
            {
                if (SetProperty(ref _filter, value))
                    ApplyFilter();
            }
        }
        /// <summary>
        /// Id выбранного курьера для фильтра 
        /// </summary>
        public int? SelectedCourierId
        {
            get => _selectedCourierId;
            set
            {
                if (SetProperty(ref _selectedCourierId, value))
                    ApplyFilter();
            }
        }
        /// <summary>
        /// Количество заказов
        /// </summary>
        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }
        /// <summary>
        /// Количество заказов со статусом "В пути"
        /// </summary>
        public int InProcessCount
        {
            get => _inProcessCount;
            set => SetProperty(ref _inProcessCount, value);
        }
        /// <summary>
        /// Количество заказов со статусом "Новый"
        /// </summary>
        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }
        /// <summary>
        /// Количество заказов со статусом "Доставлено" 
        /// </summary>
        public int CompletedCount
        {
            get => _completedCount;
            set => SetProperty(ref _completedCount, value);
        }


        /// <summary>
        /// Команда загрузки заказов в таблицу
        /// </summary>
        public ICommand LoadOrdersCommand { get; }
        /// <summary>
        /// Команда загрузки данных
        /// </summary>
        public ICommand LoadDataCommand { get; }
        /// <summary>
        /// Комадна удаления заказа
        /// </summary>
        public ICommand RemoveOrderCommand { get; }


        public OrderListViewModel(WindowsService windowsService, OrderService orderService, CourierService courierService)
        {
            _windowsService = windowsService;
            _orderService = orderService;
            _courierService = courierService;

            _allOrders = new List<OrderDTO>();
            Orders = new ObservableCollection<OrderDTO>();
            Couriers = new List<CourierDTO>();

            LoadOrdersCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(LoadOrdersAsync, "Ошибка загрузки"),
                canExecute: () => !IsBusy
            );

            LoadDataCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(LoadDataAsync, "Ошибка загрузки"),
                canExecute: () => !IsBusy
            );

            RemoveOrderCommand = new RelayCommandAsync(
                execute: async (parameter) =>
                {
                    if (parameter is int orderId)
                        await RemoveOrderAsync(orderId);
                },
                canExecute: _ => !IsBusy
            );
            LoadDataCommand.Execute(null);
        }


        /// <summary>
        /// Сбор статистики по списку заказов - полное кол-во, кол-во с определёнными статусами
        /// </summary>
        /// <param name="orders">Список заказов</param>
        private void SetOrderStatistic(ObservableCollection<OrderDTO> orders)
        {
            TotalCount = orders.Count;
            InProcessCount = orders.Count(o => o.Status == "В пути");
            PendingCount = orders.Count(o => o.Status == "Новый");
            CompletedCount = orders.Count(o => o.Status == "Доставлен");
        }
        /// <summary>
        /// Загрузка данных о заказах в список
        /// </summary>
        private async Task LoadOrdersAsync()
        {
            var orders = await _orderService.GetAllAsync();
            var items = new List<OrderDTO>();

            foreach (var order in orders)
            {
                string clientName = order.Client?.Name ?? string.Empty;

                items.Add(new OrderDTO
                {
                    Id = order.Id,
                    ClientName = clientName,
                    Route = $"{order.Address_From} → \n{order.Address_To}",
                    Status = order.Status ?? "—",
                    Price = order.Price,
                    OrderTime = $"{order.Created_At.ToString().Split(' ')[0]}\n{order.Created_At.ToString().Split(' ')[1]}",
                    CourierId = order.CourierId,
                });
            }

            _allOrders = items;
            Orders = new ObservableCollection<OrderDTO>(_allOrders);

            SetOrderStatistic(Orders);
        }
        /// <summary>
        /// Загрузка данных о курьерах
        /// </summary>
        private async Task LoadCouriersAsync()
        {
            var all = await _courierService.GetAllAsync();
            var list = new List<CourierDTO>
            {
                new CourierDTO { Id = 0, Name = "Все курьеры" }
            };

            foreach (var courier in all)
            {
                list.Add(new CourierDTO()
                {
                    Id = courier.Id,
                    Name = courier.Name,
                });
            }
            Couriers = list;
        }
        /// <summary>
        /// Загрузка данных
        /// </summary>
        private async Task LoadDataAsync()
        {
            await LoadOrdersAsync();
            await LoadCouriersAsync();
        }
        /// <summary>
        /// Загрузка списка заказов с учётом фильтрации по имени клиента или ардресам откуда и куда
        /// </summary>
        private void ApplyFilter()
        {
            IEnumerable<OrderDTO> filtered = _allOrders;

            if (!string.IsNullOrWhiteSpace(Filter))
            {
                string search = Filter.Trim().ToLower();
                filtered = filtered.Where(o =>
                    (o.ClientName?.ToLower().Contains(search) == true) ||
                    (o.Route?.ToLower().Contains(search) == true)
                );
            }

            if (SelectedCourierId.HasValue && SelectedCourierId.Value > 0)
                filtered = filtered.Where(o => o.CourierId == SelectedCourierId.Value);

            Orders = new ObservableCollection<OrderDTO>(filtered);
            SetOrderStatistic(Orders);
        }

        private async Task RemoveOrderAsync(object id)
        {
            if (IsBusy) return;
            if (!int.TryParse(id?.ToString(), out int orderId))
                return;

            var success = await _orderService.RemoveOrderAsync(orderId);

            if (success == true)
                await LoadOrdersAsync();
            else
            {
                ErrorMessage = "Не удалось удалить заказ";
                await Task.Delay(3000);
                ErrorMessage = null;
            }
        }
    }
}