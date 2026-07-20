// Eagle.DAL/Data/EagleDbContext.cs
using Eagle.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Eagle.DAL.Data
{
    public class EagleDbContext : IdentityDbContext<User,Role,Guid>
    {
        public EagleDbContext(DbContextOptions<EagleDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Sale> Sales => Set<Sale>();
        public DbSet<SaleItem> SaleItems => Set<SaleItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // must call this for Identity tables to be configured

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.User)
                .WithMany(u => u.Sales)
                .HasForeignKey(s => s.UserId);

            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Sale>().Property(s => s.TotalAmount).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<SaleItem>().Property(si => si.UnitPrice).HasColumnType("decimal(10,2)");
        }
    }
}