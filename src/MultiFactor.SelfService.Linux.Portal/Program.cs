using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Extensions;
using MultiFactor.SelfService.Linux.Portal.Filters;
using MultiFactor.SelfService.Linux.Portal.ModelBinding;
using MultiFactor.SelfService.Linux.Portal.ModelBinding.Binders;
using System.Net;
using System.Text;

try
{
    Start();
}
catch (Exception ex)
{
    var errorMessage = FlattenException(ex);
    StartupLogger.Error(ex, "Unable to start: {Message:l}", errorMessage);
}

void Start()
{
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
            o.Filters.Add<FeatureNotEnabledExceptionFilter>();
            o.Filters.Add<MultiFactorApiExceptionFilter>();
        });

    // "If data protection isn't configured, the keys are held in memory and discarded when the app restarts."
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Constants.KEY_STORAGE_DIRECTORY))
        .SetApplicationName("MultiFactorSSPL");

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    if (app.Environment.IsEnvironment(Constants.PRODUCTION_ENV))
    {
        app.UseExceptionHandler("/error");
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
            context.Request.Headers.Authorization = $"Bearer {token}";
        }

        await next();
    });
    app.UseStatusCodePages(context =>
    {
        var request = context.HttpContext.Request;
        if (request.IsAjaxCall()) return Task.CompletedTask;

        var response = context.HttpContext.Response;
        if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
        {
            response.Redirect($"/account/logout{MultiFactorClaimsDtoBinder.FromRequest(request)}");
        }

        return Task.CompletedTask;
    });

    app.UseAuthentication();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
    app.MapApiEndpoints();

    app.Run();
}

string FlattenException(Exception exception)
{
    var stringBuilder = new StringBuilder();

    var counter = 0;

    while (exception != null)
    {
        if (counter++ > 0)
        {
            var prefix = new string('-', counter) + ">\t";
            stringBuilder.Append(prefix);
        }

        stringBuilder.AppendLine(exception.Message);
        exception = exception.InnerException;
    }

    return stringBuilder.ToString();
}

// Needs for tests.
#pragma warning disable CA1050 // Declare types in namespaces
public partial class Program
{
}
#pragma warning restore CA1050 // Declare types in namespaces