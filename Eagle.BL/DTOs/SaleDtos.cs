namespace Eagle.BL.DTOs
{
    public record CreateSaleDto
    (
        int ProductVariantId, 
        int Quantity, 
        decimal UnitSellPrice
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
        Guid CashierId, 
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
}