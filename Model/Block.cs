using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace YTY.LjCrawl.Model
{
  /// <summary>
  /// 小区
  /// </summary>
  public class Block
  {
    public string Region { get; set; }

    public string Id { get; set; }

    public string Name { get; set; }

    public ObservableCollection<House> Houses { get; } = new ObservableCollection<House>();
  }
}
