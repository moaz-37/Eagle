namespace Eagle.DAL.Entities
{
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public int Quantity { get; set; }
        public decimal UnitSellPrice { get; set; } // what it was actually sold for
        public decimal UnitBuyPrice { get; set; }  // snapshot of Product.BuyPrice at sale time, for profit stats
    }
}