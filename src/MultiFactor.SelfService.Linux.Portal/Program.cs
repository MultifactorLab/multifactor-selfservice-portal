using Microsoft.AspNetCore.Authentication.Cookies;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Services.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddSettings(args);
builder.ConfigureLogging();
builder.LoadPortalSettings();
builder.Services.AddControllersWithViews();
builder.AddLocalization();

////////////////////////////////////////////////////////////////////////////////
// <system.web. authentication mode="Forms">
// <forms loginUrl="~/account/login" timeout="2880" requireSSL="true"/>
// </authentication>
// <httpCookies requireSSL="true"/>

//builder.Services.AddAuthentication().AddCookie(o =>
//{
//    o.LoginPath = "~/account/login";
//    o.aut
//});
//////////////////////////////////////////////////////////////////////////////////


// Adds the X-Frame-Options header with the value SAMEORIGIN.
builder.Services.AddAntiforgery();

builder.Services.AddScoped<MultiFactorSelfServiceApiClient>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Shared/Error");
}

app.UseLocalization();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
