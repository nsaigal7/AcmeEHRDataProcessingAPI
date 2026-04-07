namespace AcmeEHRDataProcessingAPI.Middlewares;

using System.Diagnostics;
using AcmeEHRDataProcessingAPI.Models;
using AcmeEHRDataProcessingAPI.Persistence;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly FhirResourceStore _store;

    private static readonly HashSet<string> _NonLoggingLinks = ["/swagger/index.html", "/swagger", "/index.html", "/swagger/v1/swagger.json", "/healthz/ready", "/healthz/live"];

    public MetricsMiddleware(RequestDelegate next, IConfiguration configuration, FhirResourceStore dbService)
    {
        _next = next;
        _configuration = configuration;
        _store = dbService;
    }

    public async Task InvokeAsync(HttpContext context, ApiMetric apiMetric)
    {
        if (SkipRecordingMetrics(context.Request))
        {
            await _next(context);
            return;
        }
        var startDateTime = DateTime.Now;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            apiMetric.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
            apiMetric.DateOfExecution = startDateTime;
            apiMetric.Request = context?.Request?.Path;
            
            if (apiMetric is not null)
            {
                await _store.ApiMetrics.InsertOneAsync(apiMetric);
            }
        }
    }

    private bool SkipRecordingMetrics(HttpRequest request)
    {
        string requestPath = request?.Path ?? string.Empty;
        
        return _NonLoggingLinks.Contains(requestPath);
    }
}
