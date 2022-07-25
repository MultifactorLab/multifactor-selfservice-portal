using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using MultiFactor.SelfService.Linux.Portal;
using MultiFactor.SelfService.Linux.Portal.Controllers;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.ModelBinding;

var builder = WebApplication.CreateBuilder(args);

builder.AddSettings(args);
builder.ConfigureLogging();
builder.LoadPortalSettings();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/account/login";
        o.LogoutPath = "/account/login";
        o.ExpireTimeSpan = TimeSpan.FromHours(48);
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.Name = Constants.COOKIE_NAME;
    });

// Adds the X-Frame-Options header with the value SAMEORIGIN.
builder.Services.AddAntiforgery();

builder.AddControllersWithViewsAndLocalization(o =>
{
    o.ModelBinderProviders.Insert(0, ModelBindingConfiguration.GetModelBinderProvider());
    o.Filters.Add<ModelStateErrorExceptionFilterAttribute>();
});

// "If data protection isn't configured, the keys are held in memory and discarded when the app restarts."
Console.WriteLine($"OS: {Environment.OSVersion}");
Console.WriteLine($"LocalApplicationData: {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}");
builder.Services.AddDataProtection()
    // TODO: test!
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/sspl-key-storage"))
    .SetApplicationName("MultiFactorSSP");

builder.ConfigureApplicationServices();

var app = builder.Build();

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
