using System.ComponentModel.DataAnnotations;

namespace EfCoreAdvanced.Dtos;

public record ProductDto(int Id, string Name, decimal Price, string CategoryName, byte[] RowVersion);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

// الحقول المسموح بتعديلها بس. مفيش Id ولا IsDeleted عشان نمنع الـ overposting
public class UpdateProductDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    [Range(0, 1_000_000)]
    public decimal Price { get; set; }

    public int CategoryId { get; set; }

    // النسخة اللي العميل شافها لما جاب المنتج
    [Required]
    public byte[] RowVersion { get; set; } = null!;
}
