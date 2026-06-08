

namespace DeliveryService.Models
{
    public class Basket
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int FoodId { get; set; }
        public required Food Food { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

    }
}
