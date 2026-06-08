using DeliveryService.Models;
using DeliveryService.Repositories;
using DeliveryService.Utils;


namespace DeliveryService.Services
{
    /// <summary>
    /// Сервис, работающий с заказами
    /// </summary>
    public class OrderService
    {
        private readonly OrderRepository _orderRepository;
        private readonly ClientRepository _clientRepository;


        public async Task CreateOrderAsync(int orderId)
        {
            // Теперь можно использовать Logger напрямую
            Logger.LogInfo($"Начинаем создание заказа {orderId}");
            
            try
            {
                // Ваша логика
                Logger.LogDebug($"Детали заказа: ID={orderId}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка при создании заказа {orderId}", ex);
                throw;
            }
        }
        
        public OrderService(OrderRepository orderRepo, ClientRepository clientRepo)
        {
            _orderRepository = orderRepo;
            _clientRepository = clientRepo;
        }
        /// <summary>
        /// Получение всех заказов
        /// </summary>
        /// <returns>Возвращает список всех заказов</returns>
        public async Task<List<Order>> GetAllAsync() => await _orderRepository.GetAllAsync();

        /// <summary>
        /// Получение всех активных заказов
        /// </summary>
        /// <returns>Список активных заказов</returns>
        public async Task<List<Order>> GetActiveOrdersAsync() => await _orderRepository.GetActive();

        public async Task<Order?> GetByIdAsync(int id) => await _orderRepository.GetById(id);

        public async Task<bool> CreateOrderAsync(Client client, Order order)
        {
            if (client == null) return false;
            if (string.IsNullOrEmpty(order.Address_From) || string.IsNullOrEmpty(order.Address_To)) return false;

            if (client.Id == 0)
                await _clientRepository.AddAsync(client);
            order.ClientId = client.Id;

            order.Status = "Новый";
            order.Created_At = DateTime.UtcNow;

            await _orderRepository.AddAsync(order);
            return true;
        }

        /// <summary>
        /// Меняет статус заказа
        /// </summary>
        /// <param name="orderId">Айди заказа</param>
        /// <param name="newStatus">Новый статус</param>
        /// <param name="feedback">Отзыв, при закрытии заказа</param>
        /// <returns></returns>
        public async Task<bool> ChangeStatusAsync(int orderId, string newStatus, string? feedback = null)
        {
            var order = await _orderRepository.GetById(orderId);
            if (order == null) return false;

            order.Status = newStatus;
            await _orderRepository.UpdateAsync(order);

            await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = orderId,
                Status = newStatus,
                Changed_At = DateTime.UtcNow,
                FeedBack = feedback
            });
            return true;
        }
        /// <summary>
        /// Отменяет заказ
        /// </summary>
        /// <param name="orderId">Айди заказа</param>
        /// <param name="feedback">Отзыв, при желании</param>
        /// <returns>true-если заказ отменен,false-если заказ не найден</returns>
        public async Task<bool?> CancelOrderAsync(int orderId, string? feedback = null)
        {
            var order = await _orderRepository.GetById(orderId);
            if (order == null) return false;


            order.Status = "Отменён";
            await _orderRepository.UpdateAsync(order);
            await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory
            {
                OrderId = orderId,
                Status = "Отменён",
                Changed_At = DateTime.UtcNow,
                FeedBack = feedback
            });
            return true;
        }

        /// <summary>
        /// Получеие заказа по айди курьера
        /// </summary>
        /// <param name="courierId">айди курьера</param>
        /// <returns></returns>
        public async Task<Order?> FindOrderByCourierIdAsync(int courierId) => await _orderRepository.GetByCourierId(courierId);

        /// <summary>
        /// Удаление заказа
        /// </summary>
        /// <param name="orderId">Айди заказа</param>
        /// <returns></returns>
        public async Task<bool> RemoveOrderAsync(int orderId)
        {
            Order? order = await _orderRepository.GetById(orderId);
            if(order == null) return false;
            await _orderRepository.DeleteAsync(order);
            return true;
        }

        /// <summary>
        /// Обновление данных в заказе
        /// </summary>
        /// <param name="order">Объект заказа</param>
        /// <returns></returns>
        public async Task Update(Order order) => await _orderRepository.UpdateAsync(order);

        /// <summary>
        /// Удаление заказа
        /// </summary>
        /// <param name="order">Объект заказа</param>
        /// <returns>true-удачно, false-неудачно</returns>
        public async Task<bool> DeleteAsync(Order order)
        {
            if(order  == null) return false;
            await _orderRepository.DeleteAsync(order);
            return true;
        }
        /// <summary>
        /// Добавление в историю
        /// </summary>
        /// <param name="order">Объект заказа</param>
        /// <param name="feedback">Отзыв</param>
        /// <param name="status">Новый статус</param>
        /// <returns></returns>
        public async Task AddToHistory(Order order, string status, string? feedback = null)
        {
            if (order == null) return;
            await _orderRepository.AddStatusHistoryAsync(new OrderStatusHistory
            {
                Id = order.Id,
                Order = order,
                Changed_At = DateTime.UtcNow,
                FeedBack = feedback,
                Status = status,
                OrderId = order.Id

            });

        }


    }
}
