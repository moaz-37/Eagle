namespace Eagle.DAL.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string PieceCode { get; set; } = string.Empty; // what the cashier types in
        public string Name { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public decimal BuyPrice { get; set; }   // what you paid for it
        public decimal SellPrice { get; set; }  // default/suggested sell price
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}