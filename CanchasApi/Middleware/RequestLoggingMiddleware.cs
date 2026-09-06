using System.Diagnostics;

namespace CanchasApi.Middleware
{
    /// <summary>
    /// Registra el método HTTP, ruta, código de estado y duración de cada solicitud.
    /// </summary>
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Request.Method;
            var path = context.Request.Path;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                var logLevel = statusCode >= 500 ? LogLevel.Error
                    : statusCode >= 400 ? LogLevel.Warning
                    : LogLevel.Information;

                _logger.Log(
                    logLevel,
                    "HTTP {Method} {Path} respondió {StatusCode} en {ElapsedMs} ms",
                    method, path, statusCode, elapsedMs);
            }
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
