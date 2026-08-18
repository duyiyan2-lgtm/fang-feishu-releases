namespace FangFeishu.Api.Common;

public sealed record ApiResponse<T>(int Code, string Message, T? Data, string TraceId)
{
    public static ApiResponse<T> Success(T? data, string traceId, string message = "success")
    {
        return new ApiResponse<T>(0, message, data, traceId);
    }

    public static ApiResponse<T> Fail(int code, string message, string traceId)
    {
        return new ApiResponse<T>(code, message, default, traceId);
    }
}

