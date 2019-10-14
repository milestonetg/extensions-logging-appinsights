using Microsoft.Extensions.Logging;
using System;

namespace MilestoneTG.Extensions.Logging.AppInsights
{
    public class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool> _filter;

        public ApplicationInsightsLoggerProvider(Func<string, LogLevel, bool> filter)
        {
            _filter = filter;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ApplicationInsightsLogger(categoryName, _filter);
        }

        public void Dispose()
        {

        }
    }
}
