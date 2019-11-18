using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using SparqDigital.TypTip.Web.Infrastructure.Helpers;

namespace SparqDigital.TypTip.Web.Authentication
{
     public class Program
     {
          public static void Main(string[] args)
          {
               CurrentDirectoryHelpers.SetCurrentDirectory();
               CreateWebHostBuilder(args).Build().Run();
          }

          public static IWebHostBuilder CreateWebHostBuilder(string[] args)
          {
               return WebHost.CreateDefaultBuilder(args)
                    .ConfigureLogging((hostingContext, logging) =>
                    {
                         logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                         logging.AddConsole();
                         logging.AddDebug();
                         logging.AddEventSourceLogger();
                    })
                    //.UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>();
          }
     }
}
