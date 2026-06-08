using DeliveryService.Models;


namespace DeliveryService.Services
{
    /// <summary>
    /// Класc-сервис для хранения текущего пользователя
    /// </summary>
    public class SessionService
    {
        /// <summary>
        /// Текущий заказ
        /// </summary>
        private Order? _currentOrder;


        /// <summary>
        /// Текущий пользователь
        /// </summary>
        private Client? _currentClient;

        /// <summary>
        /// Текущий пользователь
        /// </summary>
        public Client? CurrentClient
        {
            get => _currentClient;
            set
            {
                if (_currentClient?.Id != value?.Id)
                {
                    _currentClient = value;
                    CurrentUserChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Текущий пользователь
        /// </summary>
        public Order? CurrentOrder
        {
            get => _currentOrder;
            set
            {
                if (_currentOrder?.Id != value?.Id)
                {
                    _currentOrder = value;
                    CurrentUserChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Ивент при изменении текущего пользователя (пока что нигде не используется)
        /// </summary>
        public event Action? CurrentUserChanged;
    }
}