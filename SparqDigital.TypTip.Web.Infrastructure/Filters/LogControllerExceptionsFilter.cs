using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using SparqDigital.TypTip.Server.Common.ExtensionMethods;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;

namespace SparqDigital.TypTip.Web.Infrastructure.Filters
{
     public class LogControllerExceptionsFilter : ExceptionFilterAttribute
     {
          private readonly ILoggerService _loggerService;

          public LogControllerExceptionsFilter(ILoggerService loggerService)
          {
               _loggerService = loggerService;
          }

          public override async Task OnExceptionAsync(ExceptionContext context)
          {
               var details = context.HttpContext.GetLoggingHttpContextDetails();
               var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
               var actionName = descriptor?.ActionName;
               var controllerType = descriptor?.ControllerTypeInfo.UnderlyingSystemType;
               _loggerService.CreateLogger(controllerType, actionName);
               await _loggerService.LogError(context.Exception, details);
          }
     }
}