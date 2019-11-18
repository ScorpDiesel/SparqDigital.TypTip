using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SparqDigital.TypTip.Core.Data.Enums;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.DbContext;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Server.Infrastructure.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Services;
using SparqDigital.TypTip.Web.Authentication.ExtensionMethods;
using SparqDigital.TypTip.Web.Infrastructure.Interfaces;
using SparqDigital.TypTip.Web.Infrastructure.Services;
using Swashbuckle.AspNetCore.Swagger;

namespace SparqDigital.TypTip.Web.Authentication
{
     public class Startup
     {
          public void ConfigureServices(IServiceCollection services)
          {
               var configurationRoot = InitConfigBuilder();
               services.AddOptions();
               services.Configure<BootstrapConfiguration>(configurationRoot.GetSection("BootstrapConfiguration"));
               services.AddSingleton<ILoggerRepository, LoggerRepository>();
               services.AddSingleton<ILoggerService, LoggerService>();
               services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
               services.AddSingleton<IConfigurationService, ConfigurationService>();
               services.AddSingleton<IRedisService, RedisService>();
               services.AddSingleton<IAccessTokenService, AccessTokenService>();
               services.AddTransient<IPasswordHashService, PasswordHashService>();
               services.AddSingleton<IMongoDbMembershipContext, MongoDbMembershipContext>();
               services.AddTransient<IUserAccountRepository, AccountRepository>();
               services.AddTransient<IAdministratorAccountRepository, AdministratorAccountRepository>();
               services.AddTransient<IUserGroupRepository, UserGroupRepository>();
               services.AddTransient<IAccountService, AccountService>();
               services.AddTransient<IUserGroupService, UserGroupService>();
               services.AddTransient<IDbHashKeyRepository, DbHashKeyRepository>();
               services.AddTransient<IDbHashKeyService, DbHashKeyService>();
               services.AddSingleton<IScheduledBackgroundService, ScheduledBackgroundService>();
               services.AddHostedService<ServiceScheduler>();
               services.AddCors(options =>
               {
                    options.AddPolicy("CorsPolicy",
                         builder => builder
                              .AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials());
               });
               services.AddSwaggerGen(options =>
               {
                    options.SwaggerDoc("v1", new Info { Title = "TypTipAdminPanel", Version = "v1" });
                    //options.SwaggerDoc("v1", new Info { Title = "TypTipAdminPanel", Version = "v1" });
               });
               //services.AddAntiforgery(options =>
               //{
               //     options.HeaderName = "X-XSRF-TOKEN";
               //});

          }

          public void Configure(IApplicationBuilder app, IHostingEnvironment env)
          {
               app.AddAdminPipeline(env);
               app.AddUserPipeline(env);
          }

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
