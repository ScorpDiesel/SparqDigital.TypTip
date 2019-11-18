using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace SparqDigital.AutoJest.Server.Functions
{
     public static class EndPointKeepWarm
     {
          private static readonly HttpClient HttpClient = new HttpClient();
          private static readonly string EndPointsToHit = Environment.GetEnvironmentVariable("EndPointUrls");

          [FunctionName("EndPointKeepWarm")]
          // run every 5 minutes
          public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo timerInfoimer, ILogger log)
          {
               log.LogInformation($"Run(): EndPointKeepWarm function executed at: {DateTime.Now}. Past due? {timerInfoimer.IsPastDue}");

               if (!string.IsNullOrEmpty(EndPointsToHit))
               {
                    var endPoints = EndPointsToHit.Split(';');
                    foreach (var endPoint in endPoints)
                    {
                         var tidiedUrl = endPoint.Trim();
                         log.LogInformation($"Run(): About to hit URL: '{tidiedUrl}'");

                         await HitUrl(tidiedUrl, log);      
                    }
               }
               else
               {
                    log.LogError($"Run(): No URLs specified in environment variable 'EndPointUrls'. Expected a single URL or multiple URLs " +
                        "separated with a semi-colon (;). Please add this config to use the tool.");
               }

               log.LogInformation($"Run(): Completed..");
          }

          private static async Task<HttpResponseMessage> HitUrl(string url, ILogger log)
          {
               var stringContent = new StringContent(string.Empty, Encoding.UTF8, "application/json");
               var response = await HttpClient.PostAsync(url, stringContent);
               if (response.IsSuccessStatusCode)
               {
                    log.LogInformation($"hitUrl(): Successfully hit URL: '{url}'");
               }
               else
               {
                    log.LogError($"hitUrl(): Failed to hit URL: '{url}'. Response: {(int)response.StatusCode + " : " + response.ReasonPhrase}");
               }

               return response;
          }
     }
}
