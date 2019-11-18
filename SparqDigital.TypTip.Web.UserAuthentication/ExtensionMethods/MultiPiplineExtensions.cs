using AspNetCore.Identity.Mongo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SparqDigital.TypTip.Server.Data.Entities;
using SparqDigital.TypTip.Server.Infrastructure.DbContext;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Interfaces.Services;
using SparqDigital.TypTip.Server.Infrastructure.Repositories;
using SparqDigital.TypTip.Server.Infrastructure.Services;
using SparqDigital.TypTip.Web.UserAuthentication.MultiPipeline;
using Swashbuckle.AspNetCore.Swagger;
using WebApiContrib.Core;

namespace SparqDigital.TypTip.Web.UserAuthentication.ExtensionMethods
{
     public static class MultiPiplineExtensions
     {
          public static IApplicationBuilder AdminPipelineBuilder(this IApplicationBuilder app, string ConnectionString, IAccessTokenService accessTokenService)
          {

               app.UseBranchWithServices("/admin",
                    services =>
                    {
                         services.AddCors(options =>
                         {
                              options.AddPolicy("CorsPolicy",
                                   builder => builder
                                        .AllowAnyOrigin()
                                        .AllowAnyMethod()
                                        .AllowAnyHeader()
                                        .AllowCredentials());
                         });
                         services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                              .ConfigureApplicationPartManager(manager =>
                              {
                                   manager.FeatureProviders.Clear();
                                   manager.FeatureProviders.Add(new TypedControllerFeatureProvider<AdminController>());
                              });
                         services.AddIdentityMongoDbProvider<AdministratorAccount>(identityOptions =>
                         {
                              identityOptions.Password.RequiredLength = 6;
                              identityOptions.Password.RequireLowercase = false;
                              identityOptions.Password.RequireUppercase = false;
                              identityOptions.Password.RequireNonAlphanumeric = false;
                              identityOptions.Password.RequireDigit = false;
                         }, mongoIdentityOptions =>
                         {
                              mongoIdentityOptions.ConnectionString = ConnectionString;
                              mongoIdentityOptions.UsersCollection = nameof(AdministratorAccount);
                              mongoIdentityOptions.UseDefaultIdentity = false;
                         });

                         var tokenValidationParameters = accessTokenService.TokenValidationParameters;
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

                         services.AddSwaggerGen(options =>
                         {
                              options.SwaggerDoc("v1", new Info { Title = "TypTipAdminPanel", Version = "v1" });
                         });
                         //services.AddAntiforgery(options =>
                         //{
                         //     options.HeaderName = "X-XSRF-TOKEN";
                         //});

                         services.AddTransient<IPasswordHashService, PasswordHashService>();
                         services.AddScoped<IMongoDbMembershipContext, MongoDbMembershipContext>();
                         services.AddTransient<IUserAccountRepository, AccountRepository>();
                         services.AddTransient<IAdministratorAccountRepository, AdministratorAccountRepository>();
                         services.AddTransient<IUserGroupRepository, UserGroupRepository>();
                         services.AddTransient<IAccountService, AccountService>();
                         services.AddTransient<IUserGroupService, UserGroupService>();
                         services.AddTransient<IDbHashKeyRepository, DbHashKeyRepository>();
                         services.AddTransient<IDbHashKeyService, DbHashKeyService>();
                    },
                    appBuilder =>
                    {
                         appBuilder.UseMvc();
                         app.UseAuthentication();
                         app.UseHttpsRedirection();
                         app.UseSwaggerUI(c =>
                         {
                              c.SwaggerEndpoint("/swagger/v1/swagger.json", "TypTipAdminPanel V1");
                         });
                         app.UseSwagger();
                         app.UseCors("CorsPolicy");
                    });
               return app;
          }

          public static IApplicationBuilder ApiPipelineBuilder(this IApplicationBuilder app, string ConnectionString, IAccessTokenService accessTokenService)
          {
               app.UseBranchWithServices("/api",
                                   services =>
                                   {
                                        services.AddCors(options =>
                                        {
                                             options.AddPolicy("CorsPolicy",
                                                  builder => builder
                                                       .AllowAnyOrigin()
                                                       .AllowAnyMethod()
                                                       .AllowAnyHeader()
                                                       .AllowCredentials());
                                        });
                                        services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                                             .ConfigureApplicationPartManager(manager =>
                                             {
                                                  manager.FeatureProviders.Clear();
                                                  manager.FeatureProviders.Add(new TypedControllerFeatureProvider<UserController>());
                                             });
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

                                        var tokenValidationParameters = accessTokenService.TokenValidationParameters;
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

                                        services.AddSwaggerGen(options =>
                                        {
                                             options.SwaggerDoc("v1", new Info { Title = "User", Version = "v1" });
                                        });
                                        //services.AddAntiforgery(options =>
                                        //{
                                        //     options.HeaderName = "X-XSRF-TOKEN";
                                        //});

                                        services.AddTransient<IPasswordHashService, PasswordHashService>();
                                        services.AddScoped<IMongoDbMembershipContext, MongoDbMembershipContext>();
                                        services.AddTransient<IUserAccountRepository, AccountRepository>();
                                        services.AddTransient<IAdministratorAccountRepository, AdministratorAccountRepository>();
                                        services.AddTransient<IUserGroupRepository, UserGroupRepository>();
                                        services.AddTransient<IAccountService, AccountService>();
                                        services.AddTransient<IUserGroupService, UserGroupService>();
                                        services.AddTransient<IDbHashKeyRepository, DbHashKeyRepository>();
                                        services.AddTransient<IDbHashKeyService, DbHashKeyService>();
                                   },
                                   appBuilder =>
                                   {
                                        appBuilder.UseMvc();
                                        app.UseAuthentication();
                                        app.UseHttpsRedirection();
                                        app.UseSwaggerUI(c =>
                                        {
                                             c.SwaggerEndpoint("/swagger/v1/swagger.json", "User V1");
                                        });
                                        app.UseSwagger();
                                        app.UseCors("CorsPolicy");

                                   });
               return app;
          }
     }
}