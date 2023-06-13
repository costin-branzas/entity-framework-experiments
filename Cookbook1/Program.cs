using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


//context SETUP
CookbookContextFactory cookbookContextFactory = new CookbookContextFactory();
using CookbookContext cookbookContext =  cookbookContextFactory.CreateDbContext(args);


//ADD
Console.WriteLine("Add porridge for breakfast");

Dish dish = new Dish {Title = "Breakfast Porridge", Notes="This is very good", Stars = 4 };
cookbookContext.Add(dish);
await cookbookContext.SaveChangesAsync();

Console.WriteLine($"Added porridge (id={dish.Id}) successfully");

//READ (check stars for porridge)
Console.WriteLine("Checking stars for porridge");
List<Dish> dishes = await cookbookContext.Dishes
    .Where(dish => dish.Title.Contains("Porridge"))
    .ToListAsync(); // LINQ -> SQL by entity framework in the background

if (dishes.Count != 1)
    Console.Error.WriteLine("Unexpected number of dishes found (!=1)");

Console.WriteLine($"Porridge has {dishes[0].Stars} stars"); 

//EDIT
dish.Stars = 5;
Console.WriteLine("Change porridge stars to 5");
await cookbookContext.SaveChangesAsync();
Console.WriteLine("Changed porridge stars to 5");


//REMOVE
Console.WriteLine($"Removing porridge from database");
cookbookContext.Remove(dish);
await cookbookContext.SaveChangesAsync();
Console.WriteLine($"Porridge removed");



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
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new CookbookContext(optionsBuilder.Options);
    }
}