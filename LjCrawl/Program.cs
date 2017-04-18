using System;
using System.Collections.Generic;
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
      client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063");

      var rootRaw = await client.GetStringAsync("");
      var rootDoc = new HtmlDocument();
      rootDoc.LoadHtml(rootRaw);
      foreach (var district in rootDoc.DocumentNode.SelectNodes(
        "html/body/div[@class='m-filter']/div[@class='position']/dl[2]/dd/div[@data-role='ershoufang']/div/a"))
      {
        Console.WriteLine(district.InnerText);
        await Task.Delay(1000);
        var districtRaw = await client.GetStringAsync(district.Attributes["href"].Value);
        var districtDoc = new HtmlDocument();
        districtDoc.LoadHtml(districtRaw);
        foreach (var region in districtDoc.DocumentNode.SelectNodes("html/body/div[@class='m-filter']/div[@class='position']/dl[2]/dd/div[@data-role='ershoufang']/div[2]/a"))
        {
          Console.Write($"\t{region.InnerText}");
          var regionRaw = await client.GetStringAsync(region.Attributes["href"].Value);
          var regionDoc = new HtmlDocument();
          regionDoc.LoadHtml(regionRaw);
          var pageData =
            regionDoc.DocumentNode.SelectSingleNode(
                "html/body/div[@class='content ']/div[@class='leftContent']/div[@class='contentBottom clear']/div[@class='page-box fr']/div[@class='page-box house-lst-page-box']")
              ?.Attributes["page-data"]?.Value;
          Console.WriteLine($"{pageData}");
        }
      }
      //File.WriteAllText("output.txt", doc.DocumentNode.WriteContentTo());
      Console.WriteLine("Done");
      Console.ReadKey();
    }
  }
}
