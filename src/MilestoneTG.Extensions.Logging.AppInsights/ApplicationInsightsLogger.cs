using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace MilestoneTG.Extensions.Logging.AppInsights
{
    public class ApplicationInsightsLogger : ILogger
    {
        private string _categoryName;
        private Func<string, LogLevel, bool> _filter;
        private TelemetryClient _telemetryClient;
                
        public ApplicationInsightsLogger(string categoryName, Func<string, LogLevel, bool> filter)
        {
            _categoryName = categoryName;
            _filter = filter;
            _telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (_filter == null || _filter(_categoryName, logLevel));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            FormattedLogValues values = state as FormattedLogValues;

            if (values == null)
                return;

            Dictionary<string, string> properties = new Dictionary<string, string>();

            foreach (var entry in values)
            {
                try
                {
                    if (entry.Value is Dictionary<string, string>)
                    {
                        Dictionary<string, string> dictionary = entry.Value as Dictionary<string, string>;
                        foreach (var kvp in dictionary)
                            properties.Add(kvp.Key, kvp.Value?.ToString());
                    }
                    else if (entry.Value is KeyValuePair<string, string>)
                    {
                        KeyValuePair<string, string> keyValuePair = (KeyValuePair<string, string>)entry.Value;
                        properties.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                    else if (entry.Value is KeyValuePair<string, string>[])
                    {
                        KeyValuePair<string, string>[] kvps = (KeyValuePair<string, string>[])entry.Value;
                        foreach (KeyValuePair<string, string> kvp in kvps)
                            properties.Add(kvp.Key, kvp.Value?.ToString());
                    }
                    else if (entry.Value is HttpResponseMessage)
                    {
                        //Add Response INfo
                        HttpResponseMessage response = (HttpResponseMessage)entry.Value;
                        properties.Add("ResponseStatusCode", response.StatusCode.ToString());
                        properties.Add("ResponseReasonPhrase", response.ReasonPhrase);
                        properties.Add("ResponseVersion", response.Version.ToString());

                        string content = response.Content.ReadAsStringAsync()
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();
                            properties.Add("ResponseContent", content);
                        
                        foreach (var header in response.Headers)
                            properties.Add($"ResponseHeader{header.Key}", $"[{string.Join(",", header.Value?.Select(v => $"\"{v}\""))}]");

                        //Add Request Info
                        HttpRequestMessage request = response.RequestMessage;

                        properties.Add("RequestMethod", request.Method.Method);
                        properties.Add("RequestUri", request.RequestUri.ToString());
                        properties.Add("RequestVersion", request.Version.ToString());
                        
                        string requestContent = request.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                        properties.Add("RequestContent", requestContent);

                        foreach (var header in request.Headers)
                            properties.Add($"RequestHeader{header.Key}", $"[{string.Join(",", header.Value?.Select(v => $"\"{v}\""))}]");

                        foreach (var property in request.Properties)
                            properties.Add($"RequestProperty{property.Key}", property.Value?.ToString());
                    }
                    else if (entry.Value is HttpRequestMessage)
                    {
                        HttpRequestMessage request = (HttpRequestMessage)entry.Value;

                        properties.Add("RequestMethod", request.Method.Method);
                        properties.Add("RequestUri", request.RequestUri?.ToString());
                        properties.Add("RequestVersion", request.Version?.ToString());

                        string requestContent = request.Content?.ReadAsStringAsync()
                            .ConfigureAwait(false)
                            .GetAwaiter()
                            .GetResult();

                        properties.Add("RequestContent", requestContent);

                        foreach (var header in request.Headers)
                            properties.Add($"RequestHeader{header.Key}", $"[{string.Join(",", header.Value?.Select(v => $"\"{v}\""))}]");

                        foreach (var property in request.Properties)
                            properties.Add($"RequestProperty{property.Key}", property.Value?.ToString());
                    }
                    else if (entry.Value is Microsoft.AspNetCore.Http.HttpRequest)
                    {
                        var request = (Microsoft.AspNetCore.Http.HttpRequest)entry.Value;

                        properties.Add("RequestMethod", request.Method);
                        properties.Add("RequestUri", $"{request.Scheme}://{request.Host}/{request.Path}{request.QueryString.ToString()}");

                        string requestContent = string.Empty;
                        if (request.ContentLength.GetValueOrDefault() > 0)
                        {
                            if (request.Body != null && request.ContentLength > 0)                            
                            {
                                request.Body.Position = 0;
                                byte[] buffer = new byte[(int)request.ContentLength];
                                request.Body?.Read(buffer, 0, buffer.Length);
                                requestContent = Encoding.UTF8.GetString(buffer);
                            }
                        }
                        properties.Add("RequestContent", requestContent);

                        foreach (var header in request.Headers)
                        {
                            string value = string.Empty;
                            if (header.Key == "Authorization")
                                value = "XXXXXXXXXXXXX";
                            else
                                value = $"[{string.Join(",", header.Value.Select(v => $"\"{v}\""))}]";

                            properties.Add($"RequestHeader_{header.Key}", value);
                        }
                    }
                    else if (entry.Value is KeyValuePair<string, object>)
                    {
                        KeyValuePair<string, object> keyValuePair = (KeyValuePair<string, object>)entry.Value;
                        properties.Add(keyValuePair.Key, JsonConvert.SerializeObject(keyValuePair.Value));
                    }
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }

            try
            {
                var message = formatter(state, exception);

                switch (logLevel)
                {
                    case LogLevel.Critical:
                    case LogLevel.Error:
                        if (exception == null)
                            _telemetryClient.TrackException(new Exception(message));
                        else
                            _telemetryClient.TrackException(exception, properties);
                        break;
                    case LogLevel.Debug:
                    case LogLevel.Trace:
                        _telemetryClient.TrackTrace(message, properties);
                        break;
                    default:
                        _telemetryClient.TrackEvent(logLevel.ToString(), properties);
                        break;
                }
            }
            catch { }            
        }
    }
}
