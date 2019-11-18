using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Server.Infrastructure.Services;

namespace SparqDigital.TypTip.Web.UserAuthentication
{
     public class Program
     {
          public static void Main(string[] args)
          {
               CreateWebHostBuilder(args).Build().Run();
          }

          public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
              WebHost.CreateDefaultBuilder(args)
                   .ConfigureLogging((hostingContext, logging) =>
                   {
                        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                        logging.AddDebug();
                        logging.AddEventSourceLogger();
                   })
                   .ConfigureServices(serviceCollection =>
                   {
                        serviceCollection
                             .AddSingleton<IConfigurationService>(new ConfigurationService(InitConfigBuilder()))
                             .AddSingleton<IRedisService, RedisService>()
                             .AddScoped<IAccessTokenService, AccessTokenService>();
                   })
                   //.UseKestrel()
                   .UseContentRoot(Directory.GetCurrentDirectory())
                   .UseIISIntegration()
                   .UseStartup<Startup>();
          
          private static IConfigurationRoot InitConfigBuilder()
          {
               var appRoot = Environment.CurrentDirectory;
               return new ConfigurationBuilder()
                    .SetBasePath(appRoot)
                    .AddJsonFile("appsettings.json", true, true)
                    .AddEnvironmentVariables()
                    .Build();
          }
     }
}
