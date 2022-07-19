using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Syslog;
using System.Net;
using System.Net.Sockets;

namespace MultiFactor.SelfService.Linux.Portal.Extensions
{
    internal static class Logging
    {
        public static void ConfigureLogging(this WebApplicationBuilder applicationBuilder)
        {
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch);

            ConfigureFilelog(applicationBuilder, loggerConfiguration);
            ConfigureSyslog(applicationBuilder, loggerConfiguration, out var syslogInfoMessage);

            Log.Logger = loggerConfiguration.CreateLogger();

            Log.Logger.Information(syslogInfoMessage);

            //try
            //{
            //    Configuration.Load();
            //    SetLogLevel(Configuration.Current.LogLevel, levelSwitch);

            //    if (syslogInfoMessage != null)
            //    {
            //        Log.Logger.Debug(syslogInfoMessage);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Log.Logger.Error(ex, "Unable to start");
            //    throw;
            //}
        }

        private static void ConfigureFilelog(WebApplicationBuilder applicationBuilder, LoggerConfiguration loggerConfiguration)
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
            var loggingFormat = applicationBuilder.Configuration.GetValue<string>($"{nameof(PortalSettings)}:{nameof(PortalSettings.LoggingFormat)}");
            switch (loggingFormat?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }

        private static void ConfigureSyslog(WebApplicationBuilder applicationBuilder, LoggerConfiguration loggerConfiguration, out string logMessage)
        {
            logMessage = null;

            var logServer = applicationBuilder.GetSettingsValue(x => x.SyslogServer, null);
            if (logServer == null)
            {
                return;
            }

            var uri = new Uri(logServer);
            if (uri.Port == -1)
            {
                throw new Exception($"Invalid port number for syslog-server {logServer}");
            }

            var format = applicationBuilder.GetSettingsValue(x => x.SyslogFormat, SyslogFormat.RFC5424.ToString());
            var facility = applicationBuilder.GetSettingsValue(x => x.SyslogFacility, Facility.Auth.ToString());
            var appName = applicationBuilder.GetSettingsValue(x => x.SyslogAppName, "multifactor-portal");
            var framer = FramingType.OCTET_COUNTING;
            var isJson = applicationBuilder.GetSettingsValue(x => x.LoggingFormat)?.ToLower() == "json";

            loggerConfiguration.WriteTo.LocalSyslog("TestApp", Facility.Auth, "");

            switch (uri.Scheme)
            {
                case "udp":
                    var serverIp = ResolveIP(uri.Host);
                    loggerConfiguration
                        .WriteTo
                        .UdpSyslog(serverIp, uri.Port, appName, SyslogFormat.RFC5424, Facility.Auth);
                    logMessage = $"Using syslog server: {logServer}, format: {format}, facility: {facility}, appName: {appName}";
                    break;
                case "tcp":
                    loggerConfiguration
                        .WriteTo
                        .TcpSyslog(uri.Host, uri.Port, appName, FramingType.OCTET_COUNTING, SyslogFormat.RFC5424, Facility.Auth);
                    logMessage = $"Using syslog server {logServer}, format: {format}, framing: {framer}, facility: {facility}, appName: {appName}";
                    break;
                default:
                    throw new NotImplementedException($"Unknown scheme {uri.Scheme} for syslog-server {logServer}. Expected udp or tcp");
            }
        }

        private static string ResolveIP(string host)
        {
            if (IPAddress.TryParse(host, out var addr))
            {
                return host;
            }

            // Only ipv4.
            return Dns.GetHostAddresses(host)
                .First(x => x.AddressFamily == AddressFamily.InterNetwork)
                .ToString();
        }
    }
}
