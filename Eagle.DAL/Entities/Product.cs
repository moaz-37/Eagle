namespace Eagle.DAL.Entities
{
    public class Product 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Size { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}