using DeliveryService.Models;
using DeliveryService.Repositories;
using DeliveryService.Utils;


namespace DeliveryService.Services
{
    /// <summary>
    /// Сервис, работающий с Корзиной
    /// </summary>
    public class BasketService
    {
        private readonly BasketRepository _basketRepository;
        private readonly OrderRepository _orderRepository;
        private readonly FoodRepository _foodRepository;

        public BasketService(BasketRepository basketRepository, OrderRepository orderRepository, FoodRepository foodRepository)
        {
            _basketRepository = basketRepository;
            _orderRepository = orderRepository;
            _foodRepository = foodRepository;
            Logger.LogDebug("BasketService инициализирован");
        }

        /// <summary>
        /// Получение объекта корзины по id
        /// </summary>
        /// <param name="basketId">ID объекта корзины</param>
        /// <returns>Объект корзины</returns>
        public async Task<Basket?> GetById(int basketId)
        {
            Logger.LogDebug($"Запрос объекта корзины с ID {basketId}");
            var result = await _basketRepository.GetById(basketId);
            
            if (result == null)
                Logger.LogWarning($"Объект корзины с ID {basketId} не найден");
            else
                Logger.LogDebug($"Объект корзины {basketId} найден");
                
            return result;
        }

        /// <summary>
        /// Полуение списка корзины и полной суммы по пользователю
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Список корзины и полная сумма</returns>
        public async Task<(List<Basket> userBasket, decimal totalPrice)> GetUserBasketAsync(int userId)
        {
            Logger.LogDebug($"Запрос корзины пользователя {userId}");
            
            try
            {
                var basket = await _basketRepository.GetUserBasketAsync(userId);
                decimal totalPrice = basket.Sum(b => b.Price);
                
                Logger.LogDebug($"Пользователь {userId}: найдено {basket.Count} объектов в корзине, общая сумма: {totalPrice:C}");
                return (basket, totalPrice);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при получении корзины пользователя {userId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Полуение списка корзины, исключая те объекты, которые уже привязаны к заказам и полной суммы по пользователю
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <returns>Список объектов корзины, c не привязанными к заказам и полная сумма</returns>
        public async Task<(List<Basket> userBasket, decimal totalPrice)> GetUserActiveBasketAsync(int userId)
        {
            Logger.LogDebug($"Запрос активной корзины пользователя {userId}");
            
            try
            {
                var basket = await _basketRepository.GetUserActiveBasketAsync(userId);
                decimal totalPrice = basket.Sum(b => b.Price);
                
                Logger.LogDebug($"Пользователь {userId}: найдено {basket.Count} активных объектов в корзине, общая сумма: {totalPrice:C}");
                return (basket, totalPrice);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при получении активной корзины пользователя {userId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Создание нового объекта корзины
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="foodId">ID еды</param>
        /// <param name="quantity">Количество</param>
        /// <returns>Прошла ли операция</returns>
        public async Task<bool> AddNewBasketItemAsync(int userId, int foodId, int quantity)
        {
            Logger.LogInfo($"Добавление нового объекта в корзину: пользователь {userId}, товар {foodId}, количество {quantity}");
            
            try
            {
                var food = await _foodRepository.GetById(foodId);
                if (food == null)
                {
                    Logger.LogWarning($"Товар с ID {foodId} не найден при добавлении в корзину пользователя {userId}");
                    return false;
                }

                if (quantity <= 0)
                {
                    Logger.LogWarning($"Попытка добавить некорректное количество ({quantity}) товара {foodId} для пользователя {userId}");
                    return false;
                }

                decimal price = food.Price * quantity;
                Basket item = new Basket
                {
                    UserId = userId,
                    FoodId = foodId,
                    Quantity = quantity,
                    Price = price
                };

                await _basketRepository.AddAsync(item);
                
                Logger.LogInfo($"Товар {food.Name} (ID: {foodId}) добавлен в корзину пользователя {userId}. Количество: {quantity}, цена: {price:C}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при добавлении товара {foodId} в корзину пользователя {userId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Создание нового объекта корзины или обновление уже существующего
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="foodId">ID еды</param>
        /// <param name="quantity">Количество</param>
        /// <returns>Прошла ли операция</returns>
        public async Task<bool> AddOrUpdateBasketItemAsync(int userId, int foodId, int quantity)
        {
            Logger.LogInfo($"Добавление или обновление объекта в корзине: пользователь {userId}, товар {foodId}, количество {quantity}");
            
            try
            {
                var food = await _foodRepository.GetById(foodId);
                if (food == null)
                {
                    Logger.LogWarning($"Товар с ID {foodId} не найден при операции добавления/обновления для пользователя {userId}");
                    return false;
                }

                if (quantity <= 0)
                {
                    Logger.LogWarning($"Попытка добавить/обновить с некорректным количеством ({quantity}) товара {foodId} для пользователя {userId}");
                    return false;
                }

                var item = await _basketRepository.GetActiveByUserAndFoodId(userId, foodId);
                if (item != null)
                {
                    int oldQuantity = item.Quantity;
                    item.Quantity += quantity;
                    item.Price = food.Price * item.Quantity;
                    await _basketRepository.UpdateAsync(item);
                    
                    Logger.LogInfo($"Обновлен товар {food.Name} в корзине пользователя {userId}. Количество: {oldQuantity} -> {item.Quantity}, цена: {item.Price:C}");
                }
                else
                {
                    decimal price = food.Price * quantity;
                    Basket newItem = new Basket 
                    { 
                        UserId = userId,
                        FoodId = foodId,
                        Quantity = quantity,
                        Price = price
                    };

                    await _basketRepository.AddAsync(newItem);
                    
                    Logger.LogInfo($"Добавлен новый товар {food.Name} в корзину пользователя {userId}. Количество: {quantity}, цена: {price:C}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при добавлении/обновлении товара {foodId} в корзине пользователя {userId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Удаление объекта из корзины
        /// </summary>
        /// <param name="basketId">ID объекта корзины</param>
        /// <returns>Прошла ли операция</returns>
        public async Task<bool> RemoveItemAsync(int basketId)
        {
            Logger.LogInfo($"Удаление объекта корзины с ID {basketId}");
            
            try
            {
                var item = await _basketRepository.GetById(basketId);
                if (item == null)
                {
                    Logger.LogWarning($"Объект корзины с ID {basketId} не найден для удаления");
                    return false;
                }

                await _basketRepository.DeleteAsync(item);
                
                Logger.LogInfo($"Объект корзины {basketId} успешно удален (пользователь {item.UserId}, товар {item.FoodId})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при удалении объекта корзины {basketId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Очистка всей всех объектов корзины по пользователю
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        public async Task ClearUserBasketAsync(int userId)
        {
            Logger.LogInfo($"Очистка всей корзины пользователя {userId}");
            
            try
            {
                var basketBeforeClear = await _basketRepository.GetUserBasketAsync(userId);
                int itemsCount = basketBeforeClear.Count;
                decimal totalValue = basketBeforeClear.Sum(b => b.Price);
                
                await _basketRepository.ClearUserBasketAsync(userId);
                
                Logger.LogInfo($"Корзина пользователя {userId} очищена. Удалено {itemsCount} объектов на сумму {totalValue:C}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при очистке корзины пользователя {userId}", ex);
                throw;
            }
        }
    }
}