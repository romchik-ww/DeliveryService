using DeliveryService.Data;
using DeliveryService.Models;
using Microsoft.EntityFrameworkCore;


namespace DeliveryService.Repositories
{
    /// <summary>
    /// Репозиторий для доступа к клиентам в Базе Данных
    /// </summary>
    public class ClientRepository
    {
        private readonly AppDbContext _context;
        public ClientRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получение всех клиентов
        /// </summary>
        /// <returns>Возвращает список всех клиентов</returns>
        public async Task<List<Client>> GetAllAsync()
        {
            return await _context.Clients
                .Include(o => o.Orders)
                .ToListAsync();
        }
        /// <summary>
        /// Получение клиента по id
        /// </summary>
        /// <param name="id">Айди клиента</param>
        /// <returns>Вовзращает клиента с указанным id</returns>
        public async Task<Client?> GetById(int id) => await _context.Clients.FindAsync(id);
        /// <summary>
        /// Получение клиента по логину(имени)
        /// </summary>
        /// <param name="name">Логин</param>
        /// <returns></returns>
        public async Task<Client?> GetByName(string name) => await _context.Clients.FirstOrDefaultAsync(x => x.Name == name);

        /// <summary>
        /// Добавление клиента в БД
        /// </summary>
        /// <param name="client">Созданный объект клиента, которого нужно добавить</param>
        public async Task AddAsync(Client client)
        {
            await _context.Clients.AddAsync(client);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Удаление клиента из БД
        /// </summary>
        /// <param name="client">Объект клиента, которого нужно удалить</param>
        public async Task DeleteAsync(Client client)
        {
            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Обновляет данные в базе данных
        /// </summary>
        /// <param name="client">Объекь клиента, которого нужно обновить</param>
        /// <returns></returns>
        public async Task UpdateAsync(Client client)
        {
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();
        }
    }
}
