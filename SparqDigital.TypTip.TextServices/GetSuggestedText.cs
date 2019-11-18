using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SparqDigital.TypTip.Core.Common.Constants;
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
     public static class GetSuggestedText
     {
          [FunctionName("GetSuggestedText")]
          public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "text/get")] HttpRequest req, ILogger log, ExecutionContext context, 
               [Inject]ITextTransceiverService textTransceiverService, [Inject]IAccessTokenService accessTokenService)
          {
               try
               {
                    var token = req.GetAccessToken();
                    if (string.IsNullOrEmpty(token)) return new BadRequestErrorMessageResult("Token is null");
                    var identity = await accessTokenService.GetIdentityAsync(token);
                    if (identity == null) return new BadRequestErrorMessageResult("Identity is null");
                    if (!identity.IsAuthenticated) return new UnauthorizedResult();
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    var dto = JsonConvert.DeserializeObject<QuerySuggestionTextDto>(requestBody);
                    log.LogInformation($"got: [{dto.UserKey}:{string.Join(" ", dto.Text)}]");
                    var scriptFile = GetScriptFile(context, LuaScripts.FindSuggestionText);
                    textTransceiverService.ScriptFile = scriptFile;
                    var suggestionText = await textTransceiverService.GetSuggestedTextAsync(dto);
                    var json = string.Empty;
                    if (suggestionText != null && suggestionText.Any())
                    {
                         log.LogInformation($"sending count: [{string.Join(" ", suggestionText.Count())}]");
                         json = JsonConvert.SerializeObject(suggestionText);
                    }

                    return new OkObjectResult(json);
               }
               catch (SecurityTokenExpiredException ex)
               {
                    log.LogInformation(ex.Message);
                    return new UnauthorizedResult();
               }
               catch (Exception ex)
               {
                    log.LogInformation($"{ex.Message}\n{ex.StackTrace}");
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
