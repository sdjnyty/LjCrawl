using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using HtmlAgilityPack;

namespace YTY.LjCrawl
{
  class Program
  {
    static void Main(string[] args)
    {
      DoTheJob().Wait();
    }

    private static async Task DoTheJob()
    {
      var client = new HttpClient { BaseAddress = new Uri("http://bj.lianjia.com/ershoufang") };
      var rootRaw = await client.GetStringAsync("");
      var rootDoc = new HtmlDocument();
      rootDoc.LoadHtml(rootRaw);
      foreach (var node in rootDoc.DocumentNode.SelectNodes(
        "html/body/div[@class='m-filter']/div[@class='position']/dl[2]/dd/div[@data-role='ershoufang']/div/a"))
      {
        var regionRaw=await client.GetStringAsync(node.Attributes["href"].Value);
        var regionDoc = new HtmlDocument();
        regionDoc.LoadHtml(regionRaw);
        var pageParentNode =
          regionDoc.DocumentNode.SelectSingleNode(
            "html/body/div[@class='content ']/div[@class='leftContent']/div[@class='contentBottom clear']/div[@class='page-box fr']/div[@class='page-box house-lst-page-box']/span");
        Console.Write($"{pageParentNode.SelectSingleNode("span").NextSibling.InnerText} pages\t");
        Console.WriteLine(node.InnerText);
      }
      //File.WriteAllText("output.txt", doc.DocumentNode.WriteContentTo());
      Console.WriteLine("Done");
      Console.ReadKey();
    }
  }
}
