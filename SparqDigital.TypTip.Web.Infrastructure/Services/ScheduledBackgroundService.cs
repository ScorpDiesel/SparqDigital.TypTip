using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Web.Data.Services;
using SparqDigital.TypTip.Web.Infrastructure.Interfaces;
using StackExchange.Redis;
using StackExchange.Redis.Extensions;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Newtonsoft;

namespace SparqDigital.TypTip.Web.Infrastructure.Services
{
     public class ScheduledBackgroundService : IScheduledBackgroundService
     {
          private readonly IAccountService _accountService;
          private readonly ILoggerService _loggerService;
          private readonly IDatabase _database;
          private readonly StackExchangeRedisCacheClient _cacheClient;
          private readonly string _adminTokenPrefix;
          private readonly string _userTokenPrefix;
          public ScheduledBackgroundService(ILoggerService loggerService, IConfigurationService configurationService, 
               IAccountService accountService, IRedisService redisService)
          {
               _loggerService = loggerService;
               _loggerService.CreateLogger<ScheduledBackgroundService>();
               _accountService = accountService;
               _database = redisService.RedisCache;
               var serializer = new NewtonsoftSerializer();
               _cacheClient = new StackExchangeRedisCacheClient(_database.Multiplexer, serializer);
               var sysConfig = configurationService.SystemConfiguration;
               var redisConfig = sysConfig.RedisOptions;
               _adminTokenPrefix = redisConfig.AdminTokenPrefix;
               _userTokenPrefix = redisConfig.UserTokenPrefix;
               ServiceConfiguration = new Dictionary<string, TimeSpan>();
               var schedulerOptions = sysConfig.SchedulerOptions;
               var services = schedulerOptions.Services;
               foreach (var config in services)
               {
                    var serviceName = config.Name.ToLower();
                    var interval = Convert.ToDouble(config.Interval);
                    TimeSpan intervalType;
                    switch (config.IntervalType.ToLower())
                    {
                         case "seconds":
                              intervalType = TimeSpan.FromSeconds(interval);
                              break;
                         case "minutes":
                              intervalType = TimeSpan.FromMinutes(interval);
                              break;
                         case "hours":
                              intervalType = TimeSpan.FromHours(interval);
                              break;
                         case "days":
                              intervalType = TimeSpan.FromDays(interval);
                              break;
                    }

                    ServiceConfiguration[serviceName] = intervalType;
               }
          }

          public Dictionary<string, TimeSpan> ServiceConfiguration { get; }

          public async Task LogoutAdminAccountAsync()
          {
               var enumerable = await _accountService.GetOnlineAccountsAsync<AdministratorAccount>();
               var accounts = enumerable as AdministratorAccount[] ?? enumerable.ToArray();
               if (accounts.Length != 0)
               {
                    foreach (var account in accounts)
                    {
                         var token = await _database.StringGetAsync($"{_adminTokenPrefix}:{account.Id}");
                         if (!string.IsNullOrEmpty(token.ToString())) continue;
                         account.IsOnline = false;
                         var isUpdated = await _accountService.UpdateEntityAsync(account);
                         await _loggerService.LogInformation("Admin token expired so was logged out.",
                              new {AccountId = account.Id});
                    }
               }

               await Task.CompletedTask;
          }

          public async Task LogoutUserAccountAsync()
          {
               var enumerable = await _accountService.GetOnlineAccountsAsync<UserAccount>();
               var accounts = enumerable as UserAccount[] ?? enumerable.ToArray();
               if (accounts.Length != 0)
               {
                    foreach (var account in accounts)
                    {
                         var userKeys = await _cacheClient.SearchKeysAsync($"{_userTokenPrefix}:{account.Id}*");
                         if (userKeys.ToArray().Length != 0) continue;
                         account.IsOnline = false;
                         var isUpdated = await _accountService.UpdateEntityAsync(account);
                         await _loggerService.LogInformation("User token expired so was logged out.",
                              new { AccountId = account.Id });
                    }
               }

               await Task.CompletedTask;
          }
     }
}