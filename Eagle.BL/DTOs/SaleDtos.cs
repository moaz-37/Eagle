namespace Eagle.BL.DTOs
{
    // Cart submission
    public record SaleItemInputDto
    (
        int ProductVariantId,
        int Quantity,
        decimal UnitSellPrice,
        string? OverrideCode
    );

    public record CreateSaleRequestDto
    (
        List<SaleItemInputDto> Items,
        string PaymentType, // "Cash" or "Credit"
        string? CustomerName,
        string? CustomerPhone,
        decimal? AmountPaidNow
    );

    // Unified receipt / balance DTO — used for printing AND for checking payment status
    public record SaleReceiptItemDto
    (
        string PieceCode,
        string ProductName,
        string Color,
        string Size,
        int Quantity,
        decimal UnitSellPrice,
        decimal LineTotal
    );

    public record SaleReceiptDto
    (
        int SaleId,
        DateTime SaleDate,
        string CashierName,
        string PaymentType,
        string CustomerName,
        string? CustomerPhone,
        decimal TotalAmount,
        decimal AmountPaid,
        decimal RemainingAmount,
        bool IsFullyPaid,
        List<SaleReceiptItemDto> Items
    );

    public record SaleResult
    (
        bool Succeeded,
        string? Error,
        int? SaleId = null
    );

    public record SaleStatsFilter
    (
        DateTime? From,
        DateTime? To,
        Guid? CashierId
    );

    public record SaleStatsSummary
    (
        int SalesCount,
        int PiecesSold,
        decimal TotalRevenue,
        decimal TotalProfit,
        List<CashierBreakdown> ByCashier
    );

    public record CashierBreakdown
    (
        string CashierName,
        int SalesCount,
        decimal Revenue,
        decimal Profit
    );

    public record SaleRecordDto
    (
        int SaleId,
        DateTime SaleDate,
        string PieceCode,
        string ProductName,
        string Color,
        string Size,
        int Quantity,
        decimal UnitSellPrice,
        decimal LineTotal,
        string CashierName
    );

    // Return / Retry
    public record SaleItemForReturnDto
    (
        int SaleItemId,
        DateTime SaleDate,
        string PieceCode,
        string ProductName,
        string Color,
        string Size,
        int QuantitySold,
        int QuantityReturned,
        int QuantityRemaining,
        decimal UnitSellPrice,
        string CashierName
    );

    public record CreateReturnDto
    (
        int SaleItemId,
        int Quantity
    );

    public record ReturnResult
    (
        bool Succeeded,
        string? Error,
        int? ReturnId = null
    );

    // Combined timeline: sale (positive profit) + return/retry (negative profit)
    public record SaleTimelineEntryDto
    (
        string Type,
        DateTime Date,
        string PieceCode,
        string ProductName,
        string Color,
        string Size,
        int Quantity,
        decimal UnitPrice,
        decimal ProfitAmount,
        string PersonName
    );

    // Payments
    public record AddPaymentDto
    (
        int SaleId,
        decimal Amount
    );

    public record PaymentResult
    (
        bool Succeeded,
        string? Error
    );
}