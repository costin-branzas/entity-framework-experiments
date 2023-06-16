﻿using System;
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

class Brick
{
    public int Id { get; set; }

    [MaxLength(250)]
    public string Title { get; set; } = string.Empty;


    public Color? Color { get; set; }

    //BASIC many to many relationship (EF will auto create the many-to-many relation table) (1 brick can have mutiple tags, 1 tag can corespond to mutiple bricks)
    //what tags does this brick have?
    public List<Tag> Tags { get; set; }

    //one to many relationship (one brick can have mutiple availabilities)
    //what vendors have this brick available?
    public List<BrickAvailability> Availability { get; set; }
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
    public List<Brick> Bricks { get; set; }
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