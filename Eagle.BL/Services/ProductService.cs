using Eagle.BL.DTOs;
using Eagle.DAL.Data;
using Eagle.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Eagle.BL.Services
{
    public class ProductService
    {
        private readonly EagleDbContext _db;
        public ProductService(EagleDbContext db) => _db = db;

        public async Task<ProductLookupResult?> LookupByPieceCodeAsync(string pieceCode)
        {
            var product = await _db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.PieceCode == pieceCode);

            if (product is null) return null;

            return new ProductLookupResult(
                product.Id, product.PieceCode, product.Name, product.Brand,
                product.BuyPrice, product.SellPrice,
                product.Variants
                    .Select(v => new VariantDto(v.Id, v.Color, v.Size, v.StockQuantity))
                    .ToList());
        }

        public async Task<List<Product>> GetAllAsync(string? search = null)
        {
            var query = _db.Products.Include(p => p.Variants).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.PieceCode.Contains(search));

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<int> CreateAsync(CreateProductDto dto)
        {
            var pieceCode = await GenerateUniquePieceCodeAsync();

            var product = new Product
            {
                PieceCode = pieceCode,
                Name = dto.Name,
                Brand = dto.Brand,
                BuyPrice = dto.BuyPrice,
                SellPrice = dto.SellPrice,
                CreatedAt = DateTime.UtcNow
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product.Id;
        }

        public async Task<Product?> GetByIdAsync(int id) =>
            await _db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);

        private async Task<string> GenerateUniquePieceCodeAsync()
        {
            var random = new Random();
            string code;
            bool exists;

            do
            {
                code = random.Next(10000, 99999).ToString(); // always 5 digits, no leading zero issues
                exists = await _db.Products.AnyAsync(p => p.PieceCode == code);
            } while (exists);

            return code;
        }

        public async Task<List<string>> GetColorsAsync(int productId) =>
            await _db.ProductVariants
                .Where(v => v.ProductId == productId)
                .Select(v => v.Color)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

        public async Task<List<VariantDto>> GetSizesForColorAsync(int productId, string color) =>
            await _db.ProductVariants
                .Where(v => v.ProductId == productId && v.Color == color)
                .OrderBy(v => v.Size)
                .Select(v => new VariantDto(v.Id, v.Color, v.Size, v.StockQuantity))
                .ToListAsync();

        public async Task AddOrIncrementVariantAsync(AddVariantDto dto)
        {
            var existing = await _db.ProductVariants.FirstOrDefaultAsync(v =>
                v.ProductId == dto.ProductId && v.Color == dto.Color && v.Size == dto.Size);

            if (existing != null)
            {
                existing.StockQuantity += dto.StockQuantity;
            }
            else
            {
                _db.ProductVariants.Add(new ProductVariant
                {
                    ProductId = dto.ProductId,
                    Color = dto.Color,
                    Size = dto.Size,
                    StockQuantity = dto.StockQuantity
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<StoreOverviewDto> GetStoreOverviewAsync()
        {
            var products = await _db.Products.Include(p => p.Variants).ToListAsync();

            var inventoryValueAtCost = products.Sum(p => p.Variants.Sum(v => v.StockQuantity) * p.BuyPrice);
            var expectedProfit = products.Sum(p => p.Variants.Sum(v => v.StockQuantity) * (p.SellPrice - p.BuyPrice));

            return new StoreOverviewDto(inventoryValueAtCost, expectedProfit, 0, 0);
        }

    }
}