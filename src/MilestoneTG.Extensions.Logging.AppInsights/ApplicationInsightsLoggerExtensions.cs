using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace MilestoneTG.Extensions.Logging.AppInsights
{
    public static class ApplicationInsightsLoggerExtensions
    {
        public static ILoggerFactory AddApplicationInsights(this ILoggerFactory factory)
        {
            return AddApplicationInsights(factory, LogLevel.Information);
        }

        public static ILoggerFactory AddApplicationInsights(this ILoggerFactory factory, IConfiguration configuration)
        {
            LogLevel logLevel;
            if (!Enum.TryParse<LogLevel>(configuration["LogLevel:Default"], out logLevel))
                logLevel = LogLevel.Information;
            return AddApplicationInsights(factory, logLevel);
        }

        public static ILoggerFactory AddApplicationInsights(this ILoggerFactory factory,
                                        Func<string, LogLevel, bool> filter = null)
        {
            factory.AddProvider(new ApplicationInsightsLoggerProvider(filter));
            return factory;
        }

        public static ILoggerFactory AddApplicationInsights(this ILoggerFactory factory, LogLevel minLevel)
        {
            return AddApplicationInsights(
                factory,
                (_, logLevel) => logLevel >= minLevel);
        }
    }
}
