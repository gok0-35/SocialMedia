namespace SocialMedia.Api.Application.Services;

public enum ServiceErrorType
{
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound
}

public sealed class ServiceError
{
    public ServiceErrorType Type { get; }
    public string Message { get; }

    public ServiceError(ServiceErrorType type, string message)
    {
        Type = type;
        Message = message;
    }
}

public sealed class ServiceResult<T>
{
    public bool IsSuccess => Error is null;
    public T? Data { get; }
    public ServiceError? Error { get; }

    private ServiceResult(T data)
    {
        Data = data;
        Error = null;
    }

    private ServiceResult(ServiceError error)
    {
        Data = default;
        Error = error;
    }

    public static ServiceResult<T> Success(T data) => new(data);

    public static ServiceResult<T> Fail(ServiceErrorType type, string message)
        => new(new ServiceError(type, message));
}

