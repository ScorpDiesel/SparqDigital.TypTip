using AspNetCore.Identity.Mongo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.DbContext;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Server.Infrastructure.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Services;
using Swashbuckle.AspNetCore.Swagger;

namespace SparqDigital.TypTip.Web.UserAuthentication
{
     public class Startup
     {
          private string ConnectionString => Configuration.GetSection("ConnectionStrings")["mongodb_constring"];
          private readonly IAccessTokenService _accessTokenService;
          public Startup(IConfiguration configuration, IAccessTokenService accessTokenService)
          {

               Configuration = configuration;
               _accessTokenService = accessTokenService;
          }

          public IConfiguration Configuration { get; }

          // This method gets called by the runtime. Use this method to add services to the container.
          public void ConfigureServices(IServiceCollection services)
          {
               services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
               services.AddIdentityMongoDbProvider<UserAccount>(identityOptions =>
               {
                    identityOptions.Password.RequiredLength = 6;
                    identityOptions.Password.RequireLowercase = false;
                    identityOptions.Password.RequireUppercase = false;
                    identityOptions.Password.RequireNonAlphanumeric = false;
                    identityOptions.Password.RequireDigit = false;
               }, mongoIdentityOptions =>
               {
                    mongoIdentityOptions.ConnectionString = ConnectionString;
                    mongoIdentityOptions.UsersCollection = nameof(UserAccount);
                    mongoIdentityOptions.UseDefaultIdentity = false;
               });

               var tokenValidationParameters = _accessTokenService.TokenValidationParameters;
               services.AddAuthentication(configOptions =>
                    {
                         configOptions.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                         configOptions.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(bearerOptions =>
                    {
                         bearerOptions.ClaimsIssuer = tokenValidationParameters.ValidIssuer;
                         bearerOptions.Audience = tokenValidationParameters.ValidAudience;
                         bearerOptions.RequireHttpsMetadata = false; //true if in production
                         bearerOptions.SaveToken = true;
                         bearerOptions.TokenValidationParameters = tokenValidationParameters;
                    });
               //.AddMicrosoftAccount(mOptions => {})
               //.AddGoogle(gOptions => {})
               //.AddTwitter(tOptions => {})
               //.AddFacebook(fOptions => {})
               //.AddYahoo(yOptions => {});



               services.AddSwaggerGen(options =>
               {
                    options.SwaggerDoc("v1", new Info { Title = "TypTipAdminPanel", Version = "v1" });
               });
               //services.AddAntiforgery(options =>
               //{
               //     options.HeaderName = "X-XSRF-TOKEN";
               //});
               services.AddCors(options =>
               {
                    options.AddPolicy("CorsPolicy",
                         builder => builder
                              .AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials());
               });
               services.AddMvc();

               services.AddTransient<IPasswordHashService, PasswordHashService>();
               services.AddScoped<IMongoDbMembershipContext, MongoDbMembershipContext>();
               services.AddTransient<IUserAccountRepository, AccountRepository>();
               services.AddTransient<IAdministratorAccountRepository, AdministratorAccountRepository>();
               services.AddTransient<IUserGroupRepository, UserGroupRepository>();
               services.AddTransient<IAccountService, AccountService>();
               services.AddTransient<IUserGroupService, UserGroupService>();
               services.AddTransient<IDbHashKeyRepository, DbHashKeyRepository>();
               services.AddTransient<IDbHashKeyService, DbHashKeyService>();
          }

          // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
          public void Configure(IApplicationBuilder app, IHostingEnvironment env)
          {
               if (env.IsDevelopment())
               {
                    app.UseDeveloperExceptionPage();
               }
               else
               {
                    app.UseExceptionHandler("/Error");
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
               }

               app.UseAuthentication();
               app.UseHttpsRedirection();
               app.UseSwaggerUI(c =>
               {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TypTipAdminPanel V1");
               });
               app.UseSwagger();
               app.UseCors("CorsPolicy");
               app.UseMvc();
          }
     }
}
