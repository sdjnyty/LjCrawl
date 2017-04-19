using System.Collections.ObjectModel;

namespace YTY.LjCrawl.Model
{
  public class District
  {
    public string Name { get; set; }

    public ObservableCollection<Region> Regions { get; } = new ObservableCollection<Region>();
  }
}
