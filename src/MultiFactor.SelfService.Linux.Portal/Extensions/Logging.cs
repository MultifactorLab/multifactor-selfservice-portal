using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class Logging
    {
        public static void ConfigureLogging(this WebApplicationBuilder applicationBuilder)
        {
            var logLevel = GetLogMinimalLevel(applicationBuilder.Configuration.GetSettingsValue(x => x.LoggingLevel));
            var levelSwitch = new LoggingLevelSwitch(logLevel);
            var loggerConfiguration = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch);

            ConfigureConsoleLog(loggerConfiguration);
            ConfigureFileLog(loggerConfiguration, applicationBuilder);

            var logger = loggerConfiguration.CreateLogger();
            applicationBuilder.Logging.ClearProviders();
            applicationBuilder.Logging.AddSerilog(logger);
            Log.Logger = logger;
            Log.Logger.Information("Logging subsystem has been configured");
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

        private static void ConfigureConsoleLog(LoggerConfiguration loggerConfiguration)
        {
            loggerConfiguration.WriteTo.Console(LogEventLevel.Warning);
        }

        private static void ConfigureFileLog(LoggerConfiguration loggerConfiguration, WebApplicationBuilder applicationBuilder)
        {
            // TODO: проверить в линуксе
            var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            var formatter = GetLogFormatter(applicationBuilder);
            if (formatter != null)
            {
                loggerConfiguration.WriteTo.File(formatter, $"{path}\\Logs\\log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration.WriteTo.File($"{path}\\Logs\\log-.txt", rollingInterval: RollingInterval.Day);
            }
        }

        private static ITextFormatter? GetLogFormatter(WebApplicationBuilder applicationBuilder)
        {
            var loggingFormat = applicationBuilder.Configuration.GetSettingsValue(x => x.LoggingFormat);
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
