using MongoDB.Bson;

namespace SparqDigital.TypTip.Web.Data.Models
{
     public class UserHashKeyModel
     {
          public string Id { get; set; }
          public string CurrentUserKey { get; set; }
          public bool IsLocked { get; set; }
     }
}