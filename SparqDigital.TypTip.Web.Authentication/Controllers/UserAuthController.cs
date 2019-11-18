using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SparqDigital.TypTip.Core.Data.Dtos;
using SparqDigital.TypTip.Core.Data.Entities;
using SparqDigital.TypTip.Core.Data.Models;
using SparqDigital.TypTip.Core.Data.Structs;
using SparqDigital.TypTip.Server.Common.ExtensionMethods;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Web.Authentication.MultiPipeline;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace SparqDigital.TypTip.Web.Authentication.Controllers
{
     [Authorize]
     [Route("auth")]
     [ApiController]
     public class UserAuthController : UserController
     {
          private readonly UserManager<UserAccount> _userManager;
          private readonly SignInManager<UserAccount> _signInManager;
          private readonly IAccountService _accountService;
          private readonly IAccessTokenService _accessTokenService;
          private readonly INotificationService _notificationService;
          private readonly ILoggerService _loggerService;

          public UserAuthController(IAccountService accountService, ILoggerFactory loggerFactory, IAccessTokenService accessTokenService, ILoggerService loggerService, INotificationService notificationService,
               UserManager<UserAccount> userManager, SignInManager<UserAccount> signInManager)
          {
               _accountService = accountService;
               _accessTokenService = accessTokenService;
               _notificationService = notificationService;
               _loggerService = loggerService.CreateLogger(this);
               _signInManager = signInManager;
               _userManager = userManager;
          }

          // POST: user/auth/register
          [HttpPost]
          [Route("register")]
          [AllowAnonymous]
          public async Task<IActionResult> Register(RegisterUserModel model)
          {
               var isValid = await _accountService.IsEmailValidAsync<UserAccount>(model.Email);
               if (isValid) return BadRequest(new { Message = "Email address unavailable." });
               var account = await _accountService.CreateEntityAsync<UserAccount>(model);
               var result = await _userManager.CreateAsync(account, model.Password);
               if (!result.Succeeded) return BadRequest(result.Errors);
               var token = await _userManager.GenerateEmailConfirmationTokenAsync(account);
               await _notificationService.SendEmailConfirmationLink(new NotificationDto { Email = model.Email, Name = model.Name, Token = token });
               return Ok(new { Message = "Created new user account." }); //Use anonymous object to avoid mongodb "_t" type discriminator
          }

          // POST: user/auth/login
          [HttpPost]
          [AllowAnonymous]
          [Route("login")]
          //[ValidateAntiForgeryToken]
          public async Task<IActionResult> Login(UserLoginModel model)
          {
               var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, lockoutOnFailure: true);
               if (result == SignInResult.Failed) return BadRequest(new { Message = "Email or password is incorrect." });
               if (result == SignInResult.LockedOut) return BadRequest(new { Message = "Account locked out." });
               if (result == SignInResult.NotAllowed) return BadRequest(new { Message = "Access denied." });

               var account = await _accountService.GetAccountByEmailAsync<UserAccount>(model.Email);
               var remoteAddress = HttpContext.Connection.RemoteIpAddress.ToString();
               account.Ip = remoteAddress;
               account.IsOnline = true;
               account.LastLoginDate = DateTime.UtcNow;
               await _accountService.UpdateEntityAsync(account);
               var newToken = await _accessTokenService.GenerateTokenAsync(account);
               var isSuccess = await _accessTokenService.SaveTokenAsync<UserAccount>(newToken, account.Id);
               return Ok(new { Message = "User log in.", AuthResult = new AuthResult {AccessToken = newToken, Key = account.User.UserKey }});
          }

          // POST: user/auth/logout
          [HttpPost]
          [Route("logout")]
          public async Task<IActionResult> Logout()
          {
               var token = Request.GetAccessToken();
               var claims = HttpContext.User.Claims;
               var id = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
               var enumerable = await _accountService.GetEntityAsync<UserAccount>(id);
               if (!(enumerable.FirstOrDefault() is UserAccount account))
               {
                    await _loggerService.LogInformation("Account not found.");
                    return NotFound();
               }

               var accessToken = new AccessToken { Token = token };
               var isValid = await _accessTokenService.IsTokenValidAsync(accessToken, id);
               if (!isValid) throw new SecurityTokenValidationException("Invalid token.");
               var isSuccess = await _accessTokenService.DeleteTokenAsync(accessToken, id);
               account.IsOnline = false;
               await _accountService.UpdateEntityAsync(account);
               await _signInManager.SignOutAsync();
               await _loggerService.LogInformation("User log out.");
               return Ok();
          }

          [HttpGet]
          [AllowAnonymous]
          [Route("email/confirm")]
          public async Task<IActionResult> ConfirmEmail(string u, string t)
          {
               if (u == null || t == null)
               {
                    await _loggerService.LogInformation("Confirm Email failed.");
                    return BadRequest();
               }

               var user = await _userManager.FindByIdAsync(u);
               if (user == null)
               {
                    await _loggerService.LogInformation($"User not found. id={u}.");
                    return BadRequest();
               }

               var result = await _userManager.ConfirmEmailAsync(user, t);
               if (!result.Succeeded)
               {
                    return BadRequest(new { Message = "Confirm Email failed." });
               }

               return Ok();
          }

          [HttpPost]
          [AllowAnonymous]
          //[ValidateAntiForgeryToken]
          [Route("password/forgot")]
          public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
          {
               var user = await _userManager.FindByEmailAsync(model.Email);
               if (user == null || user.IsAccountDisabled || !await _userManager.IsEmailConfirmedAsync(user))
               {
                    //await _loggerService.Information();
                    // Don't reveal that the user does not exist
                    return BadRequest("User not found.");
               }
               var token = await _userManager.GeneratePasswordResetTokenAsync(user);
               var link = string.Empty;
               await _notificationService.SendPasswordRecoveryLink(new NotificationDto { Email = user.Email, Name = user.Name, Token = token });
               await _loggerService.LogInformation($"Password reset link sent. link={ link }");
               return Ok();
          }

          [HttpPost]
          [AllowAnonymous]
          //[ValidateAntiForgeryToken]
          [Route("password/reset")]
          public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
          {
               var user = await _userManager.FindByEmailAsync(model.Email);
               if (user == null)
               {
                    //await _loggerService.Information();
                    // Don't reveal that the user does not exist
                    return BadRequest("User not found.");
               }

               var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
               if (!result.Succeeded)
               {
                    await _loggerService.LogInformation("Password reset failure.");
                    return BadRequest(new { Message = "Password reset failure." });
               }

               await _loggerService.LogInformation("Password reset success.");
               return Ok();
          }
     }
}
