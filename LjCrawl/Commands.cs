using System;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Windows;
using YTY.LjCrawl.Model;

namespace YTY.LjCrawl
{
  public static class Commands
  {
    public static ICommand StartCrawl { get; } = new StartCrawlCommand();

    public static ICommand HyperlinkByLjId { get; } = new HyperlinkByLjIdCommand();

    public static ICommand ExportToExcel { get; } = new ExportToExcelCommand();

    private class StartCrawlCommand : ICommand
    {
      public bool CanExecute(object parameter)
      {
        return true;
      }

      public async void Execute(object parameter)
      {
        var session = parameter as CrawlSession;
        await session.StartCrawl();
        MessageBox.Show(Application.Current.MainWindow, "完成数据抓取，可以点击导出");
      }

      public event EventHandler CanExecuteChanged;
    }

    private class HyperlinkByLjIdCommand : ICommand
    {
      public bool CanExecute(object parameter)
      {
        return true;
      }

      public void Execute(object parameter)
      {
        Process.Start($"http://bj.lianjia.com/ershoufang/{parameter}.html");
      }

      public event EventHandler CanExecuteChanged;
    }

    private class ExportToExcelCommand : ICommand
    {
      public bool CanExecute(object parameter)
      {
        return true;
      }

      public void Execute(object parameter)
      {
        var session = parameter as CrawlSession;
        var fileName = Path.Combine(Environment.CurrentDirectory, DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx");
        session.Save(fileName);
        if (MessageBox.Show(Application.Current.MainWindow, $"保存完成，文件路径\n{fileName}\n是否打开？", string.Empty, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
          Process.Start(fileName);
      }

      public event EventHandler CanExecuteChanged;
    }
  }
}
