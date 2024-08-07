using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace Netcorext.Logging.Serilog;

public class JsonPropertyFilterFormatter : ITextFormatter
{
    private readonly string? _closingDelimiter;
    private readonly bool _renderMessage;
    private readonly JsonValueFormatter _jsonValueFormatter = new();

    private readonly HashSet<string> _defaultAllowList = new(StringComparer.CurrentCultureIgnoreCase)
                                                         {
                                                             "ConnectionId",
                                                             "ContentLength",
                                                             "DeviceId",
                                                             "Duration",
                                                             "Elapsed",
                                                             "ElapsedMilliseconds",
                                                             "EventId",
                                                             "Headers",
                                                             "Host",
                                                             "HostingRequestFinishedLog",
                                                             "Ip",
                                                             "MachineName",
                                                             "Method",
                                                             "Path",
                                                             "Protocol",
                                                             "QueryString",
                                                             "RequestId",
                                                             "ResponseHeaders",
                                                             "Scheme",
                                                             "SourceContext",
                                                             "StatusCode",
                                                             "ThreadId",
                                                             "TraceIdentifier",
                                                             "Traffic",
                                                             "Url",
                                                             "User",
                                                             "UserAgent",
                                                             "XRequestId"
                                                         };

    private readonly HashSet<string> _allowProperties = new(StringComparer.CurrentCultureIgnoreCase);

    public JsonPropertyFilterFormatter(string? closingDelimiter = null, bool renderMessage = false, IFormatProvider? formatProvider = null, string? allowProperties = null)
    {
        _closingDelimiter = closingDelimiter;
        _renderMessage = renderMessage;

        var allowProps = allowProperties?.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                                         .Select(t => t.Trim())
                                         .ToArray();

        if (allowProps == null || allowProps.Length == 0)
            _allowProperties.UnionWith(_defaultAllowList);
        else if (allowProps.Contains("clear"))
        {
            _allowProperties.UnionWith(allowProps);
            _allowProperties.Remove("clear");
        }
        else if (allowProps.Contains("*"))
        {
            _allowProperties.Clear();
            _allowProperties.Add("*");
        }
        else
            _allowProperties.UnionWith(_defaultAllowList);
    }

    public void Format(LogEvent logEvent, TextWriter output)
    {
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
        if (output == null) throw new ArgumentNullException(nameof(output));

        output.Write("{\"Timestamp\":\"");
        output.Write(logEvent.Timestamp.ToString("O"));
        output.Write("\",\"Level\":\"");
        output.Write(logEvent.Level);
        output.Write("\",\"MessageTemplate\":");
        JsonValueFormatter.WriteQuotedJsonString(logEvent.MessageTemplate.Text, output);

        if (_renderMessage)
        {
            output.Write(",\"RenderedMessage\":");
            var message = logEvent.MessageTemplate.Render(logEvent.Properties);
            JsonValueFormatter.WriteQuotedJsonString(message, output);
        }

        if (logEvent.TraceId != null)
        {
            output.Write(",\"TraceId\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.TraceId.ToString()!, output);
        }

        if (logEvent.SpanId != null)
        {
            output.Write(",\"SpanId\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.SpanId.ToString()!, output);
        }

        if (logEvent.Exception != null)
        {
            output.Write(",\"Exception\":");
            JsonValueFormatter.WriteQuotedJsonString(logEvent.Exception.ToString(), output);
        }

        if (logEvent.Properties.Count != 0)
        {
            output.Write(",\"Properties\":{");

            char? propertyDelimiter = null;

            foreach (var property in logEvent.Properties)
            {
                if (!_allowProperties.Contains("*") && !_allowProperties.Contains(property.Key.ToLower()))
                    continue;

                if (propertyDelimiter != null)
                    output.Write(propertyDelimiter.Value);
                else
                    propertyDelimiter = ',';

                JsonValueFormatter.WriteQuotedJsonString(property.Key, output);
                output.Write(':');
                _jsonValueFormatter.Format(property.Value, output);
            }

            output.Write('}');
        }

        output.Write('}');
        output.Write(_closingDelimiter);
        output.WriteLine();
    }
}
