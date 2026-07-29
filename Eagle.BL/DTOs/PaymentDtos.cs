namespace Eagle.BL.DTOs
{
    public record SaleItemInputDto(int ProductVariantId, int Quantity, decimal UnitSellPrice, string? OverrideCode);

    public record CreateSaleRequestDto(
        List<SaleItemInputDto> Items,
        string PaymentType, // "Cash" or "Credit"
        string? CustomerName,
        string? CustomerPhone,
        decimal? AmountPaidNow);

    public record SaleLineDto(string PieceCode, string ProductName, string Color, string Size, int Quantity, decimal UnitPrice);

    public record SaleBalanceDto(
        int SaleId, DateTime SaleDate,
        string CustomerName, string? CustomerPhone,
        decimal TotalAmount, decimal AmountPaid, decimal RemainingAmount, bool IsFullyPaid,
        string CashierName,
        List<SaleLineDto> Lines);

    public record AddPaymentDto(int SaleId, decimal Amount);
    public record PaymentResult(bool Succeeded, string? Error);
}