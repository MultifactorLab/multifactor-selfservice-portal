using Elastic.CommonSchema.Serilog;
using MultiFactor.SelfService.Linux.Portal.Core;
using MultiFactor.SelfService.Linux.Portal.Core.Serialization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class Logging
    {
        public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder applicationBuilder)
        {
            var logLevel = GetLogMinimalLevel(applicationBuilder.Configuration.GetConfigValue<string>("Logging:LogLevel:Default"));
            var loggerConfiguration = new LoggerConfiguration().MinimumLevel.Is(logLevel);
            var msOverride = applicationBuilder.Configuration.GetConfigValue<string>("Logging:LogLevel:Microsoft");
            if (!string.IsNullOrEmpty(msOverride))
            {
                loggerConfiguration.MinimumLevel.Override("Microsoft", GetLogMinimalLevel(msOverride));
            }

            ConfigureConsoleLog(loggerConfiguration, applicationBuilder);
            ConfigureFileLog(loggerConfiguration, applicationBuilder);

            var logger = loggerConfiguration.CreateLogger();
            applicationBuilder.Logging.ClearProviders();
            applicationBuilder.Logging.AddSerilog(logger);

            Log.Logger = logger;
            if (applicationBuilder.Environment.IsEnvironment("localhost"))
            {
                Log.Logger.Information($"Environment: {applicationBuilder.Configuration.GetConfigValue<string>("Environment")}. Logging subsystem has been configured");
            }

            return applicationBuilder;
        }

        private static LogEventLevel GetLogMinimalLevel(string level) =>
            level switch
            {
                "Debug" => LogEventLevel.Debug,
                "Info" => LogEventLevel.Information,
                "Warn" => LogEventLevel.Warning,
                "Error" => LogEventLevel.Error,
                _ => LogEventLevel.Information
            };

        private static void ConfigureFileLog(LoggerConfiguration loggerConfiguration, WebApplicationBuilder applicationBuilder)
        {
            var useFileOutput = applicationBuilder.Configuration.GetSection("Logging:Output:File").GetValue("enabled", true);
            if (!useFileOutput)
            {
                return;
            }

            var restrictToLogLevel = applicationBuilder.Configuration.GetSection("Logging:Output:File").GetValue("restrictToLogLevel", LogEventLevel.Debug.ToString());
            var formatter = GetLogFormatter(applicationBuilder);
            var path = $"{Constants.LOG_DIRECTORY}/sspl-log-.txt";
            if (formatter != null)
            {
                loggerConfiguration.WriteTo.File(formatter, path, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: GetLogMinimalLevel(restrictToLogLevel));
            }
            else
            {
                loggerConfiguration.WriteTo.File(path, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: GetLogMinimalLevel(restrictToLogLevel));
            }
        }

        private static void ConfigureConsoleLog(LoggerConfiguration loggerConfiguration, WebApplicationBuilder applicationBuilder)
        {
            var useConsoleOutput = applicationBuilder.Configuration.GetSection("Logging:Output:Console").GetValue("enabled", true);
            if (!useConsoleOutput)
            {
                return;
            }

            var restrictToLogLevel = applicationBuilder.Configuration.GetSection("Logging:Output:Console").GetValue("restrictToLogLevel", LogEventLevel.Debug.ToString());
            var formatter = GetLogFormatter(applicationBuilder);
            if (formatter != null)
            {
                loggerConfiguration.WriteTo.Console(formatter, restrictedToMinimumLevel: GetLogMinimalLevel(restrictToLogLevel));
            }
            else
            {
                loggerConfiguration.WriteTo.Console(restrictedToMinimumLevel: GetLogMinimalLevel(restrictToLogLevel));
            }
        }

        private static ITextFormatter GetLogFormatter(WebApplicationBuilder applicationBuilder)
        {
            var loggingFormat = applicationBuilder.Configuration.GetPortalSettingsValue(x => x.LoggingFormat);

            if (string.IsNullOrEmpty(loggingFormat))
            {
                return null;
            }

            if (!Enum.TryParse<SerilogJsonFormatterTypes>(loggingFormat, true, out var formatterType))
            {
                return null;
            }

            return formatterType switch
            {
                SerilogJsonFormatterTypes.Json or SerilogJsonFormatterTypes.JsonUtc => new RenderedCompactJsonFormatter(),
                SerilogJsonFormatterTypes.JsonTz => new CustomCompactJsonFormatter("yyyy-MM-dd HH:mm:ss.fff zzz"),
                SerilogJsonFormatterTypes.ElasticCommonSchema => new EcsTextFormatter(),
                _ => null,
            };
        }
    }
}
