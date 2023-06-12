using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Hello, World!");



//the model class
class Dish
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Notes { get; set; }

    public int? Stars { get; set; }

    public List<DishIngedient> DishIngedients { get; set; } = new List<DishIngedient>();
}

class DishIngedient
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal Amount { get; set; }

    public Dish? Dish { get; set; }

    public int DishId { get; set; }
}

class CookbookContext : DbContext
{
    public DbSet<Dish> Dishes { get; set; }

    public DbSet<DishIngedient> Ingredients { get; set; }

    public CookbookContext(DbContextOptions<CookbookContext> dbContextOptions) : base(dbContextOptions)
    {

    }
}

class CookbookContextFactory : IDesignTimeDbContextFactory<CookbookContext>
{
    public CookbookContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<CookbookContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
            // .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new CookbookContext(optionsBuilder.Options);
    }
}