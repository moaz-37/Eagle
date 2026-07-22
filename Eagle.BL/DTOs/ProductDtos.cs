namespace Eagle.BL.DTOs
{
    public record ProductLookupResult
    (
        int ProductId, 
        string PieceCode, 
        string Name, 
        string? Brand,
        decimal BuyPrice, 
        decimal SellPrice,

        List<VariantDto> Variants
    );

    public record VariantDto
    (
        int VariantId, 
        string Color, 
        string Size, 
        int StockQuantity
    );

    public record CreateProductDto
    (
        //string PieceCode, 
        string Name, 
        string? Brand, 
        decimal BuyPrice, 
        decimal SellPrice
    );

    public record AddVariantDto
    (
        int ProductId, 
        string Color, 
        string Size, 
        int StockQuantity
    );

    public record ProductDetailDto
    (
        int ProductId, 
        string PieceCode, 
        string Name, 
        string? Brand,
        decimal BuyPrice, decimal SellPrice, DateTime CreatedAt,
        List<VariantDto> Variants,
        List<SaleRecordDto> SaleHistory
    );
}