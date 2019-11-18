using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace SparqDigital.TypTip.Web.Authentication.MultiPipeline
{
     public class TypedControllerFeatureProvider<TController> : ControllerFeatureProvider where TController : ControllerBase
     {
          protected override bool IsController(TypeInfo typeInfo)
          {
               if (!typeof(TController).GetTypeInfo().IsAssignableFrom(typeInfo)) return false;
               return base.IsController(typeInfo);
          }
     }
}