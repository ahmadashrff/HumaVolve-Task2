namespace EfCoreAdvanced.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // (4) Soft Delete
    public bool IsDeleted { get; set; }

    // (6) Optimistic concurrency token (SQL Server rowversion)
    public byte[] RowVersion { get; set; } = null!;
}
