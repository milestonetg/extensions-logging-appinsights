# extensions-logging-appinsights

A Microsoft.Extensions.Logging provider for ApplicationInsights.

## Request Logging

This logger logs the complete query string and request body to assist in troubleshooting. It however, does NOT
make any attempt to mask the data.

If you don't want to, or can't, log complete request data, consider 
[Microsoft's App Insights Logger](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights/) 
instead.
