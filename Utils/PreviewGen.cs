using ImageMagick;
using PuppeteerSharp;
using spapp_backend.Core.Enums;
using System;

namespace spapp_backend.Utils
{
  public class PreviewGen
  {
    private LaunchOptions launchOptions = null!;
    private int currentLaunchCount = 0;

    public async Task Init()
    {
      using var browserFetcher = new BrowserFetcher();
      await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

      launchOptions = new LaunchOptions()
      {
        Headless = true,
        Args = new string[] { "--no-sandbox" }
      };
    }

    public async Task MakePreview(string part, ulong id, MCServer mcs)
    {
      if (currentLaunchCount >= 3)
      {
        return;
      }

      try
      {
        currentLaunchCount++;

        using (var browser = await Puppeteer.LaunchAsync(launchOptions))
        using (var page = await browser.NewPageAsync())
        {
          await page.SetViewportAsync(new ViewPortOptions { Width = 1340, Height = 680 });
          var conf = App.GetConfig("Settings");
          var mode = mcs == MCServer.SP ? "SP" : "SPM";
          var previewUrl = $"{conf["FrontendUrl"]}/preview/{part}{id}?mode={mode}";
          var savePath = $"{conf["StaticDir"]}/previews/{part}/{id}.png";
          var optPath = $"{conf["StaticDir"]}/previews/{part}/{id}.webp";

          if (!Directory.Exists($"{conf["StaticDir"]}/previews/{part}"))
          {
            Directory.CreateDirectory($"{conf["StaticDir"]}/previews/{part}");
          }

          await page.GoToAsync(previewUrl);
          await page.ScreenshotAsync(savePath,
            new ScreenshotOptions
            {
              OmitBackground = true,
              Clip = new PuppeteerSharp.Media.Clip { Width = 1280, Height = 640 }
            }
          );

          using var image = new MagickImage(savePath);

          image.Format = MagickFormat.WebP;
          image.Quality = 95;
          File.Delete(savePath);
          await image.WriteAsync(optPath);
        }

        currentLaunchCount--;
      }
      catch (Exception ex)
      {
        App.Logger.WriteExceptionLog(ex, "previewgen.txt");
      }
    }
  }
}
