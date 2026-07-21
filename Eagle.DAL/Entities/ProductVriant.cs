namespace Eagle.DAL.Entities
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string Color { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int StockQuantity { get; set; }

        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}