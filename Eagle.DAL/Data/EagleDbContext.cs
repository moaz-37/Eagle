using Eagle.DAL.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Eagle.DAL.Data
{
    public class EagleDbContext : IdentityDbContext<User, Role, Guid>
    {
        public EagleDbContext(DbContextOptions<EagleDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sales)
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.PieceCode)
                .IsUnique();

            modelBuilder.Entity<ProductVariant>()
                .HasIndex(v => new { v.ProductId, v.Color, v.Size })
                .IsUnique();

            modelBuilder.Entity<Product>().Property(p => p.BuyPrice).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Product>().Property(p => p.SellPrice).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitSellPrice).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitBuyPrice).HasColumnType("decimal(10,2)");
        }
    }
}