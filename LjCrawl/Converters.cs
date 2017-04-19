using System;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using YTY.LjCrawl.Model;

namespace YTY.LjCrawl
{
  public static class Converters
  {
    public static IValueConverter HouseDirectionToString { get; } = new HouseDirectionToStringConverter();

    private class HouseDirectionToStringConverter : IValueConverter
    {
      public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
      {
        var direction = (int)value;
        return string.Join(" ",
          Enumerable.Range(0, 8)
            .Select(i => new { i, flag = 1 << i })
            .Where(tuple => (tuple.flag & direction) == tuple.flag)
            .Select(tuple => directions[tuple.i]));
      }

      public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      {
        throw new NotImplementedException();
      }

      private static readonly string[] directions = { "南", "东南", "东", "东北", "北", "西北", "西", "西南" };
    }
  }
}
