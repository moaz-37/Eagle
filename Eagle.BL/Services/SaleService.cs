using Eagle.BL.DTOs;
using Eagle.DAL.Data;
using Eagle.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eagle.BL.Services
{
    public class SaleService
    {
        private readonly EagleDbContext _db;
        private readonly OverrideCodeService _overrideCodeService;

        public SaleService(EagleDbContext db, OverrideCodeService overrideCodeService)
        {
            _db = db;
            _overrideCodeService = overrideCodeService;
        }

        public async Task<SaleResult> CreateSaleAsync(CreateSaleDto dto, Guid cashierId)
        {
            if (dto.Items is null || dto.Items.Count == 0)
                return new SaleResult(false, "لا توجد أصناف في عملية البيع.");

            var variantIds = dto.Items.Select(i => i.ProductVariantId).Distinct().ToList();
            var variants = await _db.ProductVariants
                .Include(v => v.Product)
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id);

            // Merge duplicate lines for the same variant (in case the client sends two rows for it)
            var mergedItems = dto.Items
                .GroupBy(i => i.ProductVariantId)
                .Select(g => new CreateSaleItemDto(
                    g.Key,
                    g.Sum(i => i.Quantity),
                    g.First().UnitSellPrice,
                    g.First().OverrideCode))
                .ToList();

            var saleItems = new List<SaleItem>();
            decimal total = 0;

            foreach (var item in mergedItems)
            {
                if (!variants.TryGetValue(item.ProductVariantId, out var variant))
                    return new SaleResult(false, "أحد الأصناف غير موجود.");

                if (item.Quantity <= 0)
                    return new SaleResult(false, $"الكمية غير صحيحة للصنف {variant.Product.PieceCode}.");

                if (variant.StockQuantity < item.Quantity)
                    return new SaleResult(false,
                        $"الكمية المتاحة من {variant.Product.PieceCode} ({variant.Color} - {variant.Size}) هي {variant.StockQuantity} فقط.");

                if (item.UnitSellPrice < variant.Product.BuyPrice)
                {
                    var isValidCode = await _overrideCodeService.ValidateCodeAsync(item.OverrideCode);
                    if (!isValidCode)
                        return new SaleResult(false,
                            $"سعر البيع أقل من سعر الشراء للصنف {variant.Product.PieceCode}. يجب إدخال الكود اليومي الصحيح من المدير.");
                }

                saleItems.Add(new SaleItem
                {
                    ProductVariantId = variant.Id,
                    Quantity = item.Quantity,
                    UnitSellPrice = item.UnitSellPrice,
                    UnitBuyPrice = variant.Product.BuyPrice
                });

                total += item.UnitSellPrice * item.Quantity;
            }

            var user = await _db.Users.FindAsync(cashierId);

            var sale = new Sale
            {
                SaleDate = DateTime.UtcNow,
                UserId = cashierId,
                CashierNameSnapshot = user?.FullName ?? "غير معروف",
                TotalAmount = total
            };

            foreach (var si in saleItems)
                sale.SaleItems.Add(si);

            foreach (var item in mergedItems)
                variants[item.ProductVariantId].StockQuantity -= item.Quantity;

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

        public async Task<SaleReceiptDto?> GetSaleReceiptAsync(int saleId)
        {
            var sale = await _db.Sales
                .Include(s => s.SaleItems).ThenInclude(si => si.ProductVariant).ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale is null) return null;

            return new SaleReceiptDto(
                sale.Id, sale.SaleDate, sale.CashierNameSnapshot, sale.TotalAmount,
                sale.SaleItems.Select(si => new SaleReceiptItemDto(
                    si.ProductVariant.Product.PieceCode, si.ProductVariant.Product.Name,
                    si.ProductVariant.Color, si.ProductVariant.Size,
                    si.Quantity, si.UnitSellPrice, si.UnitSellPrice * si.Quantity
                )).ToList()
            );
        }
    }
}