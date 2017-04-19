using System.Collections.ObjectModel;

namespace YTY.LjCrawl.Model
{
  public class Region
  {
    public string Name { get; set; }

    public ObservableCollection<House> Houses { get; } = new ObservableCollection<House>();
  }
}
