namespace Eagle.DAL.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;

        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public Guid? ReceivedByUserId { get; set; }
        public User? ReceivedByUser { get; set; }
        public string ReceivedByNameSnapshot { get; set; } = string.Empty;
    }
}