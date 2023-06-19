using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

Console.WriteLine("Hello, World!");


enum Color
{
    Black, //will get saved as 0 in the DB
    White, //will get saved as 1 in the DB...
    Red,
    Yellow,
    Orange,
    Green
}

#region Model
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
            // .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);

        return new BrickContext(optionsBuilder.Options);
    }
}
