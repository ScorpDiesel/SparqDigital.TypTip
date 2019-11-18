using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SparqDigital.TypTip.Server.Infrastructure.DbContext;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Server.Infrastructure.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Services;
using SparqDigital.TypTip.TextServices;
using StackExchange.Redis;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(Startup))]
namespace SparqDigital.TypTip.TextServices
{
     internal class Startup : IWebJobsStartup
     {
          public void Configure(IWebJobsBuilder builder) => builder.AddDependencyInjection<ServiceProviderBuilder>();
     }

     internal class ServiceProviderBuilder : IServiceProviderBuilder
     {
          private readonly ILoggerFactory _loggerFactory;

          public ServiceProviderBuilder(ILoggerFactory loggerFactory) => _loggerFactory = loggerFactory;

          public IServiceProvider Build()
          {
               var services = new ServiceCollection();
               services.AddSingleton<IConfigurationService, ConfigurationService>();
               services.AddTransient<IRedisService, RedisService>();
               services.AddTransient<IAccessTokenService, AccessTokenService>();
               services.AddTransient<ITextTransceiverService, TextTransceiverService>();
               //services.AddScoped<IScopedGreeter, Greeter>();
               //services.AddSingleton<ISingletonGreeter, Greeter>();

               return services.BuildServiceProvider();
          }

          private IConfigurationRoot InitConfigBuilder()
          {
               var appRoot = Environment.CurrentDirectory;
               return new ConfigurationBuilder()
                    .SetBasePath(appRoot)
                    .AddJsonFile("local.settings.json", true, true)
                    .AddEnvironmentVariables()
                    .Build();
          }
     }
}