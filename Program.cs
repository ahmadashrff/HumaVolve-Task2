using EfCoreAdvanced.Data;
using EfCoreAdvanced.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));

    if (builder.Environment.IsDevelopment())
    {
        // اطبع الـ SQL في الـ console عشان تتأكد إن مفيش N+1
        options.LogTo(Console.WriteLine,
            new[] { DbLoggerCategory.Database.Command.Name },
            Microsoft.Extensions.Logging.LogLevel.Information);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // للتشغيل السريع: بينشئ الـ DB ويزرع داتا تجريبية.
    // لو هتستخدم Migrations: امسح السطر ده واستخدم db.Database.Migrate() بدله.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Seed(db);
}

app.MapControllers();
app.Run();

static void Seed(AppDbContext db)
{
    if (db.Categories.Any()) return;

    var electronics = new Category { Name = "Electronics" };
    var books = new Category { Name = "Books" };
    var home = new Category { Name = "Home" };
    db.Categories.AddRange(electronics, books, home);

    var cats = new[] { electronics, books, home };
    for (var i = 1; i <= 30; i++)
    {
        db.Products.Add(new Product
        {
            Name = i == 1 ? "Laptop" : $"Product {i:00}",
            Price = 10 + i * 5,
            Category = cats[i % 3]
        });
    }
    db.SaveChanges();
}
