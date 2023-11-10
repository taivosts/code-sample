using Sample.Core.Models.Responses;
using System.Net;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sample.Api.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger _logger;
        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong: {ex}", ex);
                await HandleExceptionAsync(httpContext, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            _logger.LogError($"Exception: {exception.Message}", exception);
            await context.Response.WriteAsync(new ErrorResponseDetail()
            {
                StatusCode = context.Response.StatusCode,
                Message = "Internal Server Error from Exception middleware."
            }.ToString());
        }
    }
}
