using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Authentication;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Security.Cookies;
using Microsoft.AspNet.Security.Facebook;
using Microsoft.AspNet.Security.Google;
using Microsoft.AspNet.Security.Twitter;
using Microsoft.Data.Entity;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using IdentitySample.Models;
using System;
using Microsoft.AspNet.Security;
using System.Threading.Tasks;

namespace IdentitySamples
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            /* Adding IConfiguration as a service in the IoC to avoid instantiating Configuration again.
                 * Below code demonstrates usage of multiple configuration sources. For instance a setting say 'setting1' is found in both the registered sources, 
                 * then the later source will win. By this way a Local config can be overridden by a different setting while deployed remotely.
            */
            var configuration = new Configuration();
            configuration.AddJsonFile("LocalConfig.json");
            configuration.AddEnvironmentVariables(); //All environment variables in the process's context flow in as configuration values.

            app.UseServices(services =>
            {
                // Add EF services to the services container
                services.AddEntityFramework()
                        .AddSqlServer();

                // Configure DbContext           
                services.SetupOptions<IdentityDbContextOptions>(options =>
                {
                    options.DefaultAdminUserName = configuration.Get("DefaultAdminUsername");
                    options.DefaultAdminPassword = configuration.Get("DefaultAdminPassword");
                    options.UseSqlServer(configuration.Get("Data:IdentityConnection:ConnectionString"));
                });

                // Add Identity services to the services container
                services.AddIdentitySqlServer<ApplicationDbContext, ApplicationUser>()
                        .AddAuthentication()
                        .SetupOptions(options =>
                        {
                            options.Password.RequireDigit = false;
                            options.Password.RequireLowercase = false;
                            options.Password.RequireUppercase = false;
                            options.Password.RequireNonLetterOrDigit = false;
                        });

                // Add MVC services to the services container
                services.AddMvc();
            });

            /* Error page middleware displays a nice formatted HTML page for any unhandled exceptions in the request pipeline.
             * Note: ErrorPageOptions.ShowAll to be used only at development time. Not recommended for production.
             */
            app.UseErrorPage(ErrorPageOptions.ShowAll);

            // Add static files to the request pipeline
            app.UseStaticFiles();

            // REVIEW: move to extension method? app.UseExternalLoginCookie()?
            app.SetDefaultSignInAsAuthenticationType(ClaimsIdentityOptions.DefaultExternalLoginAuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = ClaimsIdentityOptions.DefaultExternalLoginAuthenticationType,
                AuthenticationMode = AuthenticationMode.Passive,
                CookieName = ".ASP" + ClaimsIdentityOptions.DefaultExternalLoginAuthenticationType,
                ExpireTimeSpan = TimeSpan.FromMinutes(5),
            });


            // Add cookie-based authentication to the request pipeline
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = ClaimsIdentityOptions.DefaultAuthenticationType,
                LoginPath = new PathString("/Account/Login"),
                Notifications = new CookieAuthenticationNotifications
                {
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30))
                }
            });

            app.UseTwoFactorSignInCookies();

            app.UseGoogleAuthentication(new GoogleAuthenticationOptions
            {
                ClientId = "514485782433-fr3ml6sq0imvhi8a7qir0nb46oumtgn9.apps.googleusercontent.com",
                ClientSecret = "V2nDD9SkFbvLTqAUBWBBxYAL"
            });

            app.UseFacebookAuthentication(new FacebookAuthenticationOptions
            {
                AppId = "901611409868059",
                AppSecret = "4aa3c530297b1dcebc8860334b39668b"
            });

            app.UseTwitterAuthentication(new TwitterAuthenticationOptions
            {

                ConsumerKey = "BSdJJ0CrDuvEhpkchnukXZBUv",
                ConsumerSecret = "xKUNuKhsRdHD03eLn67xhPAyE1wFFEndFo1X2UJaK2m1jdAxf4"
            });
            
            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });
            });

            //Populates the Admin user and role
            SampleData.InitializeIdentityDatabaseAsync(app.ApplicationServices).Wait();
        }
    }
}