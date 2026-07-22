namespace Eagle.DAL.Entities
{
    public class SaleReturn
    {
        public int Id { get; set; }

        public int SaleItemId { get; set; }
        public SaleItem SaleItem { get; set; } = null!;

        public int Quantity { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string ProcessedByNameSnapshot { get; set; } = string.Empty;
    }
}