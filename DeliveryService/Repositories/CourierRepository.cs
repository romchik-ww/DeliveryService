using DeliveryService.Data;
using DeliveryService.Models;
using Microsoft.EntityFrameworkCore;
using DeliveryService.Utils;


namespace DeliveryService.Repositories
{
    /// <summary>
    /// Репозиторий для доступа к Курьерам в базе данных
    /// </summary>
    public class CourierRepository
    {
        private readonly AppDbContext _context;

        public CourierRepository(AppDbContext context)
        {
            _context = context;
            Logger.LogDebug("CourierRepository инициализирован");
        }

        /// <summary>
        /// Получение курьера по id
        /// </summary>
        /// <param name="courierId">ID курьера</param>
        /// <returns>Курьер</returns>
        public async Task<Courier?> GetById(int courierId)
        {
            Logger.LogDebug($"Запрос курьера с ID {courierId}");
            
            try
            {
                var result = await _context.Couriers.FindAsync(courierId);
                
                if (result == null)
                    Logger.LogWarning($"Курьер с ID {courierId} не найден");
                else
                    Logger.LogDebug($"Курьер {courierId} найден: {result.Name}");
                    
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при получении курьера {courierId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Получение всех курьеров
        /// </summary>
        /// <returns>Список курьеров</returns>
        public async Task<List<Courier>> GetAllAsync()
        {
            Logger.LogDebug("Запрос всех курьеров из БД");
            
            try
            {
                var result = await _context.Couriers
                    .Include(o => o.Orders)
                    .ToListAsync();
                    
                Logger.LogDebug($"Получено {result.Count} курьеров");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при получении всех курьеров", ex);
                throw;
            }
        }

        /// <summary>
        /// Получение всех активных курьеров
        /// </summary>
        /// <returns>Список курьеров, у которых IsActive равен True</returns>
        public async Task<List<Courier>> GetActive()
        {
            Logger.LogDebug("Запрос активных курьеров");
            
            try
            {
                var result = await _context.Couriers
                    .Where(c => c.IsActive)
                    .Include(o => o.Orders)
                    .ToListAsync();
                    
                Logger.LogDebug($"Найдено {result.Count} активных курьеров");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при получении активных курьеров", ex);
                throw;
            }
        }

        /// <summary>
        /// Получение всех свободных от заказов курьеров
        /// </summary>
        /// <returns>Список курьеров, у которых нет активных заказов</returns>
        public async Task<List<Courier>> GetFreeCouriers()
        {
            Logger.LogDebug("Запрос свободных курьеров (без активных заказов)");
            
            try
            {
                var result = await _context.Couriers
                    .Where(c => c.IsActive)
                    .Where(c => !c.Orders.Any(o => o.Status == "В пути"))
                    .ToListAsync();
                    
                Logger.LogDebug($"Найдено {result.Count} свободных курьеров");
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при получении свободных курьеров", ex);
                throw;
            }
        }

        /// <summary>
        /// Добавление курьера
        /// </summary>
        /// <param name="courier">Курьер</param>
        public async Task AddAsync(Courier courier)
        {
            if (courier == null)
            {
                Logger.LogWarning("Попытка добавить null-курьера");
                return;
            }

            Logger.LogInfo($"Добавление нового курьера: {courier.Name}, Email: {courier.Email ?? "не указан"}");
            
            try
            {
                await _context.Couriers.AddAsync(courier);
                await _context.SaveChangesAsync();
                Logger.LogInfo($"Курьер {courier.Name} (ID: {courier.Id}) успешно добавлен в БД");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при добавлении курьера {courier.Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// Обновление курьера в базе данных
        /// </summary>
        /// <param name="courier">Курьер</param>
        public async Task UpdateAsync(Courier courier)
        {
            if (courier == null)
            {
                Logger.LogWarning("Попытка обновить null-курьера");
                return;
            }

            Logger.LogDebug($"Обновление курьера {courier.Id}: {courier.Name}, IsActive={courier.IsActive}");
            
            try
            {
                _context.Couriers.Update(courier);
                await _context.SaveChangesAsync();
                Logger.LogDebug($"Курьер {courier.Id} успешно обновлен");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при обновлении курьера {courier.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Переключение статуса IsActive 
        /// </summary>
        /// <param name="courierId">ID курьера</param>
        public async Task ToggleOnline(int courierId)
        {
            Logger.LogInfo($"Переключение онлайн-статуса для курьера {courierId}");
            
            try
            {
                var courier = await _context.Couriers.FindAsync(courierId);

                if (courier != null)
                {
                    bool oldStatus = courier.IsActive;
                    courier.IsActive = !courier.IsActive;
                    await _context.SaveChangesAsync();
                    
                    Logger.LogInfo($"Курьер {courierId}: статус изменен с {oldStatus} на {courier.IsActive}");
                }
                else
                {
                    Logger.LogWarning($"Курьер с ID {courierId} не найден для переключения статуса");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при переключении статуса курьера {courierId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Удаление курьера
        /// </summary>
        /// <param name="courier">Курьер</param>
        public async Task DeleteAsync(Courier courier)
        {
            if (courier == null)
            {
                Logger.LogWarning("Попытка удалить null-курьера");
                return;
            }

            Logger.LogInfo($"Удаление курьера {courier.Id}: {courier.Name}");
            
            try
            {
                _context.Couriers.Remove(courier);
                await _context.SaveChangesAsync();
                Logger.LogInfo($"Курьер {courier.Id} успешно удален из БД");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при удалении курьера {courier.Id}", ex);
                throw;
            }
        }
    }
}