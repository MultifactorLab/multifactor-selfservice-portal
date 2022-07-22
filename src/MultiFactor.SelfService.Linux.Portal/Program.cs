using Microsoft.AspNetCore.Authentication.Cookies;
using MultiFactor.SelfService.Linux.Portal.Controllers;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.ModelBinding;
using MultiFactor.SelfService.Linux.Portal.Services.Api;
using MultiFactor.SelfService.Linux.Portal.Stories.LoginStory;

var builder = WebApplication.CreateBuilder(args);

builder.AddSettings(args);
builder.ConfigureLogging();
builder.LoadPortalSettings();


// <system.web. authentication mode="Forms">
// <forms loginUrl="~/account/login" timeout="2880" requireSSL="true"/>
// </authentication>
// <httpCookies requireSSL="true"/>
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/account/login";
});

// Adds the X-Frame-Options header with the value SAMEORIGIN.
builder.Services.AddAntiforgery();

builder.AddControllersWithViewsAndLocalization(o =>
{
    o.ModelBinderProviders.Insert(0, ModelBindingConfiguration.GetModelBinderProvider());
    o.Filters.Add<ModelStateErrorExceptionFilterAttribute>();
});





builder.Services.AddScoped<MultiFactorSelfServiceApiClient>();
builder.Services.AddScoped<LoginStory>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsEnvironment("production"))
{
    app.UseExceptionHandler("/Shared/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
