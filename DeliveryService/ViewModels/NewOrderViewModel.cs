using DeliveryService.Commands;
using DeliveryService.Models;
using DeliveryService.Services;
using DeliveryService.Utils;


namespace DeliveryService.ViewModels
{
    /// <summary>
    /// Логика взаимодействия пользователя и базы данных с NewOrderView
    /// </summary>
    public class NewOrderViewModel : BaseViewModel
    {
        private readonly SessionService _sessionService;
        private readonly OrderService _orderService;
        private readonly ClientService _clientService;
        private readonly BasketService _basketService;
        private readonly WindowsService _windowService;
        private readonly CourierService _courierService;

        /// <summary>
        /// Имя клиента
        /// </summary>
        private string _clientName;
        /// <summary>
        /// Номер клиента
        /// </summary>
        private string _clientPhone;
        /// <summary>
        /// Номер клиента, "очищенный" от всего, кроме цифр
        /// </summary>
        private string _cleanedPhoneNumber;

        private List<Basket> _clientBasket;

        /// <summary>
        /// Адрес отправки
        /// </summary>
        private string _addressFrom;
        /// <summary>
        /// Ширина адреса отправки
        /// </summary>
        private double _latFrom;
        /// <summary>
        /// Долгота адреса отправки
        /// </summary>
        private double _lonFrom;

        /// <summary>
        /// Адрес доставки
        /// </summary>
        private string _addressTo;
        /// <summary>
        /// Ширина адреса доставки
        /// </summary>
        private double _latTo;
        /// <summary>
        /// Долгота адреса доставки
        /// </summary>
        private double _lonTo;

        /// <summary>
        /// Цена
        /// </summary>
        private decimal _price;

        /// <summary>
        /// Переменная, необходимая для переключения режима откуда/куда
        /// </summary>
        private bool _isFromMode;

        /// <summary>
        /// Переменная, необходимая для переключения режима откуда/куда
        /// </summary>
        public bool IsFromMode
        {
            get => _isFromMode;
            set => SetProperty(ref _isFromMode, value);
        }

        /// <summary>
        /// Имя клиента
        /// </summary>
        public string ClientName
        {
            get => _clientName;
            set => SetProperty(ref _clientName, value);
        }
        /// <summary>
        /// Номер клиента
        /// </summary>
        public string ClientPhone
        {
            get => _clientPhone;
            set => SetProperty(ref _clientPhone, value);
        }
        /// <summary>
        /// Адрес отправки
        /// </summary>
        public string AddressFrom
        {
            get => _addressFrom;
            set => SetProperty(ref _addressFrom, value);
        }
        /// <summary>
        /// Ширина адреса отправки
        /// </summary>
        public double LatFrom
        {
            get => _latFrom;
            set => SetProperty(ref _latFrom, value);
        }
        /// <summary>
        /// Долгота адреса отправки
        /// </summary>
        public double LonFrom
        {
            get => _lonFrom;
            set => SetProperty(ref _lonFrom, value);
        }
        /// <summary>
        /// Адрес доставки
        /// </summary>
        public string AddressTo
        {
            get => _addressTo;
            set => SetProperty(ref _addressTo, value);
        }
        /// <summary>
        /// Ширина адреса доставки
        /// </summary>
        public double LatTo
        {
            get => _latTo;
            set => SetProperty(ref _latTo, value);
        }
        /// <summary>
        /// Долгота адреса доставки
        /// </summary>
        public double LonTo
        {
            get => _lonTo;
            set => SetProperty(ref _lonTo, value);
        }
        /// <summary>
        /// Цена
        /// </summary>
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        /// <summary>
        /// Команда создания и сохранения заказа 
        /// </summary>
        public ICommand SaveCommand { get; }
        /// <summary>
        /// Команда закрытия окна
        /// </summary>
        public ICommand CloseCommand { get; }
        /// <summary>
        /// Команда загрузки пользователя
        /// </summary>
        public ICommand LoadUserCommand { get; }

<<<<<<< HEAD
        public NewOrderViewModel(SessionService sessionService, 
=======

        public NewOrderViewModel(SessionService sessionService,
>>>>>>> 644a02dc6c6d2af9fc6593825dd037450435a5db
            OrderService orderService, ClientService clientService, BasketService basketService, WindowsService windowService, CourierService courierService)
        {
            _sessionService = sessionService;
            _orderService = orderService;
            _clientService = clientService;
            _basketService = basketService;
            _windowService = windowService;
            _courierService = courierService;
            _clientBasket = new List<Basket>();

            Logger.LogDebug("NewOrderViewModel инициализирован");

            SaveCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(SaveOrderAsync, "Ошибка создания заказа"),
                canExecute: () => !IsBusy
            );

            CloseCommand = new RelayCommand(_ => CloseWindow(false));

            LoadUserCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(LoadUser, "Ошибка загрузки пользователя"),
                canExecute: () => !IsBusy
            );

            LoadUserCommand.Execute(null);
            IsFromMode = true;
            
            Logger.LogDebug($"NewOrderViewModel настроен для пользователя {_sessionService.CurrentClient?.Id}");
        }

        /// <summary>
        /// Загрузка имени и телефона пользователя
        /// </summary>
        private async Task LoadUser()
        {
            Logger.LogInfo($"Загрузка данных пользователя {_sessionService.CurrentClient?.Id}");
            
            try
            {
                ClientName = _sessionService.CurrentClient.Name;
                ClientPhone = _sessionService.CurrentClient.Phone.ToString();

                var (userBasket, totalPrice) = await _basketService.GetUserActiveBasketAsync(_sessionService.CurrentClient.Id);
                _clientBasket = userBasket;
                Price = totalPrice;
                
                Logger.LogInfo($"Пользователь {_sessionService.CurrentClient.Id} загружен. Имя: {ClientName}, корзина: {_clientBasket.Count} позиций, сумма: {Price:C}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при загрузке пользователя {_sessionService.CurrentClient?.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Проверка валидации ClientName, AddressFrom, AddressTo, и Price
        /// </summary>
        /// <returns>true, если все поля валидны, иначе false</returns>
        private bool ValidateProperty()
        {
            if (string.IsNullOrWhiteSpace(ClientName))
            {
                ErrorMessage = "Введите имя клиента";
                Logger.LogWarning("Ошибка валидации: не указано имя клиента");
                return false;
            }
            if (string.IsNullOrWhiteSpace(AddressFrom))
            {
                ErrorMessage = "Укажите адрес отправления";
                Logger.LogWarning("Ошибка валидации: не указан адрес отправления");
                return false;
            }
            if (string.IsNullOrWhiteSpace(AddressTo))
            {
                ErrorMessage = "Укажите адрес доставки";
                Logger.LogWarning("Ошибка валидации: не указан адрес доставки");
                return false;
            }
            if (Price <= 0)
            {
                ErrorMessage = "Цена должна быть больше нуля";
                Logger.LogWarning($"Ошибка валидации: некорректная цена {Price}");
                return false;
            }

            Logger.LogDebug("Валидация полей заказа пройдена успешно");
            return true;
        }

        /// <summary>
        /// Проверка валидации ClientPhone и "очищение" от не-цифр
        /// </summary>
        /// <returns>true, если валиден, иначе false</returns>
        private bool ValidatePhoneNumber()
        {
            if (string.IsNullOrWhiteSpace(ClientPhone))
            {
                ErrorMessage = "Введите номер телефона";
                _cleanedPhoneNumber = null;
                Logger.LogWarning("Ошибка валидации: не указан номер телефона");
                return false;
            }

            string cleaned = new string(ClientPhone.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(cleaned))
            {
                ErrorMessage = "Номер телефона должен содержать хотя бы одну цифру";
                _cleanedPhoneNumber = null;
                Logger.LogWarning($"Ошибка валидации: номер телефона '{ClientPhone}' не содержит цифр");
                return false;
            }
            if (cleaned.Length < 10 || cleaned.Length > 11)
            {
                ErrorMessage = "Номер телефона должен содержать 10–11 цифр";
                _cleanedPhoneNumber = null;
                Logger.LogWarning($"Ошибка валидации: длина номера {cleaned.Length} цифр (требуется 10-11)");
                return false;
            }

            _cleanedPhoneNumber = cleaned;
            Logger.LogDebug($"Номер телефона валиден: {_cleanedPhoneNumber}");
            return true;
        }

        private async Task SaveOrderAsync()
        {
            ErrorMessage = null;
            Logger.LogInfo($"Начало создания заказа для пользователя {_sessionService.CurrentClient?.Id}");

            if (!ValidateProperty())
            {
                Logger.LogWarning("Создание заказа отменено: не пройдена валидация полей");
                return;
            }

            if (_clientBasket == null || _clientBasket.Count == 0)
            {
                ErrorMessage = "Корзина пуста. Невозможно оформить заказ.";
                Logger.LogWarning($"Создание заказа отменено: корзина пользователя {_sessionService.CurrentClient?.Id} пуста");
                return;
            }

            Logger.LogDebug($"Корзина пользователя {_sessionService.CurrentClient?.Id} содержит {_clientBasket.Count} позиций на сумму {Price:C}");

            try
            {
                Client? client = await _clientService.GetClientById(_sessionService.CurrentClient.Id);
                if (client == null)
                {
                    if (!int.TryParse(ClientPhone, out int phoneNumber))
                    {
                        ErrorMessage = "Номер телефона должен содержать только цифры";
                        Logger.LogWarning($"Ошибка: клиент {_sessionService.CurrentClient.Id} не найден, номер телефона не распознан: {ClientPhone}");
                        return;
                    }
                }

                var order = new Order
                {
                    ClientId = _sessionService.CurrentClient.Id,
                    Address_From = AddressFrom,
                    Lat_From = LatFrom,
                    Lon_From = LonFrom,
                    Address_To = AddressTo,
                    Lat_To = LatTo,
                    Lon_To = LonTo,
                    Price = Price,             
                    Status = "Новый",
                    Created_At = DateTime.UtcNow,
                    BasketId = _clientBasket[0].Id, 
                };
                
                Logger.LogDebug($"Создаем заказ: от {AddressFrom} до {AddressTo}, цена {Price:C}");
                
                bool success = await _orderService.CreateOrderAsync(client, order);
                if (!success)
                {
                    ErrorMessage = "Не удалось создать заказ";
                    Logger.LogError($"Не удалось создать заказ для пользователя {_sessionService.CurrentClient.Id}", new Exception("CreateOrderAsync вернул false"));
                    return;
                }
                
                _sessionService.CurrentOrder = order;
                Logger.LogInfo($"Заказ {order.Id} успешно создан для пользователя {_sessionService.CurrentClient.Id}");

                foreach (var item in _clientBasket)
                {
                    await _basketService.RemoveItemAsync(item.Id);
                    Logger.LogDebug($"Удален товар из корзины: BasketId={item.Id}");
                }
                
                Logger.LogInfo($"Корзина пользователя {_sessionService.CurrentClient.Id} очищена после создания заказа");
                
                _windowService.OpenOrderAccept();
                CloseWindow(true);
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                Logger.LogError($"Ошибка при сохранении заказа для пользователя {_sessionService.CurrentClient?.Id}", ex);
                ErrorMessage = $"Ошибка при создании заказа: {ex.Message}";
                throw;
=======
                ClientId = _sessionService.CurrentClient.Id,
                Address_From = AddressFrom,
                Lat_From = LatFrom,
                Lon_From = LonFrom,
                Address_To = AddressTo,
                Lat_To = LatTo,
                Lon_To = LonTo,
                Price = Price,
                Status = "Новый",
                Created_At = DateTime.UtcNow,
                BasketId = _clientBasket[0].Id,
            };
            bool success = await _orderService.CreateOrderAsync(client, order);
            if (!success)
            {
                ErrorMessage = "Не удалось создать заказ";
                return;
>>>>>>> 644a02dc6c6d2af9fc6593825dd037450435a5db
            }
        }

        /// <summary>
        /// Устанавливает выбранный адрес в поля для ввода
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <param name="address"></param>
        public void SetSelectedAddress(double lat, double lon, string address)
        {
            if (IsFromMode)
            {
                LatFrom = lat;
                LonFrom = lon;
                AddressFrom = address;
                Logger.LogDebug($"Установлен адрес отправки: {address} ({lat}, {lon})");
                IsFromMode = !IsFromMode;
            }
            else
            {
                LatTo = lat;
                LonTo = lon;
                AddressTo = address;
                Logger.LogDebug($"Установлен адрес доставки: {address} ({lat}, {lon})");
                IsFromMode = !IsFromMode;
            }
        }

        /// <summary>
        /// Закрытие окна
        /// </summary>
        /// <param name="result">Результат работы окна</param>
        private void CloseWindow(bool result)
        {
            Logger.LogDebug($"Закрытие окна NewOrderView с результатом {result}");
            
            var window = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);

            if (window != null)
            {
                window.DialogResult = result;
                window.Close();
                Logger.LogDebug("Окно NewOrderView закрыто");
            }
            else
            {
                Logger.LogWarning("Окно NewOrderView не найдено для закрытия");
            }
        }
    }
}