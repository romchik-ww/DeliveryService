

namespace DeliveryService.Models
{
    public class Food
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string ImageUrl { get; set; }
        public int Weight { get; set; }
        public int CategoriesId { get; set; }
        public decimal Price { get; set; }
        public Categories? Categories { get; set; }

        public ICollection<Basket> Baskets { get; set; } = new List<Basket>();
    }
}
