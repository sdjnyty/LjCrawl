using System.Collections.ObjectModel;

namespace YTY.LjCrawl
{
  public class CrawlSession
  {
    public ObservableCollection<District> Districts { get; } = new ObservableCollection<District>();
  }
}
