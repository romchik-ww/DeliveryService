using DeliveryService.Models;
using DeliveryService.Repositories;

namespace DeliveryService.Services
{
    /// <summary>
    /// Сервис, работающий с Едой
    /// </summary>
    public class FoodService
    {
        private readonly FoodRepository _foodRepository;


        public FoodService(FoodRepository foodRepository)
        {
            _foodRepository = foodRepository;
        }


        /// <summary>
        /// Получение объекта еды по id
        /// </summary>
        /// <param name="foodId">ID еды</param>
        /// <returns>Объект еды</returns>
        public async Task<Food?> GetById(int foodId) => await _foodRepository.GetById(foodId);
        /// <summary>
        /// Получение всей еды
        /// </summary>
        /// <returns>Список всеё еды</returns>
        public async Task<List<Food>> GetAllAsync() => await _foodRepository.GetAllAsync();
        /// <summary>
        /// Получение всей еды по категории
        /// </summary>
        /// <param name="categoryId">ID категории</param>
        /// <returns>Список объектов еды с указанной категорией. Если таких нет - пустой список</returns>
        public async Task<List<Food>> GetAllFromCategoryAsync(int categoryId)
            => await _foodRepository.GetAllFromCategoryAsync(categoryId);
        /// <summary>
        /// Добавление еды в базы данных
        /// </summary>
        /// <param name="food">Объект еды</param>
        /// <returns>Прошла ли операция</returns>
        public async Task<bool> AddAsync(Food food)
        {
            if (food == null)
                return false;

            await _foodRepository.AddAsync(food);
            return true;
        }
    }
}