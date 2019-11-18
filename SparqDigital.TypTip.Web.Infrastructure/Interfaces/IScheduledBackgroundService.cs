using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SparqDigital.TypTip.Web.Infrastructure.Interfaces
{
     public interface IScheduledBackgroundService
     {
          Dictionary<string, TimeSpan> ServiceConfiguration { get; }
          Task LogoutAdminAccountAsync();
          Task LogoutUserAccountAsync();
     }
}