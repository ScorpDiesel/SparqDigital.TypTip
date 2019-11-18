using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SparqDigital.TypTip.Core.Data.Dtos;
using SparqDigital.TypTip.Core.Data.Structs;
using SparqDigital.TypTip.Server.Common.ExtensionMethods;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;

namespace SparqDigital.TypTip.Web.UserAuthentication.Controllers
{
     [Authorize]
     [Route("api/auth")]
     [ApiController]
     public class UserAuthController : ControllerBase
     {
          private readonly UserManager<UserAccount> _userManager;
          private readonly SignInManager<UserAccount> _signInManager;
          private readonly IAccountService _accountService;
          private readonly IAccessTokenService _accessTokenService;
          private readonly ILogger _logger;

          public UserAuthController(IAccountService accountService, ILoggerFactory loggerFactory, IAccessTokenService accessTokenService,
               UserManager<UserAccount> userManager, SignInManager<UserAccount> signInManager)
          {
               _accountService = accountService;
               _accessTokenService = accessTokenService;
               _logger = loggerFactory.CreateLogger<UserAuthController>();
               _signInManager = signInManager;
               _userManager = userManager;
          }

          // GET: api/auth
          [HttpGet]
          public async Task<IEnumerable<string>> Get() //for testing
          {
               var token = Request.GetAccessToken();
               var claims = HttpContext.User.Claims;
               var idClaim = claims.FirstOrDefault(c => c.Type == "Id");
               if (idClaim == null) return null; //put something here
               var id = idClaim.Value;
               var accessToken = new AccessToken { Token = token };
               var isValid = await _accessTokenService.IsTokenValidAsync(accessToken, id, true);
               return new [] { "value1", "value2" };
          }

          // POST: api/auth/register
          [HttpPost]
          [Route("register")]
          [AllowAnonymous]
          public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
          {
               if (!ModelState.IsValid) return BadRequest(ModelState);
               try
               {
                    var userAccount = await _accountService.CreateEntityAsync<UserAccount>(dto);
                    var result = await _userManager.CreateAsync(userAccount, dto.Password);
                    if (result.Succeeded)
                    {
                         _logger.LogInformation($"Created a new user account: '{userAccount.Email}' at {DateTime.UtcNow:dddd, dd MMMM yyyy HH:mm tt}");
                         var code = await _userManager.GenerateEmailConfirmationTokenAsync(userAccount);
                         /* Call SendGrid service to send email confirmation link*/
                         return Ok();
                    }
               }
               catch (Exception ex)
               {
                    _logger.LogError(ex.Message);
                    return BadRequest(ex);
               }

               return BadRequest("Signup failed.");
          }

          // POST: api/auth/login
          [HttpPost]
          [AllowAnonymous]
          [Route("login")]
          //[ValidateAntiForgeryToken]
          public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
          {
               if (!ModelState.IsValid) return BadRequest(ModelState);
               try
               {
                    //var token = Request.GetAccessToken();
                    //if (string.IsNullOrEmpty(token)) return new UnauthorizedResult();

                    var result = await _signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                         _logger.LogInformation($"User logged in at {DateTime.UtcNow:dddd, dd MMMM yyyy HH:mm tt}");
                         var remoteAddress = HttpContext.Connection.RemoteIpAddress.ToString();
                         var account = await _accountService.GetAccountByEmailAsync<UserAccount>(dto.Email);
                         account.Ip = remoteAddress;
                         account.IsOnline = true;
                         account.LastLoginDate = DateTime.UtcNow;
                         await _accountService.UpdateEntityAsync(account);
                         var newToken = await _accessTokenService.GenerateTokenAsync(account.Id);
                         var isSuccess = await _accessTokenService.SaveTokenAsync(newToken, account.Id, true);
                         return new OkObjectResult(newToken);
                    }
                    if (result.RequiresTwoFactor)
                    {
                         //return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                    }
                    if (result.IsLockedOut)
                    {
                         _logger.LogWarning("User account locked out.");
                         //return RedirectToAction(nameof(Lockout));
                    }

               }
               catch (Exception ex)
               {
                    _logger.LogInformation(ex.Message);
                    return BadRequest(ex);
               }

               return BadRequest("Incorrect Email or Passord.");
          }

          // POST: api/auth/logout
          [HttpPost]
          [Route("logout")]
          public async Task<IActionResult> Logout()
          {
               if (!ModelState.IsValid) return BadRequest(ModelState);
               try
               {
                    var token = Request.GetAccessToken();
                    var claims = HttpContext.User.Claims;
                    var idClaim = claims.FirstOrDefault(c => c.Type == "Id");
                    if (idClaim == null) return null; //put something here
                    var id = idClaim.Value;
                    var enumerable = await _accountService.GetEntityAsync<UserAccount>(id);
                    if (!(enumerable.FirstOrDefault() is UserAccount account)) return null; //put something here
                    var accessToken = new AccessToken { Token = token };
                    var isSuccess = await _accessTokenService.DeleteTokenAsync(accessToken, id, true);
                    account.IsOnline = false;
                    await _accountService.UpdateEntityAsync(account);
                    await _signInManager.SignOutAsync();
                    _logger.LogInformation($"User '{account.Email}' logged out.");
                    return new OkResult();
               }
               catch (Exception ex)
               {
                    _logger.LogInformation(ex.Message);
                    return BadRequest(ex.Message);
               }
          }

          [HttpGet]
          [AllowAnonymous]
          [Route("confirmemail")]
          public async Task<IActionResult> ConfirmEmail(string userId, string code)
          {
               if (userId == null || code == null)
               {
                    return new BadRequestResult();
               }
               var user = await _userManager.FindByIdAsync(userId);
               if (user == null)
               {
                    throw new ApplicationException($"Unable to find user with ID '{userId}'.");
               }
               var result = await _userManager.ConfirmEmailAsync(user, code);
               return result.Succeeded ? (IActionResult)new OkResult() : new BadRequestResult();
          }

          [HttpPost]
          [AllowAnonymous]
          //[ValidateAntiForgeryToken]
          [Route("password/forgot")]
          public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
          {
               if (!ModelState.IsValid) return BadRequest(ModelState);
               var user = await _userManager.FindByEmailAsync(dto.Email);
               if (user == null || user.IsAccountDisabled || !await _userManager.IsEmailConfirmedAsync(user)) return BadRequest(ModelState);

               /* Call SendGrid service to email password reset link*/
               return null;
          }

          [HttpPost]
          [AllowAnonymous]
          //[ValidateAntiForgeryToken]
          [Route("password/reset")]
          public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
          {
               if (!ModelState.IsValid) return BadRequest(ModelState);
               var user = await _userManager.FindByEmailAsync(dto.Email);
               if (user == null)
               {
                    // Don't reveal that the user does not exist
                    return BadRequest();
               }

               var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
               if (!isPasswordValid) return BadRequest("Incorrect password.");
               var result = await _userManager.ResetPasswordAsync(user, dto.Code, dto.NewPassword);
               if (result.Succeeded) return new OkResult();
               return BadRequest("Could not reset password.");
          }
     }
}
