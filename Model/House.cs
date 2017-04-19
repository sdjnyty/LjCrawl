using System;

namespace YTY.LjCrawl.Model
{
  public class House
  {
    public long Id { get; set; }

    public string Block { get; set; }

    public string HouseType { get; set; }

    public double Area { get; set; }

    public HouseDirection Direction { get; set; }

    public string Decoration { get; set; }

    public string Story { get; set; }

    public int TotalStories { get; set; }

    public int? Year { get; set; }

    public string BuildingType { get; set; }

    public int NumFollowers { get; set; }

    public int NumVisitors { get; set; }

    public HouseTag Tags { get; set; }

    public double Price { get; set; }

    public int UnitPrice { get; set; }
  }

  [Flags]
  public enum HouseDirection
  {
    South = 0b0000_0001,
    SouthEast = 0b0000_0010,
    East = 0b0000_0100,
    NorthEast = 0b0000_1000,
    North = 0b0001_0000,
    NorthWest = 0b0010_0000,
    West = 0b0100_0000,
    SouthWest = 0b1000_0000,
  }

  [Flags]
  public enum HouseTag
  {
    Certificate5Years = 1,
    Certificate2Years = 2,
  }
}
