using Eagle.BL.DTOs;
using Eagle.DAL.Data;
using Eagle.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eagle.BL.Services
{
    public class SaleService
    {
        private readonly EagleDbContext _db;
        public SaleService(EagleDbContext db) => _db = db;

        public async Task<SaleResult> CreateSaleAsync(CreateSaleDto dto, Guid cashierId)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == dto.ProductVariantId);

            if (variant is null)
                return new SaleResult(false, "Variant not found.");

            if (dto.Quantity <= 0)
                return new SaleResult(false, "Quantity must be greater than zero.");

            if (variant.StockQuantity < dto.Quantity)
                return new SaleResult(false, $"Only {variant.StockQuantity} piece(s) available in this color/size.");

            if (dto.UnitSellPrice < variant.Product.BuyPrice)
                return new SaleResult(false, "Sell price cannot be lower than the buy price.");

            var user = await _db.Users.FindAsync(cashierId);

            var sale = new Sale
            {
                SaleDate = DateTime.UtcNow,
                UserId = cashierId,
                CashierNameSnapshot = user?.FullName ?? "غير معروف",
                TotalAmount = dto.UnitSellPrice * dto.Quantity
            };

            sale.SaleItems.Add(
                new SaleItem
                {
                    ProductVariantId = variant.Id,
                    Quantity = dto.Quantity,
                    UnitSellPrice = dto.UnitSellPrice,
                    UnitBuyPrice = variant.Product.BuyPrice
                });

            variant.StockQuantity -= dto.Quantity;

            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            return new SaleResult(true, null, sale.Id);
        }

        public async Task<List<SaleRecordDto>> GetSaleRecordsAsync(SaleStatsFilter filter)
        {
            var query = _db.SaleItems
                .Include(si => si.Sale)
                .Include(si => si.ProductVariant).ThenInclude(v => v.Product)
                .AsQueryable();

            if (filter.From.HasValue) query = query.Where(si => si.Sale.SaleDate >= filter.From.Value);

            if (filter.To.HasValue) query = query.Where(si => si.Sale.SaleDate <= filter.To.Value);

            if (filter.CashierId.HasValue) query = query.Where(si => si.Sale.UserId == filter.CashierId.Value);

            var items = await query.OrderByDescending(si => si.Sale.SaleDate).ToListAsync();

            return items.Select(si => new SaleRecordDto
            (
                si.Sale.Id, si.Sale.SaleDate,
                si.ProductVariant.Product.PieceCode, si.ProductVariant.Product.Name,
                si.ProductVariant.Color, si.ProductVariant.Size,
                si.Quantity, si.UnitSellPrice, si.UnitSellPrice * si.Quantity,
                si.Sale.CashierNameSnapshot
            )).ToList();
        }

        public async Task<List<SaleRecordDto>> GetSaleHistoryForProductAsync(int productId)
        {
            var items = await _db.SaleItems
                .Include(si => si.Sale)
                .Include(si => si.ProductVariant).ThenInclude(v => v.Product)
                .Where(si => si.ProductVariant.ProductId == productId)
                .OrderByDescending(si => si.Sale.SaleDate)
                .ToListAsync();

            return items.Select(si => new SaleRecordDto
            (
                si.Sale.Id, si.Sale.SaleDate,
                si.ProductVariant.Product.PieceCode, si.ProductVariant.Product.Name,
                si.ProductVariant.Color, si.ProductVariant.Size,
                si.Quantity, si.UnitSellPrice, si.UnitSellPrice * si.Quantity,
                si.Sale.CashierNameSnapshot
            )).ToList();
        }

        // Search sale items eligible for return, by piece code
        public async Task<List<SaleItemForReturnDto>> SearchSaleItemsByPieceCodeAsync(string pieceCode)
        {
            var items = await _db.SaleItems
                .Include(si => si.Sale)
                .Include(si => si.ProductVariant).ThenInclude(v => v.Product)
                .Where(si => si.ProductVariant.Product.PieceCode == pieceCode)
                .OrderByDescending(si => si.Sale.SaleDate)
                .ToListAsync();

            var itemIds = items.Select(i => i.Id).ToList();
            var returnedMap = await _db.SaleReturns
                .Where(r => itemIds.Contains(r.SaleItemId))
                .GroupBy(r => r.SaleItemId)
                .Select(g => new { SaleItemId = g.Key, Qty = g.Sum(r => r.Quantity) })
                .ToDictionaryAsync(x => x.SaleItemId, x => x.Qty);

            return items
                .Select(si =>
                {
                    var returned = returnedMap.TryGetValue(si.Id, out var q) ? q : 0;
                    return new SaleItemForReturnDto(
                        si.Id, si.Sale.SaleDate, si.ProductVariant.Product.PieceCode, si.ProductVariant.Product.Name,
                        si.ProductVariant.Color, si.ProductVariant.Size, si.Quantity, returned, si.Quantity - returned,
                        si.UnitSellPrice, si.Sale.CashierNameSnapshot);
                })
                .Where(dto => dto.QuantityRemaining > 0)
                .ToList();
        }

        public async Task<ReturnResult> CreateReturnAsync(CreateReturnDto dto, Guid processedByUserId)
        {
            var saleItem = await _db.SaleItems
                .Include(si => si.ProductVariant)
                .FirstOrDefaultAsync(si => si.Id == dto.SaleItemId);

            if (saleItem is null)
                return new ReturnResult(false, "عملية البيع غير موجودة.");

            if (dto.Quantity <= 0)
                return new ReturnResult(false, "الكمية غير صحيحة.");

            var alreadyReturned = await _db.SaleReturns
                .Where(r => r.SaleItemId == dto.SaleItemId)
                .SumAsync(r => (int?)r.Quantity) ?? 0;

            var remaining = saleItem.Quantity - alreadyReturned;
            if (dto.Quantity > remaining)
                return new ReturnResult(false, $"لا يمكن إرجاع أكثر من {remaining} قطعة.");

            var user = await _db.Users.FindAsync(processedByUserId);

            var saleReturn = new SaleReturn
            {
                SaleItemId = dto.SaleItemId,
                Quantity = dto.Quantity,
                ReturnDate = DateTime.UtcNow,
                UserId = processedByUserId,
                ProcessedByNameSnapshot = user?.FullName ?? "غير معروف"
            };

            saleItem.ProductVariant.StockQuantity += dto.Quantity;

            _db.SaleReturns.Add(saleReturn);
            await _db.SaveChangesAsync();

            return new ReturnResult(true, null, saleReturn.Id);
        }

        // Unified timeline: sales (+profit) and returns (-profit)
        public async Task<List<SaleTimelineEntryDto>> GetTimelineAsync(SaleStatsFilter filter)
        {
            var salesQuery = _db.SaleItems
                .Include(si => si.Sale)
                .Include(si => si.ProductVariant).ThenInclude(v => v.Product)
                .AsQueryable();

            if (filter.From.HasValue) salesQuery = salesQuery.Where(si => si.Sale.SaleDate >= filter.From.Value);

            if (filter.To.HasValue) salesQuery = salesQuery.Where(si => si.Sale.SaleDate <= filter.To.Value);

            if (filter.CashierId.HasValue) salesQuery = salesQuery.Where(si => si.Sale.UserId == filter.CashierId.Value);

            var saleItems = await salesQuery.ToListAsync();

            var entries = saleItems.Select(si => new SaleTimelineEntryDto
            (
                "بيع", si.Sale.SaleDate, si.ProductVariant.Product.PieceCode, si.ProductVariant.Product.Name,
                si.ProductVariant.Color, si.ProductVariant.Size, si.Quantity, si.UnitSellPrice,
                (si.UnitSellPrice - si.UnitBuyPrice) * si.Quantity,
                si.Sale.CashierNameSnapshot
            )).ToList();

            var saleItemIds = saleItems.Select(s => s.Id).ToList();
            var returns = await _db.SaleReturns
                .Include(r => r.SaleItem).ThenInclude(si => si.ProductVariant).ThenInclude(v => v.Product)
                .Where(r => saleItemIds.Contains(r.SaleItemId))
                .ToListAsync();

            entries.AddRange(returns.Select(r => new SaleTimelineEntryDto
            (
                "إرجاع", r.ReturnDate, r.SaleItem.ProductVariant.Product.PieceCode, r.SaleItem.ProductVariant.Product.Name,
                r.SaleItem.ProductVariant.Color, r.SaleItem.ProductVariant.Size, r.Quantity, r.SaleItem.UnitSellPrice,
                -(r.SaleItem.UnitSellPrice - r.SaleItem.UnitBuyPrice) * r.Quantity,
                r.ProcessedByNameSnapshot
            )));

            return entries.OrderByDescending(e => e.Date).ToList();
        }

        public async Task<SaleStatsSummary> GetStatsAsync(SaleStatsFilter filter)
        {
            var entries = await GetTimelineAsync(filter);

            var byCashier = entries
                .GroupBy(e => e.PersonName)
                .Select(g => new CashierBreakdown
                (
                    g.Key,
                    g.Count(e => e.Type == "بيع"),
                    g.Sum(e => e.Type == "بيع" ? e.Quantity * e.UnitPrice : -e.Quantity * e.UnitPrice),
                    g.Sum(e => e.ProfitAmount)
                ))
                .ToList();

            return new
            (
                entries.Count(e => e.Type == "بيع"),
                entries.Sum(e => e.Type == "بيع" ? e.Quantity : -e.Quantity),
                entries.Sum(e => e.Type == "بيع" ? e.Quantity * e.UnitPrice : -e.Quantity * e.UnitPrice),
                entries.Sum(e => e.ProfitAmount),
                byCashier
            );
        }


        public async Task<List<SaleReturnRecordDto>> GetReturnHistoryForProductAsync(int productId)
        {
            var returns = await _db.SaleReturns
                .Include(r => r.SaleItem).ThenInclude(si => si.ProductVariant)
                .Where(r => r.SaleItem.ProductVariant.ProductId == productId)
                .OrderByDescending(r => r.ReturnDate)
                .ToListAsync();

            return returns.Select(r => new SaleReturnRecordDto(
                r.Id, r.ReturnDate,
                r.SaleItem.ProductVariant.Color, r.SaleItem.ProductVariant.Size,
                r.Quantity, r.SaleItem.UnitSellPrice,
                r.ProcessedByNameSnapshot
            )).ToList();
        }
    }
}