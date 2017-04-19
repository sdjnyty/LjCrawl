using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using OfficeOpenXml;

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
      var handler = new WebRequestHandler {UseCookies = true};
      var client = new HttpClient(handler) { BaseAddress = new Uri("http://bj.lianjia.com/ershoufang") };
      client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063");

      var rootRaw = await client.GetStringAsync("");
      Console.WriteLine(handler.CookieContainer.GetCookies(client.BaseAddress));
      var rootDoc = new HtmlDocument();
      rootDoc.LoadHtml(rootRaw);
      foreach (var districtNode in rootDoc.DocumentNode.SelectNodes(
        "html/body/div[@class='m-filter']/div[@class='position']/dl[2]/dd/div[@data-role='ershoufang']/div/a"))
      {
        var district = new District { Name = districtNode.InnerText };
        Districts.Add(district);
        await Task.Delay(Frequency);
        var districtRaw = await client.GetStringAsync(districtNode.Attributes["href"].Value);
        var districtDoc = new HtmlDocument();
        districtDoc.LoadHtml(districtRaw);
        foreach (var regionNode in districtDoc.DocumentNode.SelectNodes("html/body/div[@class='m-filter']/div[@class='position']/dl[2]/dd/div[@data-role='ershoufang']/div[2]/a"))
        {
          await Task.Delay(Frequency);
          var path = regionNode.Attributes["href"].Value;
          var regionRaw = await client.GetStringAsync(path);
          var regionDoc = new HtmlDocument();
          regionDoc.LoadHtml(regionRaw);
          var pageData =
            regionDoc.DocumentNode.SelectSingleNode(
                "html/body/div[@class='content ']/div[@class='leftContent']/div[@class='contentBottom clear']/div[@class='page-box fr']/div[@class='page-box house-lst-page-box']")
              ?.Attributes["page-data"]?.Value;
          if (pageData == null)
            continue;
          var pageCount = int.Parse(regexPage.Match(pageData).Groups[1].Value);
          var region = new Region { Name = regionNode.InnerText };
          district.Regions.Add(region);
          for (var pageIndex = 1; pageIndex <= pageCount; pageIndex++)
          {
            await Task.Delay(Frequency);
            var pageRaw = await client.GetStringAsync($"{path}/pg{pageIndex}");
            var pageDoc = new HtmlDocument();
            pageDoc.LoadHtml(pageRaw);
            foreach (var item in pageDoc.DocumentNode.SelectNodes(
              "html/body/div[@class='content ']/div[@class='leftContent']/ul[@class='sellListContent']/li/div[@class='info clear']"))
            {
              var url = item.SelectSingleNode("div[@class='title']/a").Attributes["href"].Value;
              var houseInfoNode = item.SelectSingleNode("div[@class='address']/div");
              var array = houseInfoNode.InnerText.Split(new[] { " | " }, StringSplitOptions.None);
              var followMatch = regexFollow.Match(item.SelectSingleNode("div[@class='followInfo']").InnerText);
              var house = new House
              {
                Id = long.Parse(regexId.Match(url).Groups[1].Value),
                Block = array[0],
                HouseType = array[1],
                Area = double.Parse(array[2].TrimEnd('平', '米')),
                Direction = (HouseDirection)array[3].Split(' ').Sum(d => 1 << Array.IndexOf(directions, d)),
                Decoration = array[4],
                NumFollowers = int.Parse(followMatch.Groups[1].Value),
                NumVisitors = int.Parse(followMatch.Groups[2].Value),
                Price = double.Parse(item.SelectSingleNode("div[@class='priceInfo']/div[@class='totalPrice']/span").InnerText),
                UnitPrice = int.Parse(item.SelectSingleNode("div[@class='priceInfo']/div[@class='unitPrice']").Attributes["data-price"].Value)
              };

              var positionMatch = regexStory.Match(item.SelectSingleNode("div[@class='flood']/div").InnerText);
              if (positionMatch.Success)
              {
                house.Story = positionMatch.Groups[1].Value;
                house.TotalStories = int.Parse(positionMatch.Groups[2].Value);
                if (positionMatch.Groups[3].Success)
                  house.Year = int.Parse(positionMatch.Groups[3].Value);
                house.BuildingType = positionMatch.Groups[4].Value;
              }
              region.Houses.Add(house);
              var tagNode = item.SelectSingleNode("div[@class='tag']");
              if (tagNode.SelectSingleNode("span[@class='taxfree']") != null)
                house.Tags |= HouseTag.Certificate5Years;
              if (tagNode.SelectSingleNode("span[@class='five']") != null)
                house.Tags |= HouseTag.Certificate2Years;
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
        sheet.SetValue(++row, ++col, "区");
        sheet.SetValue(row, ++col, "区域");
        sheet.SetValue(row, ++col, "Id");
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
          foreach (var region in district.Regions)
          {
            foreach (var house in region.Houses)
            {
              col = 0;
              sheet.SetValue(++row, ++col, district.Name);
              sheet.SetValue(row, ++col, region.Name);
              sheet.SetValue(row, ++col, house.Id.ToString());
              sheet.Cells[row, col].Hyperlink = new Uri($"http://bj.lianjia.com/ershoufang/{house.Id}.html");
              sheet.Cells[row, col].StyleName = "Hyperlink";
              sheet.SetValue(row, ++col, house.Block);
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
    private static readonly Regex regexPage = new Regex(@"{""totalPage"":(\d+)");
    private static readonly string[] directions = { "南", "东南", "东", "东北", "北", "西北", "西", "西南" };

  }
}
