using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using SparqDigital.TypTip.Core.Common.ExtensionMethods;
using SparqDigital.TypTip.Core.Data.Interfaces;
using SparqDigital.TypTip.Server.Common.ExtensionMethods;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;

namespace SparqDigital.TypTip.Web.Infrastructure.Filters
{
     public class ValidateControllerActionsFilter : ActionFilterAttribute
     {
          private readonly ILoggerService _loggerService;

          public ValidateControllerActionsFilter(ILoggerService loggerService)
          {
               _loggerService = loggerService;
          }

          public override async Task OnResultExecutionAsync(ResultExecutingContext context,
               ResultExecutionDelegate next)
          {
               var details = context.HttpContext.GetLoggingHttpContextDetails();
               var controller = context.Controller;
               var actionName = ((ControllerBase)controller).ControllerContext.ActionDescriptor.ActionName;
               _loggerService.CreateLogger(controller, actionName);

               if (context.Result is StatusCodeResult statusCodeResult) await _loggerService.LogInformation(new { StatusCode = statusCodeResult.StatusCode, Result = (object)null }, details);
               else
               {
                    int? statusCode;
                    var result = (ObjectResult)context.Result;
                    if (context.ModelState.IsValid)
                    {
                         if (result != null)
                         {
                              statusCode = result.StatusCode;
                              if (!(result.Value is IEnumerable<object>))
                              {
                                   await _loggerService.LogInformation(new { StatusCode = statusCode, Result = result.Value },
                                        details);
                              }
                              else
                              {
                                   var valueObject = (IEnumerable<object>)result.Value;
                                   var stringArray = valueObject.Flatten();
                                   await _loggerService.LogInformation(new { StatusCode = statusCode, Result = stringArray },
                                        details);
                              }
                         }
                    }
                    else
                    {
                         context.Result = new BadRequestObjectResult(context.ModelState);
                         result = (ObjectResult)context.Result;
                         if (result != null)
                         {
                              statusCode = result.StatusCode;
                              object[] valueObjectsArray;
                              if (result.Value is string)
                              {
                                   valueObjectsArray = new object[] { new[] { result.Value } };
                              }
                              else
                              {
                                   var dict = (Dictionary<string, object>)result.Value;
                                   valueObjectsArray = dict.Values.ToArray();
                              }

                              var valuesList = new List<string>();
                              foreach (string[] value in valueObjectsArray)
                              {
                                   valuesList.AddRange(value);
                              }

                              await _loggerService.LogInformation(new { StatusCode = statusCode, Result = valuesList },
                                   details);
                         }
                    }
               }
               //if (result?.StatusCode == StatusCodes.Status401Unauthorized || result?.StatusCode == StatusCodes.Status200OK)
               //{
               //     /* Message for these status codes are used only for logging purposes.  So, remove message sent to client. */
               //     if (!(result.Value is IEnumerable<object>))
               //     {
               //          result.Value = null;
               //     }
               //     else
               //     {
               //          var valueObject = (IEnumerable<object>)result.Value;
               //          valueObject.ToArray()[0] = null;
               //          result.Value = valueObject;
               //     }
               //     context.Result = result;
               //}

               // the actual action
               await next();

               // logic after the action goes here
          }
     }
}