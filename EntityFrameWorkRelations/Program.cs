﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


BrickContextFactory factory = new BrickContextFactory();
using BrickContext context = factory.CreateDbContext();

//await AddData();
//Console.WriteLine($"Done adding data to DB.");

await QueryData();
Console.WriteLine($"Done querying data from DB.");

async Task QueryData()
{
    // basic includes ("eager loading")
    BrickAvailability[] brickAvailabilities = await context.BrickAvailabilities
        .Include(brickAvailability => brickAvailability.Brick) // "hey EF, please also include the related Brick" (translated to a JOIN in SQL)
        .Include(brickAvailability => brickAvailability.Vendor) // "hey EF, please also include the related Vendor" (translated to a JOIN in SQL)
        .ToArrayAsync();

    foreach (BrickAvailability brickAvailability in brickAvailabilities)
    {
        Console.WriteLine($"Brick {brickAvailability.Brick.Title} available at {brickAvailability.Vendor.VendorName} for {brickAvailability.PriceEur}");
    }

    Console.WriteLine();

    //multi layer include
    Brick[] bricksWithVendorsAndTags = await context.Bricks
        .Include($"{nameof(Brick.Availability)}.{nameof(BrickAvailability.Vendor)}") // this goes and loads related entities thaat are "2 levels away", first goes into availability table, then into the vendor table
        .Include(brick => brick.Tags)
        //.Where(brick => brick.Tags.Any(tag => tag.Title == "Minecraft")) // if we were to reference tags in a Where call, tags would be loaded automatically without the need for the Include call above
        .ToArrayAsync();

    foreach(Brick brickWithVendorsAndTags in bricksWithVendorsAndTags)
    {
        Console.Write($"Brick {brickWithVendorsAndTags.Title}");
        if (brickWithVendorsAndTags.Tags.Any())
        {
            Console.Write($" ({string.Join(", ", brickWithVendorsAndTags.Tags.Select(tag => tag.Title))})");
        }
        if (brickWithVendorsAndTags.Availability.Any())
        {
            Console.Write($" is available at {string.Join(", ", brickWithVendorsAndTags.Availability.Select(brickAvailability => brickAvailability.Vendor.VendorName))}");
        }
    }
    Console.WriteLine();

    //how to load related data, at a later time, AFTER intial query ("lazy" loading):
    Brick[] bricks = await context.Bricks.ToArrayAsync();
    foreach (Brick brick in bricks)
    {
        await context.Entry(brick).Collection(brick => brick.Tags).LoadAsync(); // this loads the tags for the brick
        
        Console.Write($"Brick {brick.Title}");
        Console.Write($" ({string.Join(", ", brick.Tags.Select(tag => tag.Title))})");
    }
}

async Task AddData()
{
    //vendors
    Vendor brickKing, brickDepot;
    await context.AddRangeAsync(new[] 
    { 
        brickKing = new Vendor() { VendorName = "Brick King" },
        brickDepot = new Vendor() { VendorName = "Brick Depot" },
    });
    await context.SaveChangesAsync();

    //tags
    Tag rare, ninjago, minecraft;
    await context.AddRangeAsync(new[]
    {
        rare = new Tag() { Title = "Rare" },
        ninjago = new Tag() { Title = "Ninjago" },
        minecraft = new Tag() { Title = "Minecraft" },
        
    });
    await context.SaveChangesAsync();

    //adding a baseplate
    await context.AddAsync(new BasePlate 
    { 
        Title = "BasePlate 16 x 16 with blue watter pattern", 
        Color = Color.Green, 
        Tags = new() { rare, minecraft }, // adding "many to many"
        Length = 16,
        Width = 16,
        Availability = new List<BrickAvailability> // adding "many to one"
        { 
            new BrickAvailability(){ Vendor = brickKing, AvailableAmount = 5, PriceEur = 6.5m },
            new BrickAvailability(){ Vendor = brickDepot, AvailableAmount = 10, PriceEur = 5.9m },
        }
    });
    await context.SaveChangesAsync();
}

#region Model
enum Color
{
    Black, //will get saved as 0 in the DB
    White, //will get saved as 1 in the DB...
    Red,
    Yellow,
    Orange,
    Green
}

class Brick
{
    public int Id { get; set; }

    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;


    public Color? Color { get; set; }

    //BASIC many to many relationship (EF will auto create the many-to-many relation table) (1 brick can have mutiple tags, 1 tag can corespond to mutiple bricks)
    //what tags does this brick have?
    public List<Tag> Tags { get; set; } = new();

    //one to many relationship (one brick can have mutiple availabilities)
    //what vendors have this brick available?
    public List<BrickAvailability> Availability { get; set; } = new();
}

//BasePlate & MinifigHead are derived classes, EF will create a SINGLE DB table for the entire inheritanche hierarchy (aka "(1) table per hierarchy")
class BasePlate : Brick
{
    public int Length { get; set; }

    public int Width { get; set; }
}

class MinifigHead : Brick
{
    public bool IsDualSided { get; set; }
}

class Tag
{
    public int Id { get; set; }

    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;

    //many to many relationship: 1 brick can have mutiple tags, 1 tag can corespond to mutiple bricks
    //what bricks does this tag apply to?
    public List<Brick> Bricks { get; set; } = new();
}

class Vendor
{
    public int Id { get; set; }

    [MaxLength(250)]
    public string VendorName { get; set; }

    //one to many (one vendor can have availabilities for mutiple bricks)
    //what bricks are available from this vendor?
    public List<BrickAvailability> Availability { get; set; } = new List<BrickAvailability>();
}

//ADVANCED many-to-many; this entire class with it's 2x "one to many" relationships actualy represents a many to many connection between brick and vendors but with added "actual" data
class BrickAvailability
{
    public int Id { get; set; }

    // connection to vendor
    public Vendor Vendor { get; set; }

    public int VendorId { get; set; }

    //connection to brick
    public Brick Brick { get; set; }

    public int BrickId { get; set; }

    //actual data
    public int AvailableAmount { get; set; }

    [Column(TypeName="decimal(8, 2)")] // 8 digits, 2 of them being precision after comma
    public decimal PriceEur { get; set; }
}
#endregion

#region Data Context
class BrickContext : DbContext
{
    public BrickContext(DbContextOptions<BrickContext> options) : base(options)
    {}

    public DbSet<Brick> Bricks { get; set; }

    public DbSet<Vendor> Vendors { get; set; }
    
    public DbSet<BrickAvailability> BrickAvailabilities { get; set; }
    
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //BasePlate & MinifigHead are derived classes, EF will create a SINGLE DB table for the entire inheritanche hierarchy (aka "(1) table per hierarchy")
        modelBuilder.Entity<BasePlate>().HasBaseType<Brick>();
        modelBuilder.Entity<MinifigHead>().HasBaseType<Brick>();
    }
}

class BrickContextFactory : IDesignTimeDbContextFactory<BrickContext>
{
    public BrickContext CreateDbContext(string[]? args = null)
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        var optionsBuilder = new DbContextOptionsBuilder<BrickContext>();
        optionsBuilder
            // Uncomment the following line if you want to print generated
            // SQL statements on the console.
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new BrickContext(optionsBuilder.Options);
    }
}
#endregion