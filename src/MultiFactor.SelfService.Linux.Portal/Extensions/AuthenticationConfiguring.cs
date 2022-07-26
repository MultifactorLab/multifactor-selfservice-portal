using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class AuthenticationConfiguring
    {
        public static WebApplicationBuilder ConfigureAuthentication(this WebApplicationBuilder applicationBuilder)
        {
            applicationBuilder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/account/login";
                    o.LogoutPath = "/account/login";
                    o.ExpireTimeSpan = TimeSpan.FromHours(48);
                    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    o.Cookie.Name = Constants.COOKIE_NAME;
                });

            applicationBuilder.Services
                .Configure<CookiePolicyOptions>(options =>
                {
                    // Prevent access from javascript 
                    options.HttpOnly = HttpOnlyPolicy.Always;
                    options.Secure = CookieSecurePolicy.Always;
                    options.MinimumSameSitePolicy = SameSiteMode.Strict;
                })
                // Adds the X-Frame-Options header with the value SAMEORIGIN.
                .AddAntiforgery();

            return applicationBuilder;
        }
    }
}
