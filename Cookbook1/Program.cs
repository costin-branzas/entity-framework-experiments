using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


//context SETUP
CookbookContextFactory cookbookContextFactory = new CookbookContextFactory();
//using CookbookContext cookbookContext =  cookbookContextFactory.CreateDbContext(args);


////ADD
//Console.WriteLine("Add porridge for breakfast");

//Dish dish = new Dish {Title = "Breakfast Porridge", Notes="This is very good", Stars = 4 };
//cookbookContext.Add(dish);
//await cookbookContext.SaveChangesAsync();

//Console.WriteLine($"Added porridge (id={dish.Id}) successfully");

////READ (check stars for porridge)
//Console.WriteLine("Checking stars for porridge");
//List<Dish> dishes = await cookbookContext.Dishes
//    .Where(dish => dish.Title.Contains("Porridge"))
//    .ToListAsync(); // LINQ -> SQL by entity framework in the background

//if (dishes.Count != 1)
//    Console.Error.WriteLine("Unexpected number of dishes found (!=1)");

//Console.WriteLine($"Porridge has {dishes[0].Stars} stars"); 

////EDIT
//dish.Stars = 5;
//Console.WriteLine("Change porridge stars to 5");
//await cookbookContext.SaveChangesAsync();
//Console.WriteLine("Changed porridge stars to 5");


////REMOVE
//Console.WriteLine($"Removing porridge from database");
//cookbookContext.Remove(dish);
//await cookbookContext.SaveChangesAsync();
//Console.WriteLine($"Porridge removed");


////An experiment to prove existance of "change tracker"...
//using CookbookContext cookbookContext = cookbookContextFactory.CreateDbContext();
//Dish newDish = new Dish { Title = "Foo", Notes = "bar" };
//cookbookContext.Dishes.Add(newDish);
//await cookbookContext.SaveChangesAsync();
//newDish.Notes = "Baz";
//await cookbookContext.SaveChangesAsync(); // this will result in updating only the notes, so EF knows exactly what was changed - done via Change Tracker - part of the db context


//CHANGE TRACKER experiment methods
await EntityStates(cookbookContextFactory);
await ChangeTracking(cookbookContextFactory);
await AttachEntities(cookbookContextFactory);
await NoTracking(cookbookContextFactory);

//RAW SQL
await RawSql(cookbookContextFactory);

//TRANSACTIONS
await Transactions(cookbookContextFactory);

static async Task Transactions(CookbookContextFactory cookbookContextFactory)
{
    using CookbookContext cookbookContext = cookbookContextFactory.CreateDbContext();

    using var transaction = await cookbookContext.Database.BeginTransactionAsync();

    try
    {
        cookbookContext.Dishes.Add(new Dish { Title = "Foo", Notes = "Bar" });
        await cookbookContext.SaveChangesAsync();

        await cookbookContext.Database.ExecuteSqlRawAsync("SELECT 1/0 as Bad");

        await transaction.CommitAsync(); //this commits the transactions, if this is not reached, nothing since transacation was started will actually be saved in the DB
    }
    catch(SqlException ex)
    {
        Console.Error.WriteLine($"Something bad happened: {ex.Message}");
    }
}

static async Task RawSql(CookbookContextFactory cookbookContextFactory)
{
    using CookbookContext cookbookContext = cookbookContextFactory.CreateDbContext();

    //manual sql queries, however we still have all EF functionality (loading data into the model classes, change tracking)
    
    //fixed
    Dish[] dishes = await cookbookContext.Dishes.FromSqlRaw("SELECT * FROM Dishes").ToArrayAsync();

    //with parameters
    string filter = "%z";
    dishes = await cookbookContext.Dishes.FromSqlInterpolated($"SELECT * FROM Dishes WHERE Notes LIKE {filter}").ToArrayAsync();

    //BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD
    //BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD
    //BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD BAD
    //SQL injection!!!
    //filter might be: "%z; DELETE FROM Dishes;"
    dishes = await cookbookContext.Dishes.FromSqlRaw("SELECT * FROM Dishes WHERE Notes LIKE '" + filter + "'").ToArrayAsync();

    //writing statements that affect data but do not return data
    await cookbookContext.Database.ExecuteSqlRawAsync("DELETE FROM Dishes WHERE Id NOT IN (SELECT DishId FROM Ingredients)"); //delete all dishes that do not have ingredients?

}

static async Task EntityStates(CookbookContextFactory cookbookContextFactory)
{
    //entity states (Detached, Added, Unchanged, Modified, Deleted)

    using CookbookContext cookbookContext = cookbookContextFactory.CreateDbContext();

    //create object
    Dish newDish = new Dish { Title = "Foo", Notes = "Bar" };
    EntityState entityState = cookbookContext.Entry(newDish).State; // Detached (EF Change tracker knows nothing about the newDish object)

    //add
    cookbookContext.Dishes.Add(newDish);
    entityState = cookbookContext.Entry(newDish).State; // Added (EF Change trackers knows about about this object)

    //saved to db
    await cookbookContext.SaveChangesAsync();
    entityState = cookbookContext.Entry(newDish).State; // Unchanged (EF Change trackers knows about about this object and there are NO changes compared to the DB)

    //modified
    newDish.Notes = "Baz";
    entityState = cookbookContext.Entry(newDish).State; // Modified (EF Change trackers knows about about this object and there ARE changes compared to the DB)

    //saved to db
    await cookbookContext.SaveChangesAsync();
    entityState = cookbookContext.Entry(newDish).State; // Unchanged (EF Change trackers knows about about this object and there ARE changes compared to the DB)

    //remove
    cookbookContext.Dishes.Remove(newDish);
    entityState = cookbookContext.Entry(newDish).State; // Deleted (EF Change trackers knows about about this object and compared to the DB it is Deleted)

    //saved to db
    await cookbookContext.SaveChangesAsync();
    entityState = cookbookContext.Entry(newDish).State; // Detached (EF Change tracker knows nothing about the newDish object)
}

static async Task ChangeTracking(CookbookContextFactory cookbookContextFactory)
{
    //change tracker is specific to each data context
    
    using CookbookContext cookbookContext = cookbookContextFactory.CreateDbContext();

    Dish newDish = new Dish { Title = "Foo", Notes = "Bar" };
    cookbookContext.Dishes.Add(newDish);
    await cookbookContext.SaveChangesAsync();
    
    newDish.Notes = "Baz";
    EntityEntry entry = cookbookContext.Entry(newDish);
    var originalValue = entry.OriginalValues[nameof(Dish.Notes)].ToString();
    var dishFromDb = await cookbookContext.Dishes.SingleAsync(dish => dish.Id == newDish.Id); // because the object is allready in memory, it will NOT really be retrived from the DB, so we see the modified BUT UNSAVED value

    using CookbookContext cookbookContext2 = cookbookContextFactory.CreateDbContext();
    var dishFromDb2 = await cookbookContext2.Dishes.SingleAsync(dish => dish.Id == newDish.Id); // separate context, that dit not have the dish loaded into memory, so it does actually retrieve from the DB
}

static async Task AttachEntities(CookbookContextFactory cookbookContextFactory)
{
    //using the update method to update a Detached entity (attaches is + marks it as modified by default even if no prior knowledge about it)

    using CookbookContext cookbookContext = cookbookContextFactory.CreateDbContext();
    Dish newDish = new Dish { Title = "Foo", Notes = "Bar" };
    cookbookContext.Dishes.Add(newDish);
    await cookbookContext.SaveChangesAsync();

    //"manually" forget the newDish object
    cookbookContext.Entry(newDish).State = EntityState.Detached;

    EntityState state = cookbookContext.Entry(newDish).State; //Detached

    //using the "Update" method we cand attach an object to the context and mark it for updating when "SaveChanges" is called
    cookbookContext.Dishes.Update(newDish);
    await cookbookContext.SaveChangesAsync();
}

static async Task NoTracking(CookbookContextFactory cookbookContextFactory)
{
    using CookbookContext cookbookContext = cookbookContextFactory.CreateDbContext();

    //SELECT * from dishes
    Dish[] dishes = await cookbookContext.Dishes.AsNoTracking().ToArrayAsync();
    EntityState state = cookbookContext.Entry(dishes[0]).State; // Detached

}

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
    public CookbookContext CreateDbContext(string[] args = null)
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