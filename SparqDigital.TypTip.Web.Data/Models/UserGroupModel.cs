using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SparqDigital.TypTip.Web.Data.Models
{
     public class UserGroupModel
     {
          [BsonId]
          [BsonRepresentation(BsonType.ObjectId)]
          public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
          public string Name { get; set; }
          public string Description { get; set; }

          [BsonRepresentation(BsonType.DateTime)]
          public DateTime CreationDate { get; set; }
          public string GroupKey { get; set; }
     }
}