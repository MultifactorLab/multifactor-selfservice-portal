﻿using System.Globalization;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;

namespace MultiFactor.SelfService.Linux.Portal.Core.Serialization;

// analog of the class RenderedCompactJsonFormatter with the ability to customize the timestamp template
// important - the timestamp use local (not UTC) format
public class CustomCompactJsonFormatter : ITextFormatter
{
    private readonly JsonValueFormatter _valueFormatter;
    private static string _timestampTemplate;

    public CustomCompactJsonFormatter(string timestampTemplate, JsonValueFormatter valueFormatter = null)
    {
        _valueFormatter = valueFormatter ?? new JsonValueFormatter(typeTagName: "$type");
        _timestampTemplate = timestampTemplate;
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        FormatEvent(logEvent, output, _valueFormatter);
        output.WriteLine();
    }

    private static void FormatEvent(LogEvent logEvent, TextWriter output, JsonValueFormatter valueFormatter)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(valueFormatter);

        output.Write("{\"@t\":\"");
        output.Write(logEvent.Timestamp.ToString(_timestampTemplate));
        output.Write("\",\"@m\":");
        var message = logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture);
        JsonValueFormatter.WriteQuotedJsonString(message, output);
        output.Write(",\"@i\":\"");
        var id = EventIdHash.Compute(logEvent.MessageTemplate.Text);
        output.Write(id.ToString("x8", CultureInfo.InvariantCulture));
        output.Write('"');

        if (logEvent.Level != LogEventLevel.Information)
        {
            output.Write(",\"@l\":\"");
            output.Write(logEvent.Level);
            output.Write('\"');
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"@x\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        if (logEvent.TraceId != null)
        {
            output.Write(",\"@tr\":\"");
            output.Write(logEvent.TraceId.Value.ToHexString());
            output.Write('\"');
        }

        if (logEvent.SpanId != null)
        {
            output.Write(",\"@sp\":\"");
            output.Write(logEvent.SpanId.Value.ToHexString());
            output.Write('\"');
        }

        foreach (var property in logEvent.Properties)
        {
            var name = property.Key;
            if (name.Length > 0 && name[0] == '@')
            {
                // Escape first '@' by doubling
                name = '@' + name;
            }

            output.Write(',');
            JsonValueFormatter.WriteQuotedJsonString(name, output);
            output.Write(':');
            valueFormatter.Format(property.Value, output);
        }

        output.Write('}');
    }
}
