using EfCoreAdvanced.Data;
using EfCoreAdvanced.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfCoreAdvanced.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    // (1) + (2) AsNoTracking + Select + Pagination، وJOIN واحد بدل N+1
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.StartsWith(search)); // LIKE 'x%' بيستفيد من الـ Index

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name).ThenBy(p => p.Id) // ترتيب ثابت ضروري للـ pagination
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Category.Name, p.RowVersion))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(items, page, pageSize, total);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Category.Name, p.RowVersion))
            .FirstOrDefaultAsync(ct);

        return dto is null ? NotFound() : dto;
    }

    // (5) + (6) PUT بالـ Change Tracking + Concurrency
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateProductDto dto, CancellationToken ct)
    {
        // tracked entity (من غير AsNoTracking). الـ QueryFilter بيرجّع 404 للمحذوف
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound();

        if (!await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct))
            return BadRequest("Category does not exist.");

        // نعدل الحقول المسموحة بس. EF بيقارن بالـ snapshot ويبعت UPDATE للأعمدة اللي اتغيرت فقط
        product.Name = dto.Name;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;

        // أهم سطر: نقول لـ EF "النسخة الأصلية" هي اللي العميل شافها مش اللي لسه جايبها من الـ DB
        // فيطلع: UPDATE ... WHERE Id = @id AND RowVersion = @clientVersion
        _db.Entry(product).Property(p => p.RowVersion).OriginalValue = dto.RowVersion;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The product was modified by another user. Reload and try again.");
        }

        return NoContent();
    }

    // (4) Soft Delete: UPDATE مش DELETE
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (product is null) return NotFound();

        product.IsDeleted = true;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
