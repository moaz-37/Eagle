namespace Eagle.DAL.Entities
{
    public class Sale
    {
        public int Id { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string CashierNameSnapshot { get; set; } = string.Empty;

        // "Cash" or "Credit"
        public string PaymentType { get; set; } = "Cash";
        public decimal AmountPaid { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}