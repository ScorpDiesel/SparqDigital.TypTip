using System;
using AspNetCore.Identity.Mongo;
using AspNetCore.Identity.Mongo.Collections;
using AspNetCore.Identity.Mongo.Model;
using AspNetCore.Identity.Mongo.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SparqDigital.TypTip.Web.UserAuthentication.ExtensionMethods
{
     public static class MongoIdentityExtensions
     {
          public static IdentityBuilder AddAdditionalIdentityMongoDbProvider<TUser>(this IServiceCollection services) where TUser : MongoUser
          {
               return AddAdditionalIdentityMongoDbProvider<TUser, MongoRole>(services, x => { });
          }

          public static IdentityBuilder AddAdditionalIdentityMongoDbProvider<TUser>(this IServiceCollection services,
               Action<MongoIdentityOptions> setupDatabaseAction) where TUser : MongoUser
          {
               return AddAdditionalIdentityMongoDbProvider<TUser, MongoRole>(services, setupDatabaseAction);
          }

          public static IdentityBuilder AddAdditionalIdentityMongoDbProvider<TUser, TRole>(this IServiceCollection services,
                 Action<MongoIdentityOptions> setupDatabaseAction) where TUser : MongoUser
                 where TRole : MongoRole
          {
               return AddAdditionalIdentityMongoDbProvider<TUser, TRole>(services, x => { }, setupDatabaseAction);
          }

          public static IdentityBuilder AddAdditionalIdentityMongoDbProvider(this IServiceCollection services,
              Action<IdentityOptions> setupIdentityAction, Action<MongoIdentityOptions> setupDatabaseAction)
          {
               return AddAdditionalIdentityMongoDbProvider<MongoUser, MongoRole>(services, setupIdentityAction, setupDatabaseAction);
          }

          public static IdentityBuilder AddAdditionalIdentityMongoDbProvider<TUser>(this IServiceCollection services,
              Action<IdentityOptions> setupIdentityAction, Action<MongoIdentityOptions> setupDatabaseAction) where TUser : MongoUser
          {
               return AddAdditionalIdentityMongoDbProvider<TUser, MongoRole>(services, setupIdentityAction, setupDatabaseAction);
          }

          public static IdentityBuilder AddAdditionalIdentityMongoDbProvider<TUser, TRole>(this IServiceCollection services,
              Action<IdentityOptions> setupIdentityAction, Action<MongoIdentityOptions> setupDatabaseAction) where TUser : MongoUser
              where TRole : MongoRole
          {
               var dbOptions = new MongoIdentityOptions();
               setupDatabaseAction(dbOptions);

               services.TryAddTransient<RoleStore<TRole>>();
               services.TryAddTransient<UserStore<TUser, TRole>>();
               services.TryAddTransient<UserManager<TUser>>();
               services.TryAddTransient<RoleManager<TRole>>();
               services.TryAddScoped<SignInManager<TUser>>();

               services.TryAddScoped<IUserValidator<TUser>, UserValidator<TUser>>();
               services.TryAddScoped<IPasswordValidator<TUser>, PasswordValidator<TUser>>();
               services.TryAddScoped<IPasswordHasher<TUser>, PasswordHasher<TUser>>();
               services.TryAddScoped<IRoleValidator<TRole>, RoleValidator<TRole>>();
               services.TryAddScoped<IUserClaimsPrincipalFactory<TUser>, UserClaimsPrincipalFactory<TUser, TRole>>();

               var userCollection = new IdentityUserCollection<TUser>(dbOptions.ConnectionString, dbOptions.UsersCollection);
               var roleCollection = new IdentityRoleCollection<TRole>(dbOptions.ConnectionString, dbOptions.RolesCollection);

               services.AddTransient<IIdentityUserCollection<TUser>>(x => userCollection);
               services.AddTransient<IIdentityRoleCollection<TRole>>(x => roleCollection);

               // Identity Services
               services.AddTransient<IUserStore<TUser>>(x => new UserStore<TUser, TRole>(userCollection, roleCollection, x.GetService<ILookupNormalizer>()));
               services.AddTransient<IRoleStore<TRole>>(x => new RoleStore<TRole>(roleCollection));

               if (setupIdentityAction != null) services.Configure(setupIdentityAction);
               var builder = new IdentityBuilder(typeof(TUser), typeof(TRole), services);
               builder.AddDefaultTokenProviders();
               return builder;
          }
     }
}