using Microsoft.AspNetCore.DataProtection;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Filters;
using MultiFactor.SelfService.Linux.Portal.ModelBinding;

var builder = WebApplication.CreateBuilder(args);

builder.LoadSettings(args)
    .ConfigureLogging()
    .ConfigureAuthentication()
    .AddControllersWithViewsAndLocalization(o =>
    {
        o.ModelBinderProviders.Insert(0, ModelBindingConfiguration.GetModelBinderProvider());
        o.Filters.Add<ModelStateErrorExceptionFilter>();
    });

//
// "If data protection isn't configured, the keys are held in memory and discarded when the app restarts."
Console.WriteLine($"OS: {Environment.OSVersion}");
Console.WriteLine($"LocalApplicationData: {Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}");
builder.Services.AddDataProtection()
    // TODO: test!
    .PersistKeysToFileSystem(new DirectoryInfo(@"/var/sspl-key-storage"))
    .SetApplicationName("MultiFactorSSP");
//
//


builder.ConfigureApplicationServices();

var app = builder.Build();

if (app.Environment.IsEnvironment("production"))
{
    app.UseExceptionHandler("/shared/error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.Use((context, next) =>
{
    return next();
});

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

app.Run();
