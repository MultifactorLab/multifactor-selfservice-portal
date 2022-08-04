using Microsoft.AspNetCore.DataProtection;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Filters;
using MultiFactor.SelfService.Linux.Portal.ModelBinding;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfiguration(args)
    .ConfigureLogging()
    .RegisterConfiguration()
    .ConfigureApplicationServices()
    .ConfigureAuthentication()
    .AddControllersWithViewsAndLocalization(o =>
    {
        o.ModelBinderProviders.Insert(0, ModelBindingConfiguration.GetModelBinderProvider());
        o.Filters.Add<ApplicationExceptionFilter>();
    });

//
// "If data protection isn't configured, the keys are held in memory and discarded when the app restarts."
builder.Services.AddDataProtection()
    // TODO: test!
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration.GetPortalSettingsValue(x => x.KeyStorageDirectory)))
    .SetApplicationName("MultiFactorSSPL");
//
//

var app = builder.Build();


if (app.Environment.IsEnvironment("production"))
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseRequestLocalization();
app.UseCookiePolicy();
app.UseSession();

app.UseStaticFiles();
app.Use(async (context, next) =>
{
    var token = context.Request.Cookies[Constants.COOKIE_NAME];
    if (!string.IsNullOrEmpty(token))
    {
        context.Request.Headers.Add("Authorization", $"Bearer {token}");
    }

    await next();
});
app.UseStatusCodePages(async context =>
{
    var request = context.HttpContext.Request;
    if (request.IsAjaxCall()) return;

    var response = context.HttpContext.Response;
    if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
    {
        response.Redirect($"/account/logout{MultiFactorClaimsDtoBinder.FromRequest(request)}");
    }
});

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
