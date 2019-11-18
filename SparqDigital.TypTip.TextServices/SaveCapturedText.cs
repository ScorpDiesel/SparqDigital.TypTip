using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SparqDigital.TypTip.Core.Data.Dtos;
using SparqDigital.TypTip.Server.Common.ExtensionMethods;
using SparqDigital.TypTip.Server.Infrastructure;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;

using SparqDigital.TypTip.Server.Infrastructure.Services;
using StackExchange.Redis;
using Willezone.Azure.WebJobs.Extensions.DependencyInjection;

namespace SparqDigital.TypTip.TextServices
{
     public static class SaveCapturedText
     {
          [FunctionName("SaveCapturedText")]
          public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "text/save")] HttpRequest req, ILogger log, ExecutionContext context, 
               [Inject]ITextTransceiverService textTransceiverService, [Inject]IAccessTokenService accessTokenService)
          {
               try
               {
                    var token = req.GetAccessToken();
                    if (string.IsNullOrEmpty(token)) return new UnauthorizedResult();
                    var identity = await accessTokenService.GetIdentityAsync(token);
                    if (identity == null) return new BadRequestResult();
                    if (!identity.IsAuthenticated) return new UnauthorizedResult();
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var dto = JsonConvert.DeserializeObject<CaptureTextDto>(requestBody);
                    //var scriptFile = GetScriptFile(context, scriptIncrementTextScore);
                    //textTransceiverService.ScriptFile = scriptFile;
                    var isTextCaptureSuccess = await textTransceiverService.SaveCapturedTextAsync(dto);
                    return isTextCaptureSuccess ? (ActionResult)new OkObjectResult(null) : new BadRequestResult();
               }
               catch (Exception ex)
               {
                    log.LogInformation(ex.Message);
                    return new BadRequestErrorMessageResult(ex.Message);
               }
          }

          private static string GetScriptFile(ExecutionContext ctx, string fileName)
          {
               if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName), "Cannot be null or empty");
               var filePath = $"{Directory.GetParent(ctx.FunctionDirectory).FullName}\\LuaScripts\\{fileName}";
               var scriptFile = File.ReadAllText(filePath);
               if (string.IsNullOrEmpty(scriptFile)) throw new ArgumentException("Script file does not exist");
               return scriptFile;
          }
     }
}
