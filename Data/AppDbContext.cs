using EfCoreAdvanced.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAdvanced.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Category>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(100);
        });

        b.Entity<Product>(e =>
        {
            // لازم MaxLength: nvarchar(max) مينفعش يتعمل عليه Index في SQL Server
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Price).HasPrecision(18, 2);

            // (3) Index على Name: أكتر عمود بيتفلتر/يتبحث بيه
            e.HasIndex(p => p.Name);

            // (4) المنتجات المحذوفة منطقيًا متظهرش في أي query
            e.HasQueryFilter(p => !p.IsDeleted);

            // (6) rowversion بيتغير تلقائي مع كل UPDATE
            e.Property(p => p.RowVersion).IsRowVersion();

            e.HasOne(p => p.Category)
             .WithMany(c => c.Products)
             .HasForeignKey(p => p.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
