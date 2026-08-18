namespace FangFeishu.Api.Common;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request {TraceId} was canceled by the client.", context.TraceIdentifier);
            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = 499;
                await context.Response.WriteAsJsonAsync(
                    ApiResponse<object?>.Fail(5002, "Request canceled.", context.TraceIdentifier));
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API exception. TraceId: {TraceId}", context.TraceIdentifier);
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                ApiResponse<object?>.Fail(5000, "Internal server error.", context.TraceIdentifier));
        }
    }
}
