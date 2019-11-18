using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SparqDigital.TypTip.Server.Data.Entities;

namespace SparqDigital.TypTip.Web.Data.Models
{
     public class SystemConfigurationModel
     {
          public string Id { get; set; }
          public Dictionary<string, string> ConnectionStrings { get; set; }
          public IEnumerable<MongoDbName> MongoDbNames { get; set; }
          public JwtOptions JwtOptions { get; set; }
          public RedisOptions RedisOptions { get; set; }
          public SslOptions SslOptions { get; set; }
          public UserOptions UserOptions { get; set; }
          public SchedulerOptions SchedulerOptions { get; set; }
          public NotificationOptions NotificationOptions { get; set; }
     }
}