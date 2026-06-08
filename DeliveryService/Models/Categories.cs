
namespace DeliveryService.Models
{
    public class Categories
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public ICollection<Food?> Foods { get; set; } = new List<Food?>();
    }
}
