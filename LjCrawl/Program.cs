using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using HtmlAgilityPack;

namespace YTY.LjCrawl
{
  class Program
  {
    private static readonly Regex regexId = new Regex(@".+/(\d+)\.html");
    private static readonly Regex regexStory = new Regex(@"(\w+)\(共(\d+)层\)(\d+)年建(\w+)");
    private static readonly Regex regexFollow = new Regex(@"(\d+)人关注 / 共(\d+)次带看");
    private static readonly Regex regexPage = new Regex(@"{""totalPage"":(\d+)}");
    private static readonly string[] directions = { "南", "东南", "东", "东北", "北", "西北", "西", "西南" };

    static void Main(string[] args)
    {
      Console.OutputEncoding = Encoding.Unicode;
      DoTheJob().Wait();
    }

    private static async Task DoTheJob()
    {
      var client = new HttpClient { BaseAddress = new Uri("http://bj.lianjia.com/ershoufang") };
      client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063");

      var model = new CrawlSession();
      var rootRaw = await client.GetStringAsync("");
      var rootDoc = new HtmlDocument();
      rootDoc.LoadHtml(rootRaw);
      foreach (var districtNode in rootDoc.DocumentNode.SelectNodes(
        "html/body/div[@class='m-filter']/div[@class='position']/dl[2]/dd/div[@data-role='ershoufang']/div/a"))
      {
        var district = new District { Name = districtNode.InnerText };
        model.Districts.Add(district);
        Console.WriteLine(district.Name);
        await Task.Delay(2000);
        var districtRaw = await client.GetStringAsync(districtNode.Attributes["href"].Value);
        var districtDoc = new HtmlDocument();
        districtDoc.LoadHtml(districtRaw);
        foreach (var regionNode in districtDoc.DocumentNode.SelectNodes("html/body/div[@class='m-filter']/div[@class='position']/dl[2]/dd/div[@data-role='ershoufang']/div[2]/a"))
        {
          await Task.Delay(2000);
          var path = regionNode.Attributes["href"].Value;
          var regionRaw = await client.GetStringAsync(path);
          var regionDoc = new HtmlDocument();
          regionDoc.LoadHtml(regionRaw);
          var pageData =
            regionDoc.DocumentNode.SelectSingleNode(
                "html/body/div[@class='content ']/div[@class='leftContent']/div[@class='contentBottom clear']/div[@class='page-box fr']/div[@class='page-box house-lst-page-box']")
              ?.Attributes["page-data"]?.Value;
          var pageCount = int.Parse(regexPage.Match(pageData).Groups[1].Value);
          var region = new Region { Name = regionNode.InnerText };
          district.Regions.Add(region);
          Console.WriteLine($"\t{region.Name}\t{pageCount}");
          for (var pageIndex = 1; pageIndex <= pageCount; pageIndex++)
          {
            await Task.Delay(2000);
            var pageRaw = await client.GetStringAsync($"{path}/pg{pageIndex}");
            var pageDoc = new HtmlDocument();
            pageDoc.LoadHtml(pageRaw);
            foreach (var item in pageDoc.DocumentNode.SelectNodes(
              "html/body/div[@class='content ']/div[@class='leftContent']/ul[@class='sellListContent']/li/div[@class='info clear']"))
            {
              var url = item.SelectSingleNode("div[@class='title']/a").Attributes["href"].Value;
              var houseInfoNode = item.SelectSingleNode("div[@class='address']/div");
              var array = houseInfoNode.InnerText.Split(new[] { " | " }, StringSplitOptions.None);
              var positionMatch = regexStory.Match(item.SelectSingleNode("div[@class='flood']/div").InnerText);
              var followMatch = regexFollow.Match(item.SelectSingleNode("div[@class='followInfo']").InnerText);
              var house = new House
              {
                Id = long.Parse(regexId.Match(url).Groups[1].Value),
                Block = array[0],
                HouseType = array[1],
                Area = double.Parse(array[2].TrimEnd('平','米')),
                Direction = (HouseDirection)array[3].Split(' ').Sum(d => 1 << Array.IndexOf(directions, d)),
                Decoration = array[4],
                Story = positionMatch.Groups[1].Value,
                TotalStories = int.Parse(positionMatch.Groups[2].Value),
                Year = int.Parse(positionMatch.Groups[3].Value),
                BuildingType = positionMatch.Groups[4].Value,
                NumFollowers = int.Parse(followMatch.Groups[1].Value),
                NumVisitors = int.Parse(followMatch.Groups[2].Value),
                Price = double.Parse(item.SelectSingleNode("div[@class='priceInfo']/div[@class='totalPrice']/span").InnerText),
                UnitPrice = int.Parse(item.SelectSingleNode("div[@class='priceInfo']/div[@class='unitPrice']").Attributes["data-price"].Value)
              };
              region.Houses.Add(house);
              var tagNode = item.SelectSingleNode("div[@class='tag']");
              if (tagNode.SelectSingleNode("span[@class='taxfree']") != null)
                house.Tags |= HouseTag.Certificate5Years;
              if (tagNode.SelectSingleNode("span[@class='five']") != null)
                house.Tags |= HouseTag.Certificate2Years;
            }
            break;
          }
          break;
        }
        break;
      }
      //File.WriteAllText("output.txt", doc.DocumentNode.WriteContentTo());
      Console.WriteLine("Done");
      Console.ReadKey();
    }
  }
}
