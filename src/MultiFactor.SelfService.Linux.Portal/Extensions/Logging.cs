using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class Logging
    {
        public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder applicationBuilder)
        {
            var logLevel = GetLogMinimalLevel(applicationBuilder.Configuration.GetPortalSettingsValue(x => x.LoggingLevel));
            var levelSwitch = new LoggingLevelSwitch(logLevel);
            var loggerConfiguration = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch);

            var isLocalhost = applicationBuilder.Environment.IsEnvironment("localhost");
            loggerConfiguration.WriteTo.Console(isLocalhost ? levelSwitch.MinimumLevel : LogEventLevel.Warning);

            ConfigureFileLog(loggerConfiguration, applicationBuilder);

            var logger = loggerConfiguration.CreateLogger();
            applicationBuilder.Logging.ClearProviders();
            applicationBuilder.Logging.AddSerilog(logger);

            Log.Logger = logger;
            if (isLocalhost)
            {
                Log.Logger.Information($"Environment: {applicationBuilder.Configuration.GetConfigValue<string>("Environment")}. Logging subsystem has been configured");
            }

            return applicationBuilder;
        }

        private static LogEventLevel GetLogMinimalLevel(string? level)
        {
            switch (level)
            {
                case "Debug": return LogEventLevel.Debug;
                case "Info": return LogEventLevel.Information;
                case "Warn": return LogEventLevel.Warning;
                case "Error": return LogEventLevel.Error;
                default: return LogEventLevel.Information;
            }
        }

        private static void ConfigureFileLog(LoggerConfiguration loggerConfiguration, WebApplicationBuilder applicationBuilder)
        {
            var formatter = GetLogFormatter(applicationBuilder);
            var path = $"{Core.Constants.LOG_DIRECTORY}/sspl-log-.txt";
            if (formatter != null)
            {
                loggerConfiguration.WriteTo.File(formatter, path, rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration.WriteTo.File(path, rollingInterval: RollingInterval.Day);
            }
        }

        private static ITextFormatter? GetLogFormatter(WebApplicationBuilder applicationBuilder)
        {
            var loggingFormat = applicationBuilder.Configuration.GetPortalSettingsValue(x => x.LoggingFormat);
            switch (loggingFormat?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }
    }
}
