using System;
using System.Linq;
using AspNetCore.Identity.Mongo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SparqDigital.TypTip.Core.Data.Enums;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.DbContext;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Server.Infrastructure.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Services;
using SparqDigital.TypTip.Web.Authentication.MultiPipeline;
using SparqDigital.TypTip.Web.Infrastructure.Filters;
using WebApiContrib.Core;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace SparqDigital.TypTip.Web.Authentication.ExtensionMethods
{
     public static class MultiPiplineExtensions
     {
          public static IApplicationBuilder AddAdminPipeline(this IApplicationBuilder app, IHostingEnvironment env)
          {
               var configurationRoot = InitConfigBuilder(env);
               app.UseBranchWithServices("/admin",
                    services =>
                    {
                         //services.AddHttpContextAccessor();
                         //services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                         services.AddOptions();
                         services.Configure<BootstrapConfiguration>(configurationRoot.GetSection("BootstrapConfiguration"));
                         services.AddSingleton<ILoggerRepository, LoggerRepository>();
                         services.AddSingleton<ILoggerService, LoggerService>();
                         services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
                         services.AddSingleton<IConfigurationService, ConfigurationService>();
                         services.AddSingleton<IRedisService, RedisService>();
                         services.AddScoped<IAccessTokenService, AccessTokenService>();
                         services.AddTransient<IPasswordHashService, PasswordHashService>();
                         services.AddScoped<IMongoDbMembershipContext, MongoDbMembershipContext>();
                         services.AddTransient<IUserAccountRepository, AccountRepository>();
                         services.AddTransient<IAdministratorAccountRepository, AdministratorAccountRepository>();
                         services.AddTransient<IUserGroupRepository, UserGroupRepository>();
                         services.AddTransient<IAccountService, AccountService>();
                         services.AddTransient<IUserGroupService, UserGroupService>();
                         services.AddTransient<IDbHashKeyRepository, DbHashKeyRepository>();
                         services.AddTransient<IDbHashKeyService, DbHashKeyService>();
                         services.AddTransient<INotificationService, NotificationService>();
                         var serviceProvider = services.BuildServiceProvider();
                         var loggerService = serviceProvider.GetRequiredService<ILoggerService>();
                         var bootstrapConfigOptions = serviceProvider.GetRequiredService<IOptions<BootstrapConfiguration>>();
                         var bootstrap = bootstrapConfigOptions.Value;
                         var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
                         var accessTokenService = serviceProvider.GetRequiredService<IAccessTokenService>();
                         var mongoDbUrlTemplate = bootstrap.MongoDbConnectionStrings;
                         var sysConfig = configurationService.SystemConfiguration;
                         var mongoDbNames = sysConfig.MongoDbNames;
                         var accountsDbName = mongoDbNames.FirstOrDefault(m => m.DbType == MongoDbType.Accounts)?.DbName;
                         var connectionString = string.Format(mongoDbUrlTemplate, accountsDbName);

                         services.AddMvc(options =>
                              {
                                   options.Filters.Add(new ValidateControllerActionsFilter(loggerService));
                                   options.Filters.Add(new LogControllerExceptionsFilter(loggerService));
                              })
                              .SetCompatibilityVersion(CompatibilityVersion.Version_2_2).ConfigureApplicationPartManager(manager =>
                              {
                                   manager.FeatureProviders.Clear();
                                   manager.FeatureProviders.Add(new TypedControllerFeatureProvider<AdminController>());
                              }).AddApplicationPart(typeof(AdminController).Assembly);
                         services.AddIdentityMongoDbProvider<AdministratorAccount, AdministratorRole>(identityOptions =>
                         {
                              identityOptions.Password.RequiredLength = 6;
                              identityOptions.Password.RequireLowercase = false;
                              identityOptions.Password.RequireUppercase = false;
                              identityOptions.Password.RequireNonAlphanumeric = false;
                              identityOptions.Password.RequireDigit = false;
                         }, mongoIdentityOptions =>
                         {
                              mongoIdentityOptions.ConnectionString = connectionString;
                              mongoIdentityOptions.UsersCollection = nameof(AdministratorAccount);
                              mongoIdentityOptions.UseDefaultIdentity = false;
                         }).AddRoleManager<RoleManager<AdministratorRole>>().AddDefaultTokenProviders();

                         services.Configure<DataProtectionTokenProviderOptions>(dpOptions => dpOptions.TokenLifespan = TimeSpan.FromHours(3));

                         var tokenValidationParameters = accessTokenService.TokenValidationParameters;
                         services.AddAuthentication(configOptions =>
                         {
                              configOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                              configOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                         })
                              .AddJwtBearer(bearerOptions =>
                              {
                                   bearerOptions.ClaimsIssuer = tokenValidationParameters.ValidIssuer;
                                   bearerOptions.Audience = tokenValidationParameters.ValidAudience;
                                   bearerOptions.RequireHttpsMetadata = false; //true if in production
                                   bearerOptions.SaveToken = true;
                                   bearerOptions.TokenValidationParameters = tokenValidationParameters;
                                   //bearerOptions.Events = new JwtBearerEvents
                                   //{
                                   //     OnTokenValidated = async x =>
                                   //     {
                                   //          var xxx = x.Principal;
                                   //     }
                                   //};

                              });
                    },
                    appBuilder =>
                    {
                         appBuilder.UseAuthentication();
                         appBuilder.UseHttpsRedirection();
                         appBuilder.UseMvc();
                    });

               if (env.IsDevelopment())
               {
                    app.UseDeveloperExceptionPage();
               }
               else
               {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
               }

               //app.UseSwaggerUI(c =>
               //{
               //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "TypTipAdminAuth V1");
               //});
               //app.UseSwagger();
               app.UseCors("CorsPolicy");
               return app;
          }

          public static IApplicationBuilder AddUserPipeline(this IApplicationBuilder app, IHostingEnvironment env)
          {
               var configurationRoot = InitConfigBuilder(env);
               app.UseBranchWithServices("/user",
                   services =>
                   {
                        //services.AddHttpContextAccessor();
                        //services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
                        services.AddOptions();
                        services.Configure<BootstrapConfiguration>(configurationRoot.GetSection("BootstrapConfiguration"));
                        services.AddSingleton<ILoggerRepository, LoggerRepository>();
                        services.AddSingleton<ILoggerService, LoggerService>();
                        services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();
                        services.AddSingleton<IConfigurationService, ConfigurationService>();
                        services.AddSingleton<IRedisService, RedisService>();
                        services.AddScoped<IAccessTokenService, AccessTokenService>();
                        services.AddTransient<IPasswordHashService, PasswordHashService>();
                        services.AddScoped<IMongoDbMembershipContext, MongoDbMembershipContext>();
                        services.AddTransient<IUserAccountRepository, AccountRepository>();
                        services.AddTransient<IAdministratorAccountRepository, AdministratorAccountRepository>();
                        services.AddTransient<IUserGroupRepository, UserGroupRepository>();
                        services.AddTransient<IAccountService, AccountService>();
                        services.AddTransient<IUserGroupService, UserGroupService>();
                        services.AddTransient<IDbHashKeyRepository, DbHashKeyRepository>();
                        services.AddTransient<IDbHashKeyService, DbHashKeyService>();
                        services.AddTransient<INotificationService, NotificationService>();
                        var serviceProvider = services.BuildServiceProvider();
                        var loggerService = serviceProvider.GetRequiredService<ILoggerService>();
                        var bootstrapConfigOptions = serviceProvider.GetRequiredService<IOptions<BootstrapConfiguration>>();
                        var bootstrap = bootstrapConfigOptions.Value;
                        var configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
                        var accessTokenService = serviceProvider.GetRequiredService<IAccessTokenService>();
                        var mongoDbUrlTemplate = bootstrap.MongoDbConnectionStrings;
                        var sysConfig = configurationService.SystemConfiguration;
                        var mongoDbNames = sysConfig.MongoDbNames;
                        var accountsDbName = mongoDbNames.FirstOrDefault(m => m.DbType == MongoDbType.Accounts)?.DbName;
                        var connectionString = string.Format(mongoDbUrlTemplate, accountsDbName);

                        services.AddMvc(options =>
                             {
                                  options.Filters.Add(new ValidateControllerActionsFilter(loggerService));
                                  options.Filters.Add(new LogControllerExceptionsFilter(loggerService));
                             })
                             .SetCompatibilityVersion(CompatibilityVersion.Version_2_2).ConfigureApplicationPartManager(manager =>
                             {
                                  manager.FeatureProviders.Clear();
                                  manager.FeatureProviders.Add(new TypedControllerFeatureProvider<UserController>());
                             }).AddApplicationPart(typeof(UserController).Assembly);
                        services.AddIdentityMongoDbProvider<UserAccount, UserRole>(identityOptions =>
                        {
                             identityOptions.Password.RequiredLength = 6;
                             identityOptions.Password.RequireLowercase = false;
                             identityOptions.Password.RequireUppercase = false;
                             identityOptions.Password.RequireNonAlphanumeric = false;
                             identityOptions.Password.RequireDigit = false;
                        }, mongoIdentityOptions =>
                        {
                             mongoIdentityOptions.ConnectionString = connectionString;
                             mongoIdentityOptions.UsersCollection = nameof(UserAccount);
                             mongoIdentityOptions.UseDefaultIdentity = false;
                        }).AddRoleManager<RoleManager<UserRole>>().AddDefaultTokenProviders();

                        services.Configure<DataProtectionTokenProviderOptions>(dpOptions => dpOptions.TokenLifespan = TimeSpan.FromHours(3));

                        var tokenValidationParameters = accessTokenService.TokenValidationParameters;
                        services.AddAuthentication(configOptions =>
                        {
                             configOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                             configOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        })
                             .AddJwtBearer(bearerOptions =>
                             {
                                  bearerOptions.ClaimsIssuer = tokenValidationParameters.ValidIssuer;
                                  bearerOptions.Audience = tokenValidationParameters.ValidAudience;
                                  bearerOptions.RequireHttpsMetadata = false; //true if in production
                                  bearerOptions.SaveToken = true;
                                  bearerOptions.TokenValidationParameters = tokenValidationParameters;
                             });
                   },
                   appBuilder =>
                   {
                        appBuilder.UseAuthentication();
                        appBuilder.UseHttpsRedirection();
                        appBuilder.UseMvc();
                   });

               if (env.IsDevelopment())
               {
                    app.UseDeveloperExceptionPage();
               }
               else
               {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
               }

               //app.UseSwaggerUI(c =>
               //{
               //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "TypTipApiAuth V1");
               //});
               //app.UseSwagger();
               app.UseCors("CorsPolicy");
               return app;
          }

          private static IConfigurationRoot InitConfigBuilder(IHostingEnvironment env)
          {
               var appRoot = env.ContentRootPath;
               return new ConfigurationBuilder()
                    .SetBasePath(appRoot)
                    .AddJsonFile("appsettings.json", true, true)
                    .AddEnvironmentVariables()
                    .Build();
          }
     }
}