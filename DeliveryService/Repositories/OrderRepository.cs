using DeliveryService.Data;
using DeliveryService.Models;
using DeliveryService.Utils;


namespace DeliveryService.Repositories
{
    /// <summary>
    /// Репозиторий для доступа к Заказам в базе данных
    /// </summary>
    public class OrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
            Logger.LogDebug("OrderRepository инициализирован");
        }

        /// <summary>
        /// Получение заказа по id
        /// </summary>
        /// <param name="orderId">ID заказа</param>
        /// <returns>Заказ</returns>
        public async Task<Order?> GetById(int orderId)
        {
            Logger.LogDebug($"Запрос заказа с ID {orderId}");
            
            try
            {
                var result = await _context.Orders.FindAsync(orderId);
                
                if (result == null)
                    Logger.LogWarning($"Заказ с ID {orderId} не найден");
                else
                    Logger.LogDebug($"Заказ {orderId} найден");
                    
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при получении заказа {orderId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Получение заказа по айди курьера
        /// </summary>
        /// <param name="courierId">айди курьера</param>
        /// <returns></returns>
<<<<<<< HEAD
        public async Task<Order?> GetByCourierId(int courierId)
        {
            Logger.LogDebug($"Запрос заказа для курьера {courierId}");
            
            try
            {
                var result = await _context.Orders.FirstOrDefaultAsync(x => x.CourierId == courierId);
                
                if (result == null)
                    Logger.LogDebug($"Активных заказов для курьера {courierId} не найдено");
                else
                    Logger.LogDebug($"Найден заказ {result?.Id} для курьера {courierId}");
                    
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при получении заказа для курьера {courierId}", ex);
                throw;
            }
        }
=======
        public async Task<Order?> GetByCourierId(int courierId) => await _context.Orders.FirstOrDefaultAsync(x => x.CourierId == courierId);
>>>>>>> 644a02dc6c6d2af9fc6593825dd037450435a5db

        /// <summary>
        /// Получение всех заказов
        /// </summary>
        /// <returns>Список заказов</returns>
        public async Task<List<Order>> GetAllAsync()
        {
            Logger.LogDebug("Запрос всех заказов из БД");
            
            try
            {
                var result = await _context.Orders
                    .Include(o => o.Client)
                    .Include(o => o.Courier)
                    .Include(o => o.RoutePoints)
                    .Include(o => o.StatusHistory)
                    .ToListAsync();
                    
                Logger.LogDebug($"Получено {result.Count} заказов");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при получении всех заказов", ex);
                throw;
            }
        }

        /// <summary>
        /// Получение всех незавершённых заказов
        /// </summary>
        /// <returns>Список незавершённых заказов</returns>
        public async Task<List<Order>> GetActive()
        {
            Logger.LogDebug("Запрос активных (незавершенных) заказов");
            
            try
            {
                var result = await _context.Orders
                    .Where(o => o.Status != "Done")
                    .Include(o => o.Client)
                    .Include(o => o.Courier)
                    .Include(o => o.RoutePoints)
                    .Include(o => o.StatusHistory)
                    .ToListAsync();
                    
                Logger.LogDebug($"Получено {result.Count} активных заказов");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при получении активных заказов", ex);
                throw;
            }
        }

        /// <summary>
        /// Добавление заказа
        /// </summary>
        /// <param name="order">Заказ</param>
        public async Task AddAsync(Order order)
        {
            if (order == null)
            {
                Logger.LogWarning("Попытка добавить null-заказ");
                return;
            }

            Logger.LogInfo($"Добавление нового заказа: ID={order.Id}, Клиент={order.ClientId}");
            
            try
            {
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
                Logger.LogInfo($"Заказ {order.Id} успешно добавлен в БД");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при добавлении заказа {order.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Обновление заказа в базе данных
        /// </summary>
        /// <param name="order">Заказ</param>
        public async Task UpdateAsync(Order order)
        {
            if (order == null)
            {
                Logger.LogWarning("Попытка обновить null-заказ");
                return;
            }

            Logger.LogDebug($"Обновление заказа {order.Id}. Статус: {order.Status}");
            
            try
            {
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
                Logger.LogDebug($"Заказ {order.Id} успешно обновлен");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при обновлении заказа {order.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Удаление заказа
        /// </summary>
        /// <param name="order">Заказ</param>
        public async Task DeleteAsync(Order order)
        {
            if (order == null)
            {
                Logger.LogWarning("Попытка удалить null-заказ");
                return;
            }

            Logger.LogInfo($"Удаление заказа {order.Id}");
            
            try
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                Logger.LogInfo($"Заказ {order.Id} успешно удален из БД");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при удалении заказа {order.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Добавление в историю изменения статусов заказов
        /// </summary>
        /// <param name="history">Объект класса OrderStatusHistory</param>
        /// <returns></returns>
        public async Task AddStatusHistoryAsync(OrderStatusHistory history)
        {
            if (history == null)
            {
                Logger.LogWarning("Попытка добавить null-историю статуса");
                return;
            }

            Logger.LogDebug($"Добавление записи в историю статусов: Заказ {history.OrderId}, Статус {history.Status}");
            
            try
            {
                await _context.OrderStatusHistories.AddAsync(history);
                await _context.SaveChangesAsync();
                Logger.LogDebug($"Запись истории статусов для заказа {history.OrderId} успешно добавлена");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при добавлении истории статусов для заказа {history.OrderId}", ex);
                throw;
            }
        }
    }
}