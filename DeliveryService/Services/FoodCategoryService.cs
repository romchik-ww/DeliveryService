using DeliveryService.Models;
using DeliveryService.Repositories;

namespace DeliveryService.Services
{
    /// <summary>
    /// Сервис, работающий с Категориями еды
    /// </summary>
    public class FoodCategoryService
    {
        private readonly FoodCategoryRepository _foodCategoryRepository;


        public FoodCategoryService(FoodCategoryRepository foodCategoryRepository)
        {
            _foodCategoryRepository = foodCategoryRepository;
        }


        /// <summary>
        /// Получение категории еды по id
        /// </summary>
        /// <param name="categoryId">ID категории</param>
        /// <returns>Категория еды</returns>
        public async Task<Categories?> GetById(int categoryId) => await _foodCategoryRepository.GetById(categoryId);
        /// <summary>
        /// Получение всех категорий еды
        /// </summary>
        /// <returns>Список категорий еды</returns>
        public async Task<List<Categories>> GetAllAsync() => await _foodCategoryRepository.GetAllAsync();
        /// <summary>
        /// Добавление категории еды в базу данных
        /// </summary>
        /// <param name="categories">Категория еды</param>
        /// <returns>Прошла ли операция</returns>
        public async Task<bool> AddAsync(Categories categories)
        {
            if (categories == null)
                return false;

            await _foodCategoryRepository.AddAsync(categories);
            return true;
        }
    }
}