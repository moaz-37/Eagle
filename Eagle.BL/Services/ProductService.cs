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

        public async Task<List<Product>> GetAllAsync() =>
            await _db.Products.Include(p => p.Variants).OrderByDescending(p => p.CreatedAt).ToListAsync();

        public async Task<int> CreateAsync(CreateProductDto dto)
        {
            if (await _db.Products.AnyAsync(p => p.PieceCode == dto.PieceCode))
                throw new InvalidOperationException("Piece code already exists.");

            var product = new Product
            {
                PieceCode = dto.PieceCode,
                Name = dto.Name,
                Brand = dto.Brand,
                BuyPrice = dto.BuyPrice,
                SellPrice = dto.SellPrice
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product.Id;
        }

        public async Task AddVariantAsync(AddVariantDto dto)
        {
            var exists = await _db.ProductVariants.AnyAsync(v =>
                v.ProductId == dto.ProductId && v.Color == dto.Color && v.Size == dto.Size);
            if (exists) throw new InvalidOperationException("That color/size already exists for this piece.");

            _db.ProductVariants.Add(new ProductVariant
            {
                ProductId = dto.ProductId,
                Color = dto.Color,
                Size = dto.Size,
                StockQuantity = dto.StockQuantity
            });
            await _db.SaveChangesAsync();
        }
    }
}