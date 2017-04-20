using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;
using OfficeOpenXml;
using Newtonsoft.Json;

namespace YTY.LjCrawl.Model
{
  public class CrawlSession
  {
    /// <summary>
    /// Timespan between http requests, in millisecond per request
    /// </summary>
    public int Frequency { get; set; } = 5000;

    public ObservableCollection<District> Districts { get; } = new ObservableCollection<District>();

    public async Task StartCrawl()
    {
      var handler = new WebRequestHandler { UseCookies = true };
      var client = new HttpClient(handler) { BaseAddress = new Uri("http://bj.lianjia.com/") };
      client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063");

      var rootRaw = await client.GetStringAsync("xiaoqu");
      var rootDoc = new HtmlDocument();
      rootDoc.LoadHtml(rootRaw);
      if (rootDoc.DocumentNode.SelectSingleNode("html/head/title").InnerText.StartsWith("验证异常流量"))
      {
        await DealWithException();
      }

      foreach (var districtNode in rootDoc.DocumentNode.SelectNodes("html/body/div[3]/div[1]/dl[2]/dd[1]/div[1]/div[1]/a"))
      {
        await Task.Delay(Frequency);
        var district = new District { Name = districtNode.InnerText };
        Districts.Add(district);
        var districtPath = districtNode.Attributes["href"].Value;
        var districtRaw = await client.GetStringAsync(districtPath);
        var districtDoc = new HtmlDocument();
        districtDoc.LoadHtml(districtRaw);
        var numBlocks = int.Parse(districtDoc.DocumentNode.SelectSingleNode("html/body/div[4]/div[1]/div[2]/h2[1]/span[1]").InnerText);
        var districtPageCount = (numBlocks - 1) / 30 + 1;

        for (var districtPageIndex = 1; districtPageIndex <= districtPageCount; districtPageIndex++)
        {
          var blockPageRaw = await client.GetStringAsync($"{districtPath}pg{districtPageIndex}");
          var blockPageDoc = new HtmlDocument();
          blockPageDoc.LoadHtml(blockPageRaw);

          foreach (var blockNode in blockPageDoc.DocumentNode.SelectNodes("html/body/div[4]/div[1]/ul[1]/li"))
          {
            var blockHref = blockNode.SelectSingleNode("div[2]/div[2]/a[1]").Attributes["href"].Value;
            var blockRaw = await client.GetStringAsync(blockHref);
            var blockDoc = new HtmlDocument();
            blockDoc.LoadHtml(blockRaw);
            var numHouses = int.Parse(blockDoc.DocumentNode.SelectSingleNode("html/body/div[@class='content ']/div[1]/div[2]/h2[1]/span[1]").InnerText);
            if (numHouses == 0) // 该小区0套房源
              continue;
            var pageCount = (numHouses - 1) / 30 + 1;
            await Task.Delay(Frequency);
            var block = new Block();
            district.Blocks.Add(block);
            block.Id = blockNode.SelectSingleNode("a").Attributes["href"].Value.Split('/').Reverse().ElementAt(1);
            block.Name = blockNode.SelectSingleNode("div[1]/div[1]/a[1]").InnerText;
            block.Region = blockNode.SelectSingleNode("div[1]/div[3]/a[2]").InnerText;
            for (var pageIndex = 1; pageIndex <= pageCount; pageIndex++)
            {
              HtmlDocument pageDoc;
              if (pageIndex == 1)
              {
                pageDoc = blockDoc;
              }
              else
              {
                await Task.Delay(Frequency);
                var pageRaw = await client.GetStringAsync($"ershoufang/pg{pageIndex}c{block.Id}");
                pageDoc = new HtmlDocument();
                pageDoc.LoadHtml(pageRaw);
              }
              foreach (var houseNode in pageDoc.DocumentNode.SelectNodes(
                "html/body/div[4]/div[1]/ul[1]/li/div[1]"))
              {
                var url = houseNode.SelectSingleNode("div[1]/a[1]").Attributes["href"].Value;
                var houseInfoNode = houseNode.SelectSingleNode("div[2]/div[1]");
                var array = houseInfoNode.InnerText.Split(new[] { " | " }, StringSplitOptions.None);
                var followMatch = regexFollow.Match(houseNode.SelectSingleNode("div[4]").InnerText);
                var house = new House();
                block.Houses.Add(house);
                house.Id = regexId.Match(url).Groups[1].Value;
                house.HouseType = array[1];
                house.Area = double.Parse(array[2].TrimEnd('平', '米'));
                house.Direction = (HouseDirection)array[3].Split(' ').Sum(d => 1 << Array.IndexOf(directions, d));
                house.Decoration = array[4];
                house.NumFollowers = int.Parse(followMatch.Groups[1].Value);
                house.NumVisitors = int.Parse(followMatch.Groups[2].Value);
                house.Price = double.Parse(houseNode.SelectSingleNode("div[6]/div[1]/span[1]").InnerText);
                house.UnitPrice = int.Parse(houseNode.SelectSingleNode("div[6]/div[2]").Attributes["data-price"].Value);

                var positionMatch = regexStory.Match(houseNode.SelectSingleNode("div[3]/div[1]").InnerText);
                if (positionMatch.Success)
                {
                  house.Story = positionMatch.Groups[1].Value;
                  house.TotalStories = int.Parse(positionMatch.Groups[2].Value);
                  if (positionMatch.Groups[3].Success)
                    house.Year = int.Parse(positionMatch.Groups[3].Value);
                  house.BuildingType = positionMatch.Groups[4].Value;
                }
                var tagNode = houseNode.SelectSingleNode("div[5]");
                if (tagNode.SelectSingleNode("span[@class='taxfree']") != null)
                  house.Tags |= HouseTag.Certificate5Years;
                if (tagNode.SelectSingleNode("span[@class='five']") != null)
                  house.Tags |= HouseTag.Certificate2Years;
              }
            }
          }
        }
      }
    }

    public void Save(string fileName)
    {
      using (var pkg = new ExcelPackage(new FileInfo(fileName)))
      {
        var sheet = pkg.Workbook.Worksheets.Add("Sheet 1");
        var styleHyperlink = pkg.Workbook.Styles.CreateNamedStyle("HyperLink");
        styleHyperlink.Style.Font.UnderLine = true;
        styleHyperlink.Style.Font.Color.SetColor(Color.Blue);
        var row = 0;
        var col = 0;
        sheet.SetValue(++row, ++col, "Id");
        sheet.SetValue(row, ++col, "区");
        sheet.SetValue(row, ++col, "区域");
        sheet.SetValue(row, ++col, "小区");
        sheet.SetValue(row, ++col, "户型");
        sheet.SetValue(row, ++col, "面积(㎡)");
        sheet.SetValue(row, ++col, "朝向");
        sheet.SetValue(row, ++col, "装修");
        sheet.SetValue(row, ++col, "楼层");
        sheet.SetValue(row, ++col, "总层数");
        sheet.SetValue(row, ++col, "建造年份");
        sheet.SetValue(row, ++col, "楼型");
        sheet.SetValue(row, ++col, "关注人数");
        sheet.SetValue(row, ++col, "带看次数");
        sheet.SetValue(row, ++col, "标签");
        sheet.SetValue(row, ++col, "总价(万元)");
        sheet.SetValue(row, ++col, "单价(元/㎡)");
        foreach (var district in Districts)
        {
          foreach (var block in district.Blocks)
          {
            foreach (var house in block.Houses)
            {
              col = 0;
              sheet.SetValue(++row, ++col, house.Id);
              sheet.Cells[row, col].Hyperlink = new Uri($"http://bj.lianjia.com/ershoufang/{house.Id}.html");
              sheet.Cells[row, col].StyleName = "Hyperlink";
              sheet.SetValue(row, ++col, district.Name);
              sheet.SetValue(row, ++col, block.Region);
              sheet.SetValue(row, ++col, block.Name);
              sheet.SetValue(row, ++col, house.HouseType);
              sheet.SetValue(row, ++col, house.Area);
              sheet.SetValue(row, ++col,
                string.Join("/",
                Enumerable.Range(0, 8)
                .Select(i => new { i, flag = 1 << i })
                .Where(tuple => (tuple.flag & (int)house.Direction) == tuple.flag)
                .Select(tuple => directions[tuple.i])));
              sheet.SetValue(row, ++col, house.Decoration);
              sheet.SetValue(row, ++col, house.Story);
              sheet.SetValue(row, ++col, house.TotalStories);
              sheet.SetValue(row, ++col, house.Year);
              sheet.SetValue(row, ++col, house.BuildingType);
              sheet.SetValue(row, ++col, house.NumFollowers);
              sheet.SetValue(row, ++col, house.NumVisitors);
              if (house.Tags.HasFlag(HouseTag.Certificate5Years))
                sheet.SetValue(row, ++col, "满五年");
              else if (house.Tags.HasFlag(HouseTag.Certificate2Years))
                sheet.SetValue(row, ++col, "满两年");
              else
                sheet.SetValue(row, ++col, string.Empty);
              sheet.SetValue(row, ++col, house.Price);
              sheet.SetValue(row, ++col, house.UnitPrice);
            }
          }
        }
        for (var i = 1; i <= col; i++)
          sheet.Column(i).AutoFit();
        sheet.Tables.Add(new ExcelAddressBase(1, 1, row, col), "Table1");
        pkg.Save();
      }
    }

    private static readonly Regex regexId = new Regex(@".+/(\d+)\.html");
    private static readonly Regex regexStory = new Regex(@"(\w+)\(共(\d+)层\)(?:(\d+)年建)?(\w+)");
    private static readonly Regex regexFollow = new Regex(@"(\d+)人关注 / 共(\d+)次带看");
    private static readonly string[] directions = { "南", "东南", "东", "东北", "北", "西北", "西", "西南" };

    private async Task DealWithException()
    {
      var client = new HttpClient {BaseAddress = new Uri("http://captcha.lianjia.com/human/")};
      dynamic json = JsonConvert.DeserializeObject(await client.GetStringAsync(""));
      string image0 = json.images["0"];
      
      string uuid = json.uuid;
    }
  }
}
