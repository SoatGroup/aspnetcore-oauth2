using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;



namespace AllInOne
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath);

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets();
            }

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; set; }


        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true,
                LoginPath = new PathString("/login")
            });

             app.UseOAuthAuthentication(new OAuthOptions
             {
                 AuthenticationScheme = "GitHub",
                 DisplayName = "Github",
                 ClientId = Configuration["github:clientid"],
                 ClientSecret = Configuration["github:clientsecret"],
                 CallbackPath = new PathString("/signin-github"),
                 AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                 TokenEndpoint = "https://github.com/login/oauth/access_token",
                 UserInformationEndpoint = "https://api.github.com/user",
                 ClaimsIssuer = "OAuth2-Github",
                 SaveTokens = true,

                 Events = new OAuthEvents
                 {
                     OnCreatingTicket = async context =>
                     {
                         // Get the GitHub user
                         var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
                         request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                         var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
                         response.EnsureSuccessStatusCode();

                         var user = JObject.Parse(await response.Content.ReadAsStringAsync());

                         var identifier = user.Value<string>("id");
                         if (!string.IsNullOrEmpty(identifier))
                         {
                             context.Identity.AddClaim(new Claim(
                                 ClaimTypes.NameIdentifier, identifier,
                                 ClaimValueTypes.String, context.Options.ClaimsIssuer));
                         }

                         var userName = user.Value<string>("login");
                         if (!string.IsNullOrEmpty(userName))
                         {
                             context.Identity.AddClaim(new Claim(
                                 ClaimsIdentity.DefaultNameClaimType, userName,
                                 ClaimValueTypes.String, context.Options.ClaimsIssuer));
                         }

                         var name = user.Value<string>("name");
                         if (!string.IsNullOrEmpty(name))
                         {
                             context.Identity.AddClaim(new Claim(
                                 "urn:github:name", name,
                                 ClaimValueTypes.String, context.Options.ClaimsIssuer));
                         }

                         var email = user.Value<string>("email");
                         if (!string.IsNullOrEmpty(email))
                         {
                             context.Identity.AddClaim(new Claim(
                                 ClaimTypes.Email, email,
                                 ClaimValueTypes.Email, context.Options.ClaimsIssuer));
                         }

                         var link = user.Value<string>("url");
                         if (!string.IsNullOrEmpty(link))
                         {
                             context.Identity.AddClaim(new Claim(
                                 "urn:github:url", link,
                                 ClaimValueTypes.String, context.Options.ClaimsIssuer));
                         }
                     }

                 }
             });


            app.Map("/login", x =>
            {
                x.Run(async context =>
                {
                    await context.Authentication.ChallengeAsync("GitHub", new AuthenticationProperties() { RedirectUri = "/" });
                });
            });

            app.Map("/logout", x =>
            {
                x.Run(async context =>
                {
                    await context.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/");
                });
            });


            app.Run(async (context) =>
            {
                var user = context.User;

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<html><body>");

                if (user == null || !user.Identities.Any(identity => identity.IsAuthenticated))
                {
                    await context.Response.WriteAsync("<h1>Hello anonymous</h1>");
                    await context.Response.WriteAsync("<a href=\"/login\" > Login</a>");
                }
                else
                {
                    await context.Response.WriteAsync($"<h1>Hello {context.User.Identity.Name}</h1>");
                    foreach (var claim in context.User.Claims)
                    {
                        await context.Response.WriteAsync($"{claim.Type}: {claim.Value}<br>");
                    }

                    await context.Response.WriteAsync("<br>");
                    await context.Response.WriteAsync("Tokens:<br>");
                    await context.Response.WriteAsync("Access Token: " + await context.Authentication.GetTokenAsync("access_token") + "<br>");
                    await context.Response.WriteAsync("Refresh Token: " + await context.Authentication.GetTokenAsync("refresh_token") + "<br>");
                    await context.Response.WriteAsync("Token Type: " + await context.Authentication.GetTokenAsync("token_type") + "<br>");
                    await context.Response.WriteAsync("expires_at: " + await context.Authentication.GetTokenAsync("expires_at") + "<br>");
                   

                    await context.Response.WriteAsync("<a href=\"/logout\">Logout</a><br>");
                }

                await context.Response.WriteAsync("</body></html>");
            });


           
        }
    }
}
