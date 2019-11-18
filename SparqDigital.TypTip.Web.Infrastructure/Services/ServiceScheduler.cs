using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Web.Infrastructure.Interfaces;

namespace SparqDigital.TypTip.Web.Infrastructure.Services
{
     public class ServiceScheduler : BackgroundService
     {
          private readonly ILoggerService _loggerService;
          private readonly IScheduledBackgroundService _scheduledBackgroundService;

          public ServiceScheduler(ILoggerService loggerService, IScheduledBackgroundService scheduledBackgroundService)
          {
               _loggerService = loggerService;
               _loggerService.CreateLogger<ServiceScheduler>();
               _scheduledBackgroundService = scheduledBackgroundService;
          }

          protected override async Task ExecuteAsync(CancellationToken stoppingToken)
          {
               await _loggerService.LogInformation($"Starting {nameof(ServiceScheduler)}.");

               {
                    var scheduler = new EventLoopScheduler(ts => new Thread(ts));
                    var serviceConfig = _scheduledBackgroundService.ServiceConfiguration;
                    var intervalLogoutAdmin = serviceConfig["logoutadminaccount"];
                    var intervalLogoutUser = serviceConfig["logoutuseraccount"];
                    var observableLogoutAdmin = Observable.Interval(intervalLogoutAdmin, scheduler).Select(_ => _scheduledBackgroundService.LogoutAdminAccountAsync()).Subscribe(async x => { await x; });
                    var observableLogoutUser = Observable.Interval(intervalLogoutUser, scheduler).Select(_ => _scheduledBackgroundService.LogoutUserAccountAsync()).Subscribe(async x => { await x; });
               }
          }
     }
}