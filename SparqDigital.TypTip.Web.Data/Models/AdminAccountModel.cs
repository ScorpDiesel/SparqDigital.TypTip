using System;
using System.Collections.Generic;
using AspNetCore.Identity.Mongo.Model;
using Microsoft.AspNetCore.Identity;

namespace SparqDigital.TypTip.Web.Data.Models
{
     public class AdminAccountModel
     {
          public string Id { get; set; }
          public string Name { get; set; }
          public string Ip { get; set; }
          public bool IsOnline { get; set; }
          public bool ForceLogout { get; set; }
          public DateTime SignupDate { get; set; }
          public DateTime? LastLoginDate { get; set; }
          public bool IsAccountDisabled { get; set; }
          public DateTime? AccountDisabledDate { get; set; }
          public string AccountDisabledReason { get; set; }
          public DateTime? AccountLockoutEndDate { get; set; }
          public string AuthenticatorKey { get; set; }
          public List<string> Roles { get; set; }
          public List<IdentityUserClaim<string>> Claims { get; set; }
          public List<IdentityUserLogin<string>> Logins { get; set; }
          public List<IdentityUserToken<string>> Tokens { get; set; }
          public List<TwoFactorRecoveryCode> RecoveryCodes { get; set; }
     }
}