using MongoDB.Bson;

namespace SparqDigital.TypTip.Web.Data.Models
{
     public class GroupHashKeyModel
     {
          public string Id { get; set; }
          public string CurrentSystemGroupKey { get; set; }
          public string CurrentUserGroupKey { get; set; }
          public bool IsLocked { get; set; }
     }
}