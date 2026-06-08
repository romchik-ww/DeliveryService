using DeliveryService.Models;
using DeliveryService.Repositories;
using DeliveryService.Utils;  


namespace DeliveryService.Services
{
    /// <summary>
    /// Сервис, работающий с Курьерами
    /// </summary>
    public class CourierService
    {
        private readonly OrderRepository _orderRepository;
        private readonly CourierRepository _courierRepository;

        public CourierService(OrderRepository orderRepo, CourierRepository courirerRepo)
        {
            _orderRepository = orderRepo;
            _courierRepository = courirerRepo;
            Logger.LogDebug("CourierService инициализирован");
        }

        /// <summary>
        /// Получение всех курьеров
        /// </summary>
        /// <returns>Список курьеров</returns>
        public async Task<List<Courier>> GetAllAsync()
        {
            Logger.LogDebug("Запрос на получение всех курьеров");
            var result = await _courierRepository.GetAllAsync();
            Logger.LogDebug($"Получено {result?.Count ?? 0} курьеров");
            return result;
        }

        /// <summary>
        /// Получение всех активных курьеров
        /// </summary>
        /// <returns>Список активных курьеров</returns>
        public async Task<List<Courier>> GetActiveCouriersAsync()
        {
            Logger.LogDebug("Запрос на получение активных курьеров");
            var result = await _courierRepository.GetActive();
            Logger.LogDebug($"Получено {result?.Count ?? 0} активных курьеров");
            return result;
        }

        /// <summary>
        /// Получение всех свободных от заказов курьеров
        /// </summary>
        /// <returns>Список свободных от заказов курьеров</returns>
        public async Task<List<Courier>> GetFreeCouriersAsync()
        {
            Logger.LogDebug("Запрос на получение свободных курьеров");
            var result = await _courierRepository.GetFreeCouriers();
            Logger.LogDebug($"Получено {result?.Count ?? 0} свободных курьеров");
            return result;
        }

        /// <summary>
        /// Добавление курьера в базу данных
        /// </summary>
        /// <param name="courier">Курьер</param>
        /// <returns>Прошла ли операция</returns>
        public async Task<bool> AddCourierAsync(Courier courier)
        {
            if (courier == null)
            {
                Logger.LogWarning("Попытка добавить null-курьера");
                return false;
            }

            Logger.LogInfo($"Добавление нового курьера: {courier.Name ?? "Unknown"}");
            
            try
            {
                courier.IsActive = true;
                courier.Created_At = DateTime.UtcNow;
                courier.Current_Lat = 0.0;
                courier.Current_Lon = 0.0;

                await _courierRepository.AddAsync(courier);
                Logger.LogInfo($"Курьер успешно добавлен. ID: {courier.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при добавлении курьера {courier.Name}", ex);
                return false;
            }
        }

        /// <summary>
        /// Назначение курьера на заказ
        /// </summary>
        /// <param name="courierId">ID курьера</param>
        /// <param name="orderId">ID заказа</param>
        /// <returns>Прошла ли операция назначения</returns>
        public async Task<bool> AssignCourierToOrderAsync(int courierId, int orderId)
        {
            Logger.LogInfo($"Попытка назначения курьера {courierId} на заказ {orderId}");
            
            try
            {
                Courier? courier = await _courierRepository.GetById(courierId);
                if (courier == null)
                {
                    Logger.LogWarning($"Курьер с ID {courierId} не найден");
                    return false;
                }

                Order? order = await _orderRepository.GetById(orderId);
                if (order == null)
                {
                    Logger.LogWarning($"Заказ с ID {orderId} не найден");
                    return false;
                }

                if (order.CourierId != null || order.CourierId == courierId)
                {
                    Logger.LogWarning($"Заказ {orderId} уже назначен на курьера {order.CourierId}");
                    return false;
                }

                order.CourierId = courierId;
                order.Status = "В пути";
                courier.Current_Lat = order.Lat_From;
                courier.Current_Lon = order.Lon_From;
                await _orderRepository.UpdateAsync(order);

                Logger.LogInfo($"Курьер {courierId} успешно назначен на заказ {orderId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при назначении курьера {courierId} на заказ {orderId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Назначение свободного курьера на заказ
        /// </summary>
        /// <param name="order">Заказ</param>
        /// <returns>Прошла ли операция назначения</returns>
        public async Task<bool> AssignFreeCourierToOrderAsync(Order order)
        {
            if (order == null)
            {
                Logger.LogWarning("Попытка назначить курьера на null-заказ");
                return false;
            }

            Logger.LogInfo($"Поиск свободного курьера для заказа {order.Id}");
            
            try
            {
                var list = await _courierRepository.GetFreeCouriers();
                if (list.Count == 0)
                {
                    Logger.LogWarning($"Нет свободных курьеров для заказа {order.Id}");
                    order.CourierId = null;
                    order.Status = "Новый";
                    await _orderRepository.UpdateAsync(order);
                    return false;
                }

                Courier? courier = list[new Random().Next(list.Count)];
                if (courier == null)
                {
                    Logger.LogError($"Ошибка: выбранный курьер оказался null для заказа {order.Id}");
                    return false;
                }

                if (order.CourierId != null)
                {
                    Logger.LogWarning($"Заказ {order.Id} уже имеет назначенного курьера {order.CourierId}");
                    return false;
                }

                order.CourierId = courier.Id;
                order.Status = "В пути";
                courier.Current_Lat = order.Lat_From;
                courier.Current_Lon = order.Lon_From;
                await _orderRepository.UpdateAsync(order);

                Logger.LogInfo($"Свободный курьер {courier.Id} назначен на заказ {order.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при назначении свободного курьера на заказ {order.Id}", ex);
                return false;
            }
        }

        /// <summary>
        /// Изменение статуса онлайн/офлайн курьера
        /// </summary>
        /// <param name="courierId">ID курьера</param>
        /// <returns>Прошла ли операция</returns>
        public async Task<bool> ToggleCourierOnlineAsync(int courierId)
        {
            Logger.LogInfo($"Переключение статуса онлайн/офлайн для курьера {courierId}");
            
            try
            {
                var courier = await _courierRepository.GetById(courierId);
                if (courier == null)
                {
                    Logger.LogWarning($"Курьер с ID {courierId} не найден для переключения статуса");
                    return false;
                }

                await _courierRepository.ToggleOnline(courierId);
                Logger.LogInfo($"Статус курьера {courierId} успешно переключен");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при переключении статуса курьера {courierId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Удаление курьера
        /// </summary>
        /// <param name="courierId">ID курьера</param>
        /// <returns></returns>
        public async Task<bool> RemoveCourierAsync(int courierId)
        {
            Logger.LogInfo($"Попытка удаления курьера с ID {courierId}");
            
            try
            {
                var courier = await _courierRepository.GetById(courierId);
                if (courier == null)
                {
                    Logger.LogWarning($"Курьер с ID {courierId} не найден для удаления");
                    return false;
                }

                await _courierRepository.DeleteAsync(courier);
                Logger.LogInfo($"Курьер {courierId} успешно удален");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при удалении курьера {courierId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Получение курьера по айди
        /// </summary>
        /// <param name="id">ID курьера</param>
        /// <returns>Курьер или null</returns>
        public async Task<Courier?> GetById(int id)
        {
            Logger.LogDebug($"Запрос курьера с ID {id}");
            var result = await _courierRepository.GetById(id);
            
            if (result == null)
                Logger.LogWarning($"Курьер с ID {id} не найден");
            else
                Logger.LogDebug($"Курьер {id} найден: {result.Name}");
                
            return result;
        }

        /// <summary>
        /// Обновление курьера
        /// </summary>
        /// <param name="courier">Курьер</param>
        public async Task Update(Courier courier)
        {
            if (courier == null)
            {
                Logger.LogWarning("Попытка обновить null-курьера");
                return;
            }

            Logger.LogDebug($"Обновление курьера {courier.Id}");
            
            try
            {
                await _courierRepository.UpdateAsync(courier);
                Logger.LogDebug($"Курьер {courier.Id} успешно обновлен");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при обновлении курьера {courier.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Функция, сохраняющая координаты курьера
        /// </summary>
        /// <param name="Lat">Широта</param>
        /// <param name="Lon">Долгота</param>
        /// <param name="courier">Курьер</param>
        public async Task SaveCourierCoords(double Lat, double Lon, Courier courier)
        {
            if (courier == null)
            {
                Logger.LogWarning("Попытка сохранить координаты для null-курьера");
                return;
            }

            Logger.LogDebug($"Сохранение координат курьера {courier.Id}: ({Lat}, {Lon})");
            
            try
            {
                courier.Current_Lat = Lat;
                courier.Current_Lon = Lon;
                await Update(courier);
                Logger.LogDebug($"Координаты курьера {courier.Id} успешно сохранены");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при сохранении координат курьера {courier.Id}", ex);
                throw;
            }
        }
    }
}