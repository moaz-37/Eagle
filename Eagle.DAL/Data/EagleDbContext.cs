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
        public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<DailyOverrideCode> DailyOverrideCodes => Set<DailyOverrideCode>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sales)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Sale)
                .WithMany(s => s.Payments)
                .HasForeignKey(p => p.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ReceivedByUser)
                .WithMany()
                .HasForeignKey(p => p.ReceivedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SaleReturn>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SaleReturn>()
                .HasOne(r => r.SaleItem)
                .WithMany()
                .HasForeignKey(r => r.SaleItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>().HasIndex(p => p.PieceCode).IsUnique();
            modelBuilder.Entity<ProductVariant>().HasIndex(v => new { v.ProductId, v.Color, v.Size }).IsUnique();
            modelBuilder.Entity<DailyOverrideCode>().HasIndex(c => c.Date).IsUnique();

            modelBuilder.Entity<Product>().Property(p => p.BuyPrice).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Product>().Property(p => p.SellPrice).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Sale>().Property(s => s.AmountPaid).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitSellPrice).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitBuyPrice).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Payment>().Property(p => p.Amount).HasColumnType("decimal(10,2)");
        }
    }
}