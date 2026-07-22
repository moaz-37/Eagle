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

            var sale = new Sale
            {
                SaleDate = DateTime.UtcNow,
                UserId = cashierId,
                TotalAmount = dto.UnitSellPrice * dto.Quantity
            };
            sale.SaleItems.Add(new SaleItem
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

        public async Task<SaleStatsSummary> GetStatsAsync(SaleStatsFilter filter)
        {
            var query = _db.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems)
                .AsQueryable();

            if (filter.From.HasValue) query = query.Where(s => s.SaleDate >= filter.From.Value);
            if (filter.To.HasValue) query = query.Where(s => s.SaleDate <= filter.To.Value);
            if (filter.CashierId.HasValue) query = query.Where(s => s.UserId == filter.CashierId.Value);

            var sales = await query.ToListAsync();

            var byCashier = sales
                .GroupBy(s => new { s.UserId, s.User.FullName })
                .Select(g => new CashierBreakdown(
                    g.Key.UserId, g.Key.FullName, g.Count(),
                    g.Sum(s => s.TotalAmount),
                    g.Sum(s => s.SaleItems.Sum(i => (i.UnitSellPrice - i.UnitBuyPrice) * i.Quantity))))
                .ToList();

            return new SaleStatsSummary(
                sales.Count,
                sales.Sum(s => s.SaleItems.Sum(i => i.Quantity)),
                sales.Sum(s => s.TotalAmount),
                sales.Sum(s => s.SaleItems.Sum(i => (i.UnitSellPrice - i.UnitBuyPrice) * i.Quantity)),
                byCashier);
        }


        public async Task<List<SaleRecordDto>> GetSaleRecordsAsync(SaleStatsFilter filter)
        {
            var query = _db.SaleItems
                .Include(si => si.Sale).ThenInclude(s => s.User)
                .Include(si => si.ProductVariant).ThenInclude(v => v.Product)
                .AsQueryable();

            if (filter.From.HasValue) query = query.Where(si => si.Sale.SaleDate >= filter.From.Value);
            if (filter.To.HasValue) query = query.Where(si => si.Sale.SaleDate <= filter.To.Value);
            if (filter.CashierId.HasValue) query = query.Where(si => si.Sale.UserId == filter.CashierId.Value);

            var items = await query
                .OrderByDescending(si => si.Sale.SaleDate)
                .ToListAsync();

            return items.Select(si => new SaleRecordDto(
                si.Sale.Id,
                si.Sale.SaleDate,
                si.ProductVariant.Product.PieceCode,
                si.ProductVariant.Product.Name,
                si.ProductVariant.Color,
                si.ProductVariant.Size,
                si.Quantity,
                si.UnitSellPrice,
                si.UnitSellPrice * si.Quantity,
                si.Sale.User.FullName
            )).ToList();
        }
    }
}